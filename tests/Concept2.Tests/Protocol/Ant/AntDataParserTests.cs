using Concept2.Protocol.Ant;

namespace Concept2.Tests.Protocol.Ant;

public class AntDataParserTests
{
    // ── GeneralFEData ────

    [Fact]
    public void ParseGeneralFEData_ValidPage_ParsesCorrectly()
    {
        // Page 0x10: equipment type=0x16 (Rower), elapsed=100 (25s), distance=50,
        //            speed=0x03E8 (1000 = 1.0 m/s), HR=72, flags=0x34 (distance enabled, InUse)
        var data = new byte[]
        {
            0x10, // page number
            0x16, // equipment type (Rower)
            100,  // elapsed time (100 * 0.25 = 25s)
            50,   // distance traveled (50m)
            0xE8, 0x03, // speed (1000 * 0.001 = 1.0 m/s)
            72,   // heart rate
            0x34, // bit 2: distance enabled, bits 4-6: InUse (3)
        };

        var result = AntDataParser.ParseGeneralFEData(data);

        Assert.Equal(0x16, result.EquipmentType);
        Assert.Equal(25.0, result.ElapsedTimeIncrement, 0.01);
        Assert.Equal(50, result.DistanceIncrement);
        Assert.Equal(1.0, result.InstantaneousSpeed, 0.001);
        Assert.Equal(72, result.HeartRate);
        Assert.True(result.DistanceEnabled);
        Assert.Equal(AntEquipmentState.InUse, result.State);
    }

    [Fact]
    public void ParseGeneralFEData_TooShort_Throws()
    {
        var data = new byte[] { 0x10, 0x16, 0x00 };
        Assert.Throws<ArgumentException>(() => AntDataParser.ParseGeneralFEData(data));
    }

    // ── RowerData ────

    [Fact]
    public void ParseRowerData_ValidPage_ParsesCorrectly()
    {
        // Page 0x16: stroke count=42, cadence=28 spm, power=200W (0x00C8), state=InUse
        var data = new byte[]
        {
            0x16, // page number
            0xFF, // reserved
            0xFF, // reserved
            42,   // stroke count
            28,   // cadence (spm)
            0xC8, 0x00, // instantaneous power (200W)
            0x30, // bits 4-6: InUse (3)
        };

        var result = AntDataParser.ParseRowerData(data);

        Assert.Equal(42, result.StrokeCountIncrement);
        Assert.Equal(28, result.Cadence);
        Assert.Equal(200, result.InstantaneousPower);
        Assert.Equal(AntEquipmentState.InUse, result.State);
    }

    [Fact]
    public void ParseRowerData_InvalidCadence_ReportsRawValue()
    {
        var data = new byte[] { 0x16, 0xFF, 0xFF, 10, 0xFF, 0x00, 0x00, 0x30 };

        var result = AntDataParser.ParseRowerData(data);

        Assert.Equal(0xFF, result.Cadence);
    }

    [Fact]
    public void ParseRowerData_TooShort_Throws()
    {
        var data = new byte[] { 0x16, 0xFF, 0xFF };
        Assert.Throws<ArgumentException>(() => AntDataParser.ParseRowerData(data));
    }

    // ── MetabolicData ────

    [Fact]
    public void ParseMetabolicData_ValidPage_ParsesCorrectly()
    {
        // Page 0x12: MET=350 (3.50), burn rate=5000 (500.0 kcal/hr), calories=100
        var data = new byte[]
        {
            0x12, // page number
            0xFF, // reserved
            0x5E, 0x01, // MET (350 * 0.01 = 3.50)
            0x88, 0x13, // burn rate (5000 * 0.1 = 500.0)
            100,        // calories
            0x30,       // state flags
        };

        var result = AntDataParser.ParseMetabolicData(data);

        Assert.Equal(3.50, result.InstantaneousMET, 0.01);
        Assert.Equal(500.0, result.CaloricBurnRate, 0.1);
        Assert.Equal(100, result.CaloricIncrement);
    }

    [Fact]
    public void ParseMetabolicData_TooShort_Throws()
    {
        var data = new byte[] { 0x12, 0xFF, 0x00 };
        Assert.Throws<ArgumentException>(() => AntDataParser.ParseMetabolicData(data));
    }

    // ── GetDataPageNumber ────

    [Fact]
    public void GetDataPageNumber_ReturnsFirstByte()
    {
        var data = new byte[] { 0x16, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00 };
        Assert.Equal(0x16, AntDataParser.GetDataPageNumber(data));
    }

    [Fact]
    public void GetDataPageNumber_EmptyData_Throws()
    {
        Assert.Throws<ArgumentException>(() => AntDataParser.GetDataPageNumber(ReadOnlySpan<byte>.Empty));
    }

    // ── Equipment state parsing ────

    [Theory]
    [InlineData(0x00, AntEquipmentState.Unknown)]
    [InlineData(0x10, AntEquipmentState.AsleepOrOff)]
    [InlineData(0x20, AntEquipmentState.Ready)]
    [InlineData(0x30, AntEquipmentState.InUse)]
    [InlineData(0x40, AntEquipmentState.FinishedOrPaused)]
    public void ParseGeneralFEData_EquipmentState_ParsedCorrectly(byte stateByte, AntEquipmentState expected)
    {
        var data = new byte[] { 0x10, 0x16, 0x00, 0x00, 0x00, 0x00, 0x00, stateByte };

        var result = AntDataParser.ParseGeneralFEData(data);

        Assert.Equal(expected, result.State);
    }

    // ── NordicSkierData ────

    [Fact]
    public void ParseNordicSkierData_ValidPage_ParsesCorrectly()
    {
        // Page 0x18: stride count=30, cadence=50 spm, power=150W (0x0096), state=InUse
        var data = new byte[]
        {
            0x18, // page number
            0xFF, // reserved
            0xFF, // reserved
            30,   // stride count
            50,   // cadence (spm)
            0x96, 0x00, // instantaneous power (150W)
            0x30, // bits 4-6: InUse (3)
        };

        var result = AntDataParser.ParseNordicSkierData(data);

        Assert.Equal(30, result.StrideCountIncrement);
        Assert.Equal(50, result.Cadence);
        Assert.Equal(150, result.InstantaneousPower);
        Assert.Equal(AntEquipmentState.InUse, result.State);
    }

    [Fact]
    public void ParseNordicSkierData_InvalidCadence_ReportsRawValue()
    {
        var data = new byte[] { 0x18, 0xFF, 0xFF, 10, 0xFF, 0x00, 0x00, 0x30 };

        var result = AntDataParser.ParseNordicSkierData(data);

        Assert.Equal(0xFF, result.Cadence);
    }

    [Fact]
    public void ParseNordicSkierData_TooShort_Throws()
    {
        var data = new byte[] { 0x18, 0xFF, 0xFF };
        Assert.Throws<ArgumentException>(() => AntDataParser.ParseNordicSkierData(data));
    }
}
