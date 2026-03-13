using System.Collections.Frozen;
using System.Collections.Immutable;

namespace ErgNet.Protocol.Csafe;

/// <summary>
/// Static registry that maps command identifiers to <see cref="CsafeCommandDefinition"/> objects.
/// For PM-specific commands the lookup key is <c>(wrapperCommand &lt;&lt; 8) | commandId</c>
/// (e.g., <c>0x1A89</c> for PM_GetWorkoutType).
/// </summary>
public static class CsafeCommandRegistry
{
    /// <summary>
    /// Computes the dictionary key for a command.
    /// </summary>
    /// <param name="commandId">The command byte.</param>
    /// <param name="wrapperCommand">Optional wrapper command byte.</param>
    /// <returns>An integer key suitable for the <see cref="Definitions"/> dictionary.</returns>
    public static int GetKey(byte commandId, byte? wrapperCommand = null) =>
        wrapperCommand.HasValue ? (wrapperCommand.Value << 8) | commandId : commandId;

    private static CsafeCommandDefinition Def(
        string name,
        byte commandId,
        int[] requestDataBytes,
        int[] responseDataBytes,
        byte? wrapperCommand = null) =>
        new(name, commandId,
            [.. requestDataBytes],
            [.. responseDataBytes],
            wrapperCommand);

    /// <summary>
    /// All known command definitions keyed by their composite identifier.
    /// </summary>
    public static FrozenDictionary<int, CsafeCommandDefinition> Definitions { get; } =
        BuildDefinitions();

