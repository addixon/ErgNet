namespace ErgNet.Protocol.Csafe;

/// <summary>
/// Contains all known CSAFE command byte codes, organized by category.
/// </summary>
public static class CsafeCommands
{
    /// <summary>
    /// Short (single-byte) CSAFE commands for status, state, and data retrieval.
    /// </summary>
    public static class Short
    {
        // Status / State

        /// <summary>Get the current machine status.</summary>
        public const byte GetStatus = 0x80;

        /// <summary>Reset the machine.</summary>
        public const byte Reset = 0x81;

        /// <summary>Transition to the Idle state.</summary>
        public const byte GoIdle = 0x82;

        /// <summary>Transition to the HaveId state.</summary>
        public const byte GoHaveId = 0x83;

        /// <summary>Transition to the InUse state.</summary>
        public const byte GoInUse = 0x85;

        /// <summary>Transition to the Finished state.</summary>
        public const byte GoFinished = 0x86;

        /// <summary>Transition to the Ready state.</summary>
        public const byte GoReady = 0x87;

        /// <summary>Signal a bad identifier.</summary>
        public const byte BadId = 0x88;

        // Data retrieval

        /// <summary>Get the firmware version information.</summary>
        public const byte GetVersion = 0x91;

        /// <summary>Get the machine identifier.</summary>
        public const byte GetId = 0x92;

        /// <summary>Get the current display units.</summary>
        public const byte GetUnits = 0x93;

        /// <summary>Get the serial number.</summary>
        public const byte GetSerial = 0x94;

        /// <summary>Get the odometer reading.</summary>
        public const byte GetOdometer = 0x9B;

        /// <summary>Get the last error code.</summary>
        public const byte GetErrorCode = 0x9C;

        /// <summary>Get the total work time.</summary>
        public const byte GetTWork = 0xA0;

        /// <summary>Get the horizontal distance.</summary>
        public const byte GetHorizontal = 0xA1;

        /// <summary>Get the calorie count.</summary>
        public const byte GetCalories = 0xA3;

        /// <summary>Get the current program number.</summary>
        public const byte GetProgram = 0xA4;

        /// <summary>Get the current pace.</summary>
        public const byte GetPace = 0xA6;

        /// <summary>Get the current cadence (strokes per minute).</summary>
        public const byte GetCadence = 0xA7;

        /// <summary>Get the user information.</summary>
        public const byte GetUserInfo = 0xAB;

        /// <summary>Get the current heart rate.</summary>
        public const byte GetHeartRateCurrent = 0xB0;

        /// <summary>Get the current power output.</summary>
        public const byte GetPower = 0xB4;
    }

    /// <summary>
    /// Long (multi-byte) CSAFE commands for configuration and data setting.
    /// </summary>
    public static class Long
    {
        /// <summary>Enable or disable automatic data upload.</summary>
        public const byte AutoUpload = 0x01;

        /// <summary>Set the number of identifier digits.</summary>
        public const byte IdDigits = 0x10;

        /// <summary>Set the current time.</summary>
        public const byte SetTime = 0x11;

        /// <summary>Set the current date.</summary>
        public const byte SetDate = 0x12;

        /// <summary>Set the communication timeout.</summary>
        public const byte SetTimeout = 0x13;

        /// <summary>Set user configuration 1 (PM wrapper command).</summary>
        public const byte SetUserCfg1 = 0x1A;

        /// <summary>Set the target work time.</summary>
        public const byte SetTWork = 0x20;

        /// <summary>Set the target horizontal distance.</summary>
        public const byte SetHorizontal = 0x21;

        /// <summary>Set the target calorie count.</summary>
        public const byte SetCalories = 0x23;

        /// <summary>Set the workout program.</summary>
        public const byte SetProgram = 0x24;

        /// <summary>Set the target power.</summary>
        public const byte SetPower = 0x34;

        /// <summary>Get the device capabilities.</summary>
        public const byte GetCaps = 0x70;
    }

    /// <summary>
    /// PM-specific short commands, wrapped inside a <see cref="Long.SetUserCfg1"/> (0x1A) frame.
    /// </summary>
    public static class PmShort
    {
        /// <summary>Get the current workout type.</summary>
        public const byte PM_GetWorkoutType = 0x89;

        /// <summary>Get the current drag factor.</summary>
        public const byte PM_GetDragFactor = 0xC1;

        /// <summary>Get the current stroke state.</summary>
        public const byte PM_GetStrokeState = 0xBF;

        /// <summary>Get the work time from the PM.</summary>
        public const byte PM_GetWorkTime = 0xA0;

        /// <summary>Get the work distance from the PM.</summary>
        public const byte PM_GetWorkDistance = 0xA3;

        /// <summary>Get the error value from the PM.</summary>
        public const byte PM_GetErrorValue = 0xC9;

        /// <summary>Get the current workout state.</summary>
        public const byte PM_GetWorkoutState = 0x8D;

        /// <summary>Get the workout interval count.</summary>
        public const byte PM_GetWorkoutIntervalCount = 0x9F;

        /// <summary>Get the current interval type.</summary>
        public const byte PM_GetIntervalType = 0x8E;

        /// <summary>Get the rest time.</summary>
        public const byte PM_GetRestTime = 0xCF;

        /// <summary>Get the display units setting.</summary>
        public const byte PM_GetDisplayUnits = 0x8B;

        /// <summary>Get the current workout number.</summary>
        public const byte PM_GetWorkoutNumber = 0x86;

        /// <summary>Get the ergometer machine type (RowErg, SkiErg, BikeErg, etc.).</summary>
        public const byte PM_GetErgMachineType = 0x87;

        /// <summary>Get the average pace.</summary>
        public const byte PM_GetAveragePace = 0xA6;
    }

    /// <summary>
    /// PM-specific long commands, wrapped inside a <see cref="Long.SetUserCfg1"/> (0x1A) frame.
    /// </summary>
    public static class PmLong
    {
        /// <summary>Set the split duration.</summary>
        public const byte PM_SetSplitDuration = 0x05;

        /// <summary>Get force plot data from the PM.</summary>
        public const byte PM_GetForcePlotData = 0x6B;

        /// <summary>Set the screen error mode.</summary>
        public const byte PM_SetScreenErrorMode = 0x27;

        /// <summary>Get heartbeat data from the PM.</summary>
        public const byte PM_GetHeartbeatData = 0x6C;

        /// <summary>Get stroke statistics from the PM.</summary>
        public const byte PM_GetStrokeStats = 0x6E;
    }
}
