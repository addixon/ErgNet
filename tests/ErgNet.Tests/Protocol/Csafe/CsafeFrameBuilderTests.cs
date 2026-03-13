using ErgNet.Protocol.Csafe;

namespace ErgNet.Tests.Protocol.Csafe;

public class CsafeFrameBuilderTests
{
    [Fact]
    public void Build_SingleShortCommand_ProducesValidFrame()
    {
        var commands = new[] { new CsafeCommand(CsafeCommands.Short.GetStatus) };
        var frame = CsafeFrameBuilder.Build(commands);

        Assert.Equal(CsafeConstants.Standard_Frame_Start_Flag, frame[0]);
        Assert.Equal(CsafeConstants.Stop_Frame_Flag, frame[^1]);
        // Payload: 0x80, checksum: 0x80
        Assert.Equal(4, frame.Length);
    }

    [Fact]
    public void Build_MultipleShortCommands_IncludesAll()
    {
        var commands = new[]
        {
            new CsafeCommand(CsafeCommands.Short.GetStatus),
            new CsafeCommand(CsafeCommands.Short.GetVersion),
        };
        var frame = CsafeFrameBuilder.Build(commands);

        Assert.Equal(CsafeConstants.Standard_Frame_Start_Flag, frame[0]);
        Assert.Equal(CsafeConstants.Stop_Frame_Flag, frame[^1]);
        Assert.Contains((byte)CsafeCommands.Short.GetStatus, frame);
        Assert.Contains((byte)CsafeCommands.Short.GetVersion, frame);
    }

    [Fact]
    public void Build_LongCommandWithData_IncludesLengthAndData()
    {
        var commands = new[] { new CsafeCommand(CsafeCommands.Long.SetTimeout, [30]) };
        var frame = CsafeFrameBuilder.Build(commands);

        // Frame: Start, 0x13, 0x01, 0x1E, checksum, Stop
        Assert.Equal(CsafeConstants.Standard_Frame_Start_Flag, frame[0]);
        Assert.Equal(CsafeConstants.Stop_Frame_Flag, frame[^1]);
    }

    [Fact]
    public void Build_PmWrappedCommand_WrapsCorrectly()
    {
        var commands = new[]
        {
            new CsafeCommand(CsafeCommands.PmShort.PM_GetWorkoutType,
                wrapperCommand: CsafeCommands.Long.SetUserCfg1),
        };
        var frame = CsafeFrameBuilder.Build(commands);

        Assert.Equal(CsafeConstants.Standard_Frame_Start_Flag, frame[0]);
        Assert.Equal(CsafeConstants.Stop_Frame_Flag, frame[^1]);
        // Should contain the wrapper 0x1A
        Assert.Contains(CsafeCommands.Long.SetUserCfg1, frame);
    }

    [Fact]
    public void Build_ByteStuffing_StuffsSpecialBytes()
    {
        // Create a command whose data contains 0xF1 which needs stuffing
        var commands = new[] { new CsafeCommand(CsafeCommands.Long.AutoUpload, [0xF1]) };
        var frame = CsafeFrameBuilder.Build(commands);

        // The stuffed byte 0xF1 should become 0xF3 0x01
        var stuffingIndex = Array.IndexOf(frame, CsafeConstants.Byte_Stuffing_Flag);
        Assert.True(stuffingIndex > 0, "Byte stuffing flag should be present");
        Assert.Equal(0x01, frame[stuffingIndex + 1]);
    }

    [Fact]
    public void Build_ChecksumIsXorOfPayload()
    {
        var commands = new[] { new CsafeCommand(CsafeCommands.Short.GetStatus) };
        var frame = CsafeFrameBuilder.Build(commands);

        // Unstuff and verify checksum
        var payload = UnstuffPayload(frame);
        var checksum = payload[^1];
        byte computed = 0;
        for (var i = 0; i < payload.Length - 1; i++)
        {
            computed ^= payload[i];
        }
        Assert.Equal(computed, checksum);
    }

    [Fact]
    public void Build_NullCommands_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => CsafeFrameBuilder.Build(null!));
    }

    [Fact]
    public void Build_LongCommandNoData_IncludesZeroLength()
    {
        var commands = new[] { new CsafeCommand(CsafeCommands.Long.SetTimeout) };
        var frame = CsafeFrameBuilder.Build(commands);

        // Should contain 0x13 0x00 (command + zero-length)
        var payload = UnstuffPayload(frame);
        var cmdIndex = Array.IndexOf(payload, CsafeCommands.Long.SetTimeout);
        Assert.True(cmdIndex >= 0);
        Assert.Equal(0x00, payload[cmdIndex + 1]);
    }

    [Fact]
    public void Build_MultiplePmCommands_CoalesceUnderSingleWrapper()
    {
        var commands = new[]
        {
            new CsafeCommand(CsafeCommands.PmShort.PM_GetWorkoutType,
                wrapperCommand: CsafeCommands.Long.SetUserCfg1),
            new CsafeCommand(CsafeCommands.PmShort.PM_GetDragFactor,
                wrapperCommand: CsafeCommands.Long.SetUserCfg1),
        };
        var frame = CsafeFrameBuilder.Build(commands);
        var payload = UnstuffPayload(frame);

        // The wrapper 0x1A should appear only once
        var count = payload.Count(b => b == CsafeCommands.Long.SetUserCfg1);
        Assert.Equal(1, count);
    }

    private static byte[] UnstuffPayload(byte[] frame)
    {
        var result = new List<byte>();
        for (var i = 1; i < frame.Length - 1; i++)
        {
            if (frame[i] == CsafeConstants.Byte_Stuffing_Flag && i + 1 < frame.Length - 1)
            {
                result.Add((byte)(frame[i + 1] | 0xF0));
                i++;
            }
            else
            {
                result.Add(frame[i]);
            }
        }
        return result.ToArray();
    }
}