    private static FrozenDictionary<int, CsafeCommandDefinition> BuildDefinitions()
    {
        const byte wrap = CsafeCommands.Long.SetUserCfg1;

        var defs = new Dictionary<int, CsafeCommandDefinition>
        {
            // ── Short commands: status / state (no request data, no response data) ──
            [CsafeCommands.Short.GetStatus] =
                Def("GetStatus", CsafeCommands.Short.GetStatus, [], []),
            [CsafeCommands.Short.Reset] =
                Def("Reset", CsafeCommands.Short.Reset, [], []),
            [CsafeCommands.Short.GoIdle] =
                Def("GoIdle", CsafeCommands.Short.GoIdle, [], []),
            [CsafeCommands.Short.GoHaveId] =
                Def("GoHaveId", CsafeCommands.Short.GoHaveId, [], []),
            [CsafeCommands.Short.GoInUse] =
                Def("GoInUse", CsafeCommands.Short.GoInUse, [], []),
            [CsafeCommands.Short.GoFinished] =
                Def("GoFinished", CsafeCommands.Short.GoFinished, [], []),
            [CsafeCommands.Short.GoReady] =
                Def("GoReady", CsafeCommands.Short.GoReady, [], []),
            [CsafeCommands.Short.BadId] =
                Def("BadId", CsafeCommands.Short.BadId, [], []),

            // ── Short commands: data retrieval ──
            [CsafeCommands.Short.GetVersion] =
                Def("GetVersion", CsafeCommands.Short.GetVersion, [], [1, 1, 1, 2, 2]),
            [CsafeCommands.Short.GetId] =
                Def("GetId", CsafeCommands.Short.GetId, [], [-5]),
            [CsafeCommands.Short.GetUnits] =
                Def("GetUnits", CsafeCommands.Short.GetUnits, [], [1]),
            [CsafeCommands.Short.GetSerial] =
                Def("GetSerial", CsafeCommands.Short.GetSerial, [], [-9]),
            [CsafeCommands.Short.GetOdometer] =
                Def("GetOdometer", CsafeCommands.Short.GetOdometer, [], [4, 1]),
            [CsafeCommands.Short.GetErrorCode] =
                Def("GetErrorCode", CsafeCommands.Short.GetErrorCode, [], [3]),
            [CsafeCommands.Short.GetTWork] =
                Def("GetTWork", CsafeCommands.Short.GetTWork, [], [1, 1, 1]),
            [CsafeCommands.Short.GetHorizontal] =
                Def("GetHorizontal", CsafeCommands.Short.GetHorizontal, [], [2, 1]),
            [CsafeCommands.Short.GetCalories] =
                Def("GetCalories", CsafeCommands.Short.GetCalories, [], [2]),
            [CsafeCommands.Short.GetProgram] =
                Def("GetProgram", CsafeCommands.Short.GetProgram, [], [1]),
            [CsafeCommands.Short.GetPace] =
                Def("GetPace", CsafeCommands.Short.GetPace, [], [2, 1]),
            [CsafeCommands.Short.GetCadence] =
                Def("GetCadence", CsafeCommands.Short.GetCadence, [], [2, 1]),
            [CsafeCommands.Short.GetUserInfo] =
                Def("GetUserInfo", CsafeCommands.Short.GetUserInfo, [], [2, 1, 1, 1]),
            [CsafeCommands.Short.GetHeartRateCurrent] =
                Def("GetHeartRateCurrent", CsafeCommands.Short.GetHeartRateCurrent, [], [1]),
            [CsafeCommands.Short.GetPower] =
                Def("GetPower", CsafeCommands.Short.GetPower, [], [2, 1]),

            // ── Long commands: configuration ──
            [CsafeCommands.Long.AutoUpload] =
                Def("AutoUpload", CsafeCommands.Long.AutoUpload, [1], []),
            [CsafeCommands.Long.IdDigits] =
                Def("IdDigits", CsafeCommands.Long.IdDigits, [1], []),
            [CsafeCommands.Long.SetTime] =
                Def("SetTime", CsafeCommands.Long.SetTime, [1, 1, 1], []),
            [CsafeCommands.Long.SetDate] =
                Def("SetDate", CsafeCommands.Long.SetDate, [1, 1, 1], []),
            [CsafeCommands.Long.SetTimeout] =
                Def("SetTimeout", CsafeCommands.Long.SetTimeout, [1], []),
            [CsafeCommands.Long.SetUserCfg1] =
                Def("SetUserCfg1", CsafeCommands.Long.SetUserCfg1, [0], []),
            [CsafeCommands.Long.SetTWork] =
                Def("SetTWork", CsafeCommands.Long.SetTWork, [1, 1, 1], []),
            [CsafeCommands.Long.SetHorizontal] =
                Def("SetHorizontal", CsafeCommands.Long.SetHorizontal, [2, 1], []),
            [CsafeCommands.Long.SetCalories] =
                Def("SetCalories", CsafeCommands.Long.SetCalories, [2], []),
            [CsafeCommands.Long.SetProgram] =
                Def("SetProgram", CsafeCommands.Long.SetProgram, [1, 1], []),
            [CsafeCommands.Long.SetPower] =
                Def("SetPower", CsafeCommands.Long.SetPower, [2, 1], []),
            [CsafeCommands.Long.GetCaps] =
                Def("GetCaps", CsafeCommands.Long.GetCaps, [1], []),

            // ── PM-specific short commands (wrapped in 0x1A) ──
            [GetKey(CsafeCommands.PmShort.PM_GetWorkoutType, wrap)] =
                Def("PM_GetWorkoutType", CsafeCommands.PmShort.PM_GetWorkoutType, [], [1], wrap),
            [GetKey(CsafeCommands.PmShort.PM_GetDragFactor, wrap)] =
                Def("PM_GetDragFactor", CsafeCommands.PmShort.PM_GetDragFactor, [], [1], wrap),
            [GetKey(CsafeCommands.PmShort.PM_GetStrokeState, wrap)] =
                Def("PM_GetStrokeState", CsafeCommands.PmShort.PM_GetStrokeState, [], [1], wrap),
            [GetKey(CsafeCommands.PmShort.PM_GetWorkTime, wrap)] =
                Def("PM_GetWorkTime", CsafeCommands.PmShort.PM_GetWorkTime, [], [4, 1], wrap),
            [GetKey(CsafeCommands.PmShort.PM_GetWorkDistance, wrap)] =
                Def("PM_GetWorkDistance", CsafeCommands.PmShort.PM_GetWorkDistance, [], [4, 1], wrap),
            [GetKey(CsafeCommands.PmShort.PM_GetErrorValue, wrap)] =
                Def("PM_GetErrorValue", CsafeCommands.PmShort.PM_GetErrorValue, [], [2], wrap),
            [GetKey(CsafeCommands.PmShort.PM_GetWorkoutState, wrap)] =
                Def("PM_GetWorkoutState", CsafeCommands.PmShort.PM_GetWorkoutState, [], [1], wrap),
            [GetKey(CsafeCommands.PmShort.PM_GetWorkoutIntervalCount, wrap)] =
                Def("PM_GetWorkoutIntervalCount", CsafeCommands.PmShort.PM_GetWorkoutIntervalCount, [], [1], wrap),
            [GetKey(CsafeCommands.PmShort.PM_GetIntervalType, wrap)] =
                Def("PM_GetIntervalType", CsafeCommands.PmShort.PM_GetIntervalType, [], [1], wrap),
            [GetKey(CsafeCommands.PmShort.PM_GetRestTime, wrap)] =
                Def("PM_GetRestTime", CsafeCommands.PmShort.PM_GetRestTime, [], [2], wrap),
            [GetKey(CsafeCommands.PmShort.PM_GetDisplayUnits, wrap)] =
                Def("PM_GetDisplayUnits", CsafeCommands.PmShort.PM_GetDisplayUnits, [], [1], wrap),
            [GetKey(CsafeCommands.PmShort.PM_GetWorkoutNumber, wrap)] =
                Def("PM_GetWorkoutNumber", CsafeCommands.PmShort.PM_GetWorkoutNumber, [], [1], wrap),
            [GetKey(CsafeCommands.PmShort.PM_GetErgMachineType, wrap)] =
                Def("PM_GetErgMachineType", CsafeCommands.PmShort.PM_GetErgMachineType, [], [1], wrap),
            [GetKey(CsafeCommands.PmShort.PM_GetAveragePace, wrap)] =
                Def("PM_GetAveragePace", CsafeCommands.PmShort.PM_GetAveragePace, [], [2], wrap),

            // ── PM-specific long commands (wrapped in 0x1A) ──
            [GetKey(CsafeCommands.PmLong.PM_SetSplitDuration, wrap)] =
                Def("PM_SetSplitDuration", CsafeCommands.PmLong.PM_SetSplitDuration, [1, 4], [], wrap),
            [GetKey(CsafeCommands.PmLong.PM_GetForcePlotData, wrap)] =
                Def("PM_GetForcePlotData", CsafeCommands.PmLong.PM_GetForcePlotData,
                    [1], [1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2], wrap),
            [GetKey(CsafeCommands.PmLong.PM_SetScreenErrorMode, wrap)] =
                Def("PM_SetScreenErrorMode", CsafeCommands.PmLong.PM_SetScreenErrorMode, [1], [], wrap),
            [GetKey(CsafeCommands.PmLong.PM_GetHeartbeatData, wrap)] =
                Def("PM_GetHeartbeatData", CsafeCommands.PmLong.PM_GetHeartbeatData,
                    [1], [1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2], wrap),
            [GetKey(CsafeCommands.PmLong.PM_GetStrokeStats, wrap)] =
                Def("PM_GetStrokeStats", CsafeCommands.PmLong.PM_GetStrokeStats,
                    [0], [2, 1, 2, 1, 2, 2, 2, 2, 2], wrap),
        };

        return defs.ToFrozenDictionary();
    }

    /// <summary>
    /// Attempts to find a command definition by its command identifier and optional wrapper.
    /// </summary>
    /// <param name="commandId">The command byte.</param>
    /// <param name="wrapperCommand">Optional wrapper command byte for PM commands.</param>
    /// <param name="definition">The matching definition, if found.</param>
    /// <returns><c>true</c> if a matching definition was found; otherwise <c>false</c>.</returns>
    public static bool TryGet(byte commandId, byte? wrapperCommand, out CsafeCommandDefinition? definition) =>
        Definitions.TryGetValue(GetKey(commandId, wrapperCommand), out definition);
}
