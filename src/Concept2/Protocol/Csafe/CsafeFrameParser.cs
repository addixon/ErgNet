namespace Concept2.Protocol.Csafe;

/// <summary>
/// Represents a parsed CSAFE response frame.
/// </summary>
public sealed class CsafeResponse
{
    /// <summary>The machine status byte from the response frame.</summary>
    public byte Status { get; }

    /// <summary>
    /// Parsed response data keyed by command name.
    /// Each value is an array of integer fields decoded according to the
    /// <see cref="CsafeCommandDefinition.ResponseDataBytes"/> specification.
    /// </summary>
    public IReadOnlyDictionary<string, int[]> Data { get; }

    /// <summary>
    /// Initializes a new <see cref="CsafeResponse"/>.
    /// </summary>
    /// <param name="status">The status byte.</param>
    /// <param name="data">The parsed command response data.</param>
    public CsafeResponse(byte status, IReadOnlyDictionary<string, int[]> data)
    {
        Status = status;
        Data = data;
    }
}

/// <summary>
/// Parses CSAFE response frames, handling byte unstuffing, checksum validation,
/// and response data extraction using the <see cref="CsafeCommandRegistry"/>.
/// </summary>
public static class CsafeFrameParser
{
    /// <summary>
    /// Parses a complete CSAFE response frame.
    /// </summary>
    /// <param name="frame">The raw frame bytes including start and stop flags.</param>
    /// <returns>A <see cref="CsafeResponse"/> containing the status and decoded data.</returns>
    /// <exception cref="ArgumentException">Thrown when the frame is too short or missing flags.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the checksum does not match.</exception>
    public static CsafeResponse Parse(ReadOnlySpan<byte> frame)
    {
        if (frame.Length < 3)
        {
            throw new ArgumentException("Frame is too short to be valid.", nameof(frame));
        }

        var startFlag = frame[0];
        if (startFlag is not CsafeConstants.Standard_Frame_Start_Flag
                      and not CsafeConstants.Extended_Frame_Start_Flag)
        {
            throw new ArgumentException(
                $"Invalid start flag: 0x{startFlag:X2}.", nameof(frame));
        }

        if (frame[^1] != CsafeConstants.Stop_Frame_Flag)
        {
            throw new ArgumentException("Frame is missing stop flag.", nameof(frame));
        }

        // Unstuff the payload between start and stop flags
        var unstuffed = Unstuff(frame[1..^1]);

        if (unstuffed.Count < 2)
        {
            throw new ArgumentException("Frame payload is too short.", nameof(frame));
        }

        // Last byte of unstuffed data is the checksum
        var expectedChecksum = unstuffed[^1];
        var payloadBytes = unstuffed.GetRange(0, unstuffed.Count - 1);

        byte computed = 0;
        foreach (var b in payloadBytes)
        {
            computed ^= b;
        }

        if (computed != expectedChecksum)
        {
            throw new InvalidOperationException(
                $"Checksum mismatch: expected 0x{expectedChecksum:X2}, computed 0x{computed:X2}.");
        }

        // First byte is the status
        var status = payloadBytes[0];
        var data = ParsePayload(payloadBytes, 1);

        return new CsafeResponse(status, data);
    }

