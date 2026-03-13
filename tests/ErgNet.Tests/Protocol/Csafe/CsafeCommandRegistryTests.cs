using ErgNet.Protocol.Csafe;

namespace ErgNet.Tests.Protocol.Csafe;

public class CsafeCommandRegistryTests
{
    [Fact]
    public void GetKey_WithoutWrapper_ReturnsCommandId()
    {
        var key = CsafeCommandRegistry.GetKey(0x80, null);
        Assert.Equal(0x80, key);
    }

    [Fact]
    public void GetKey_WithWrapper_ReturnsCombinedKey()
    {
        var key = CsafeCommandRegistry.GetKey(0x89, 0x1A);
        Assert.Equal(0x1A89, key);
    }

    [Theory]
    [InlineData(CsafeCommands.Short.GetStatus, "GetStatus")]
    [InlineData(CsafeCommands.Short.GetVersion, "GetVersion")]
    [InlineData(CsafeCommands.Short.GetSerial, "GetSerial")]
    [InlineData(CsafeCommands.Short.GetPower, "GetPower")]
    public void TryGet_ShortCommand_FindsDefinition(byte commandId, string expectedName)
    {
        var found = CsafeCommandRegistry.TryGet(commandId, null, out var def);
        Assert.True(found);
        Assert.NotNull(def);
        Assert.Equal(expectedName, def.Name);
        Assert.Equal(commandId, def.CommandId);
        Assert.Null(def.WrapperCommand);
    }

    [Theory]
    [InlineData(CsafeCommands.Long.SetTime, "SetTime")]
    [InlineData(CsafeCommands.Long.SetProgram, "SetProgram")]
    [InlineData(CsafeCommands.Long.GetCaps, "GetCaps")]
    public void TryGet_LongCommand_FindsDefinition(byte commandId, string expectedName)
    {
        var found = CsafeCommandRegistry.TryGet(commandId, null, out var def);
        Assert.True(found);
        Assert.NotNull(def);
        Assert.Equal(expectedName, def.Name);
    }

    [Theory]
    [InlineData(CsafeCommands.PmShort.PM_GetWorkoutType, "PM_GetWorkoutType")]
    [InlineData(CsafeCommands.PmShort.PM_GetDragFactor, "PM_GetDragFactor")]
    [InlineData(CsafeCommands.PmShort.PM_GetRestTime, "PM_GetRestTime")]
    [InlineData(CsafeCommands.PmShort.PM_GetErgMachineType, "PM_GetErgMachineType")]
    [InlineData(CsafeCommands.PmShort.PM_GetWorkoutNumber, "PM_GetWorkoutNumber")]
    [InlineData(CsafeCommands.PmShort.PM_GetAveragePace, "PM_GetAveragePace")]
    public void TryGet_PmShortCommand_FindsDefinition(byte commandId, string expectedName)
    {
        var found = CsafeCommandRegistry.TryGet(commandId, CsafeCommands.Long.SetUserCfg1, out var def);
        Assert.True(found);
        Assert.NotNull(def);
        Assert.Equal(expectedName, def.Name);
        Assert.Equal(CsafeCommands.Long.SetUserCfg1, def.WrapperCommand);
    }

    [Theory]
    [InlineData(CsafeCommands.PmLong.PM_SetSplitDuration, "PM_SetSplitDuration")]
    [InlineData(CsafeCommands.PmLong.PM_GetForcePlotData, "PM_GetForcePlotData")]
    [InlineData(CsafeCommands.PmLong.PM_GetStrokeStats, "PM_GetStrokeStats")]
    public void TryGet_PmLongCommand_FindsDefinition(byte commandId, string expectedName)
    {
        var found = CsafeCommandRegistry.TryGet(commandId, CsafeCommands.Long.SetUserCfg1, out var def);
        Assert.True(found);
        Assert.NotNull(def);
        Assert.Equal(expectedName, def.Name);
    }

    [Fact]
    public void TryGet_UnknownCommand_ReturnsFalse()
    {
        var found = CsafeCommandRegistry.TryGet(0xFF, null, out var def);
        Assert.False(found);
        Assert.Null(def);
    }

    [Fact]
    public void Definitions_ContainsAllExpectedCommands()
    {
        // 8 status + 15 data + 12 long + 14 PM short + 5 PM long = 54
        Assert.Equal(54, CsafeCommandRegistry.Definitions.Count);
    }

    [Fact]
    public void GetVersion_HasCorrectResponseLayout()
    {
        CsafeCommandRegistry.TryGet(CsafeCommands.Short.GetVersion, null, out var def);
        Assert.NotNull(def);
        Assert.Equal([1, 1, 1, 2, 2], def.ResponseDataBytes.AsSpan().ToArray());
        Assert.True(def.RequestDataBytes.IsEmpty);
    }

    [Fact]
    public void SetTime_HasCorrectRequestLayout()
    {
        CsafeCommandRegistry.TryGet(CsafeCommands.Long.SetTime, null, out var def);
        Assert.NotNull(def);
        Assert.Equal([1, 1, 1], def.RequestDataBytes.AsSpan().ToArray());
        Assert.True(def.ResponseDataBytes.IsEmpty);
    }

    [Fact]
    public void PM_GetForcePlotData_HasCorrectLayout()
    {
        CsafeCommandRegistry.TryGet(
            CsafeCommands.PmLong.PM_GetForcePlotData,
            CsafeCommands.Long.SetUserCfg1,
            out var def);

        Assert.NotNull(def);
        Assert.Equal([1], def.RequestDataBytes.AsSpan().ToArray());
        Assert.Equal(17, def.ResponseDataBytes.Length);
        Assert.Equal(1, def.ResponseDataBytes[0]);
        Assert.Equal(2, def.ResponseDataBytes[1]);
    }
}
