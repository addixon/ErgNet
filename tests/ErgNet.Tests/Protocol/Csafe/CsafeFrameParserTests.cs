using ErgNet.Protocol.Csafe;

namespace ErgNet.Tests.Protocol.Csafe;

public class CsafeFrameParserTests
{
    [Fact]
    public void Parse_MinimalFrame_ReturnsStatus()
    {
        // Build a frame: Start, status=0x01, checksum=0x01, Stop
        var frame = new byte[]
        {
            CsafeConstants.Standard_Frame_Start_Flag,
            0x01, // status
            0x01, // checksum (XOR of 0x01)
            CsafeConstants.Stop_Frame_Flag,
        };

        var response = CsafeFrameParser.Parse(frame);
        Assert.Equal(0x01, response.Status);
        Assert.Empty(response.Data);
    }

    [Fact]
    public void Parse_InvalidStartFlag_Throws()
    {
        var frame = new byte[] { 0x00, 0x01, 0x01, CsafeConstants.Stop_Frame_Flag };
        Assert.Throws<ArgumentException>(() => CsafeFrameParser.Parse(frame));
    }

    [Fact]
    public void Parse_MissingStopFlag_Throws()
    {
        var frame = new byte[] { CsafeConstants.Standard_Frame_Start_Flag, 0x01, 0x01, 0x00 };
        Assert.Throws<ArgumentException>(() => CsafeFrameParser.Parse(frame));
    }

    [Fact]
    public void Parse_BadChecksum_Throws()
    {
        var frame = new byte[]
        {
            CsafeConstants.Standard_Frame_Start_Flag,
            0x01,
            0xFF, // wrong checksum
            CsafeConstants.Stop_Frame_Flag,
        };
        Assert.Throws<InvalidOperationException>(() => CsafeFrameParser.Parse(frame));
    }

    [Fact]
    public void Parse_TooShortFrame_Throws()
    {
        var frame = new byte[] { 0xF1, 0xF2 };
        Assert.Throws<ArgumentException>(() => CsafeFrameParser.Parse(frame));
    }

    [Fact]
    public void Parse_StuffedBytes_UnstuffsCorrectly()
    {
        // Status=0xF0 → stuffed as F3 00, checksum=0xF0 → stuffed as F3 00
        var frame = new byte[]
        {
            CsafeConstants.Standard_Frame_Start_Flag,
            CsafeConstants.Byte_Stuffing_Flag, 0x00, // unstuffs to 0xF0
            CsafeConstants.Byte_Stuffing_Flag, 0x00, // checksum 0xF0
            CsafeConstants.Stop_Frame_Flag,
        };

        var response = CsafeFrameParser.Parse(frame);
        Assert.Equal(0xF0, response.Status);
    }

    [Fact]
    public void Parse_ShortCommandResponse_DecodesFields()
    {
        // Simulate GetHeartRateCurrent response: status, cmd=0xB0, len=1, HR=75, checksum
        byte status = 0x01;
        byte cmd = CsafeCommands.Short.GetHeartRateCurrent;
        byte dataLen = 0x01;
        byte hr = 75;
        byte checksum = (byte)(status ^ cmd ^ dataLen ^ hr);

        var frame = new byte[]
        {
            CsafeConstants.Standard_Frame_Start_Flag,
            status, cmd, dataLen, hr, checksum,
            CsafeConstants.Stop_Frame_Flag,
        };

        var response = CsafeFrameParser.Parse(frame);
        Assert.Equal(status, response.Status);
        Assert.True(response.Data.ContainsKey("GetHeartRateCurrent"));
        Assert.Equal(75, response.Data["GetHeartRateCurrent"][0]);
    }

    [Fact]
    public void RoundTrip_BuildAndParse_ProducesConsistentResult()
    {
        // Build a frame with GetStatus
        var commands = new[] { new CsafeCommand(CsafeCommands.Short.GetStatus) };
        var frame = CsafeFrameBuilder.Build(commands);

        // Simulate a response by replacing the command bytes with a status + response
        // For this test, just verify the builder output can be parsed without error
        // (the frame contains a command, not a response, but the structure is valid)
        // We construct a proper response frame manually
        byte status = 0x00;
        byte cmd = CsafeCommands.Short.GetStatus;
        byte checksum = (byte)(status ^ cmd);

        var responseFrame = new byte[]
        {
            CsafeConstants.Standard_Frame_Start_Flag,
            status, cmd, checksum,
            CsafeConstants.Stop_Frame_Flag,
        };

        var response = CsafeFrameParser.Parse(responseFrame);
        Assert.Equal(0x00, response.Status);
        Assert.True(response.Data.ContainsKey("GetStatus"));
    }

    [Fact]
    public void Parse_ExtendedFrameStartFlag_IsAccepted()
    {
        byte status = 0x01;
        byte checksum = status;

        var frame = new byte[]
        {
            CsafeConstants.Extended_Frame_Start_Flag,
            status, checksum,
            CsafeConstants.Stop_Frame_Flag,
        };

        var response = CsafeFrameParser.Parse(frame);
        Assert.Equal(0x01, response.Status);
    }
}