    private static Dictionary<string, int[]> ParsePayload(List<byte> bytes, int offset)
    {
        var result = new Dictionary<string, int[]>();

        while (offset < bytes.Count)
        {
            var cmdByte = bytes[offset++];

            if (cmdByte == CsafeCommands.Long.SetUserCfg1)
            {
                // PM wrapper: next byte is the inner data length
                if (offset >= bytes.Count)
                    break;

                var innerLen = bytes[offset++];
                var innerEnd = Math.Min(offset + innerLen, bytes.Count);

                ParseInnerCommands(bytes, offset, innerEnd, CsafeCommands.Long.SetUserCfg1, result);
                offset = innerEnd;
            }
            else if (CsafeConstants.IsLongCommand(cmdByte))
            {
                // Standard long command response
                if (offset >= bytes.Count)
                    break;

                var dataLen = bytes[offset++];
                if (CsafeCommandRegistry.TryGet(cmdByte, null, out var def) && def is not null)
                {
                    var values = DecodeFields(bytes, offset, dataLen, def.ResponseDataBytes);
                    result[def.Name] = values;
                }

                offset += dataLen;
            }
            else
            {
                // Short command response
                if (CsafeCommandRegistry.TryGet(cmdByte, null, out var def) && def is not null)
                {
                    if (def.ResponseDataBytes.Length > 0 && offset < bytes.Count)
                    {
                        var dataLen = bytes[offset++];
                        var values = DecodeFields(bytes, offset, dataLen, def.ResponseDataBytes);
                        result[def.Name] = values;
                        offset += dataLen;
                    }
                    else
                    {
                        result[def.Name] = [];
                    }
                }
            }
        }

        return result;
    }

    private static void ParseInnerCommands(
        List<byte> bytes, int offset, int end, byte wrapper,
        Dictionary<string, int[]> result)
    {
        while (offset < end)
        {
            var cmdByte = bytes[offset++];

            if (CsafeConstants.IsLongCommand(cmdByte))
            {
                if (offset >= end)
                    break;

                var dataLen = bytes[offset++];
                if (CsafeCommandRegistry.TryGet(cmdByte, wrapper, out var def) && def is not null)
                {
                    var values = DecodeFields(bytes, offset, dataLen, def.ResponseDataBytes);
                    result[def.Name] = values;
                }

                offset += dataLen;
            }
            else
            {
                // PM short command response
                if (CsafeCommandRegistry.TryGet(cmdByte, wrapper, out var def) && def is not null)
                {
                    if (def.ResponseDataBytes.Length > 0 && offset < end)
                    {
                        var dataLen = bytes[offset++];
                        var values = DecodeFields(bytes, offset, dataLen, def.ResponseDataBytes);
                        result[def.Name] = values;
                        offset += dataLen;
                    }
                    else
                    {
                        result[def.Name] = [];
                    }
                }
            }
        }
    }

    private static int[] DecodeFields(
        List<byte> bytes, int offset, int availableLen,
        System.Collections.Immutable.ImmutableArray<int> fieldDefs)
    {
        if (fieldDefs.Length == 0)
            return [];

        var values = new List<int>();
        var pos = offset;
        var end = Math.Min(offset + availableLen, bytes.Count);

        foreach (var fieldSize in fieldDefs)
        {
            if (pos >= end)
                break;

            if (fieldSize < 0)
            {
                // Negative value = ASCII string field; absolute value is length
                if (fieldSize == int.MinValue)
                    throw new InvalidOperationException("Invalid field size: int.MinValue cannot be handled.");

                var strLen = Math.Abs(fieldSize);
                var actualLen = Math.Min(strLen, end - pos);
                for (var i = 0; i < actualLen; i++)
                {
                    values.Add(bytes[pos++]);
                }
            }
            else if (fieldSize == 0)
            {
                // Variable-length: consume remaining bytes
                while (pos < end)
                {
                    values.Add(bytes[pos++]);
                }
            }
            else
            {
                // Fixed-size numeric field (little-endian byte order per CSAFE specification)
                var val = 0;
                var actualLen = Math.Min(fieldSize, end - pos);
                for (var i = 0; i < actualLen; i++)
                {
                    val |= bytes[pos++] << (8 * i);
                }
                values.Add(val);
            }
        }

        return values.ToArray();
    }

    private static List<byte> Unstuff(ReadOnlySpan<byte> data)
    {
        var result = new List<byte>(data.Length);
        var i = 0;

        while (i < data.Length)
        {
            if (data[i] == CsafeConstants.Byte_Stuffing_Flag && i + 1 < data.Length)
            {
                result.Add((byte)(data[i + 1] | 0xF0));
                i += 2;
            }
            else
            {
                result.Add(data[i]);
                i++;
            }
        }

        return result;
    }
}
