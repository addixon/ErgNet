namespace Concept2.Protocol.Csafe;

/// <summary>
/// Represents a single CSAFE command to be sent to the device, optionally wrapped
/// inside a PM-specific wrapper command.
/// </summary>
public sealed class CsafeCommand
{
    /// <summary>The CSAFE command byte identifier.</summary>
    public byte CommandId { get; }

    /// <summary>Optional data payload for the command.</summary>
    public byte[]? Data { get; }

    /// <summary>
    /// Optional wrapper command byte (e.g., 0x1A for PM-specific commands).
    /// When set, this command is wrapped inside the specified outer command.
    /// </summary>
    public byte? WrapperCommand { get; }

    /// <summary>
    /// Initializes a new <see cref="CsafeCommand"/>.
    /// </summary>
    /// <param name="commandId">The CSAFE command byte.</param>
    /// <param name="data">Optional data bytes to include with the command.</param>
    /// <param name="wrapperCommand">Optional wrapper command for PM-specific commands.</param>
    public CsafeCommand(byte commandId, byte[]? data = null, byte? wrapperCommand = null)
    {
        CommandId = commandId;
        Data = data;
        WrapperCommand = wrapperCommand;
    }
}

/// <summary>
/// Builds complete CSAFE frames from one or more <see cref="CsafeCommand"/> objects,
/// handling byte stuffing, checksums, and PM wrapper framing.
/// </summary>
public static class CsafeFrameBuilder
{
    /// <summary>
    /// Builds a complete CSAFE frame from the given commands.
    /// </summary>
    /// <param name="commands">The commands to include in the frame.</param>
    /// <returns>A byte array containing the complete CSAFE frame.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="commands"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the resulting frame exceeds <see cref="CsafeConstants.MaxFrameSize"/> bytes.
    /// </exception>
    public static byte[] Build(IEnumerable<CsafeCommand> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);

        // Encode payload (before stuffing)
        var payload = EncodePayload(commands);

        // Calculate checksum over the raw payload bytes
        var checksum = ComputeChecksum(payload);

        // Build the final frame with stuffing
        var frame = new List<byte>(CsafeConstants.MaxFrameSize)
        {
            CsafeConstants.Standard_Frame_Start_Flag
        };

        foreach (var b in payload)
        {
            AppendWithStuffing(frame, b);
        }

        AppendWithStuffing(frame, checksum);
        frame.Add(CsafeConstants.Stop_Frame_Flag);

        if (frame.Count > CsafeConstants.MaxFrameSize)
        {
            throw new InvalidOperationException(
                $"CSAFE frame size ({frame.Count}) exceeds maximum ({CsafeConstants.MaxFrameSize}).");
        }

        return frame.ToArray();
    }

    private static byte[] EncodePayload(IEnumerable<CsafeCommand> commands)
    {
        var payload = new List<byte>();

        // Group commands by wrapper to coalesce PM commands under a single wrapper
        var wrapperGroups = new Dictionary<byte, List<CsafeCommand>>();
        var directCommands = new List<(int Order, CsafeCommand Cmd)>();
        var order = 0;

        foreach (var cmd in commands)
        {
            if (cmd.WrapperCommand.HasValue)
            {
                var wrapper = cmd.WrapperCommand.Value;
                if (!wrapperGroups.TryGetValue(wrapper, out var group))
                {
                    group = [];
                    wrapperGroups[wrapper] = group;
                    // Record position where this wrapper group first appeared
                    directCommands.Add((order++, new CsafeCommand(wrapper)));
                }
                group.Add(cmd);
            }
            else
            {
                directCommands.Add((order++, cmd));
            }
        }

        foreach (var (_, cmd) in directCommands.OrderBy(x => x.Order))
        {
            if (wrapperGroups.TryGetValue(cmd.CommandId, out var wrappedCmds))
            {
                // Encode wrapper command with inner commands
                var innerPayload = EncodeInnerCommands(wrappedCmds);
                payload.Add(cmd.CommandId);
                payload.Add((byte)innerPayload.Length);
                payload.AddRange(innerPayload);

                // Remove so it won't be processed again
                wrapperGroups.Remove(cmd.CommandId);
            }
            else
            {
                EncodeDirectCommand(payload, cmd);
            }
        }

        return payload.ToArray();
    }

    private static byte[] EncodeInnerCommands(List<CsafeCommand> commands)
    {
        var inner = new List<byte>();
        foreach (var cmd in commands)
        {
            EncodeDirectCommand(inner, cmd);
        }
        return inner.ToArray();
    }

    private static void EncodeDirectCommand(List<byte> buffer, CsafeCommand cmd)
    {
        buffer.Add(cmd.CommandId);

        if (CsafeConstants.IsLongCommand(cmd.CommandId) && cmd.Data is { Length: > 0 })
        {
            buffer.Add((byte)cmd.Data.Length);
            buffer.AddRange(cmd.Data);
        }
        else if (CsafeConstants.IsLongCommand(cmd.CommandId))
        {
            buffer.Add(0x00);
        }
        // Short commands: just the command byte, no length/data
    }

    private static byte ComputeChecksum(ReadOnlySpan<byte> data)
    {
        byte checksum = 0;
        foreach (var b in data)
        {
            checksum ^= b;
        }
        return checksum;
    }

    private static void AppendWithStuffing(List<byte> frame, byte value)
    {
        if (value is >= CsafeConstants.Extended_Frame_Start_Flag
                  and <= CsafeConstants.Byte_Stuffing_Flag)
        {
            frame.Add(CsafeConstants.Byte_Stuffing_Flag);
            frame.Add((byte)(value & CsafeConstants.StuffingMask));
        }
        else
        {
            frame.Add(value);
        }
    }
}
