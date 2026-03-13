namespace ErgNet.Models;

/// <summary>
/// Defines the type of workout being performed on the ergometer.
/// </summary>
public enum WorkoutType : byte
{
    /// <summary>A free row with no split tracking.</summary>
    JustRowNoSplits = 0,

    /// <summary>A free row with automatic split tracking.</summary>
    JustRowSplits = 1,

    /// <summary>A fixed-distance workout with no split tracking.</summary>
    FixedDistanceNoSplits = 2,

    /// <summary>A fixed-distance workout with split tracking.</summary>
    FixedDistanceSplits = 3,

    /// <summary>A fixed-time workout with no split tracking.</summary>
    FixedTimeNoSplits = 4,

    /// <summary>A fixed-time workout with split tracking.</summary>
    FixedTimeSplits = 5,

    /// <summary>An interval workout based on fixed time periods.</summary>
    FixedTimeInterval = 6,

    /// <summary>An interval workout based on fixed distances.</summary>
    FixedDistanceInterval = 7,

    /// <summary>An interval workout with variable intervals.</summary>
    VariableInterval = 8,

    /// <summary>A variable interval workout with undefined rest periods.</summary>
    VariableUndefinedRestInterval = 9,

    /// <summary>A workout targeting a fixed number of calories.</summary>
    FixedCalorie = 10,

    /// <summary>A workout targeting fixed watt-minutes.</summary>
    FixedWattMinutes = 11,

    /// <summary>An interval workout based on fixed calorie targets.</summary>
    FixedCalsInterval = 12,

    /// <summary>The total number of defined workout types.</summary>
    Num = 13,
}

/// <summary>
/// Represents the current state of the workout session.
/// </summary>
public enum WorkoutState : byte
{
    /// <summary>Waiting for the user to begin rowing.</summary>
    WaitToBegin = 0,

    /// <summary>The user is actively rowing during the workout.</summary>
    WorkoutRow = 1,

    /// <summary>Countdown pause before the workout starts.</summary>
    CountdownPause = 2,

    /// <summary>Resting between intervals.</summary>
    IntervalRest = 3,

    /// <summary>Active work phase of a time-based interval.</summary>
    IntervalWorktime = 4,

    /// <summary>Active work phase of a distance-based interval.</summary>
    IntervalWorkDistance = 5,

    /// <summary>Transitioning from interval rest to a time-based work phase.</summary>
    IntervalRestEndToWorktime = 6,

    /// <summary>Transitioning from interval rest to a distance-based work phase.</summary>
    IntervalRestEndToWorkDistance = 7,

    /// <summary>Transitioning from a time-based work phase to rest.</summary>
    IntervalWorkTimeToRest = 8,

    /// <summary>Transitioning from a distance-based work phase to rest.</summary>
    IntervalWorkDistanceToRest = 9,

    /// <summary>The workout has ended.</summary>
    WorkoutEnd = 10,

    /// <summary>The workout is being terminated.</summary>
    Terminate = 11,

    /// <summary>The workout has been logged to the device memory.</summary>
    WorkoutLogged = 12,

    /// <summary>The device is rearming for the next workout.</summary>
    Rearm = 13,
}

/// <summary>
/// Indicates whether the rower is actively rowing.
/// </summary>
public enum RowingState : byte
{
    /// <summary>The rower is not actively rowing.</summary>
    Inactive = 0,

    /// <summary>The rower is actively rowing.</summary>
    Active = 1,
}

/// <summary>
/// Represents the current phase of the rowing stroke cycle.
/// </summary>
public enum StrokeState : byte
{
    /// <summary>Waiting for the flywheel to reach minimum speed.</summary>
    WaitingForWheelToReachMinSpeed = 0,

    /// <summary>Waiting for the flywheel to accelerate.</summary>
    WaitingForWheelToAccelerate = 1,

    /// <summary>The drive phase of the stroke (pulling the handle).</summary>
    Driving = 2,

    /// <summary>The dwell phase between drive and recovery.</summary>
    Dwelling = 3,

    /// <summary>The recovery phase of the stroke (returning the handle).</summary>
    Recovery = 4,
}

/// <summary>
/// Defines the type of interval being performed.
/// </summary>
public enum IntervalType : byte
{
    /// <summary>A time-based interval.</summary>
    Time = 0,

    /// <summary>A distance-based interval.</summary>
    Distance = 1,

    /// <summary>A rest interval.</summary>
    Rest = 2,

    /// <summary>A time-based interval with undefined rest.</summary>
    TimeRestUndefined = 3,

    /// <summary>A distance-based interval with undefined rest.</summary>
    DistanceRestUndefined = 4,

    /// <summary>An undefined rest interval.</summary>
    RestUndefined = 5,

    /// <summary>A calorie-based interval.</summary>
    Calories = 6,

    /// <summary>A calorie-based interval with undefined rest.</summary>
    CaloriesRestUndefined = 7,

    /// <summary>A watt-minutes-based interval.</summary>
    WattMinutes = 8,

    /// <summary>A watt-minutes-based interval with undefined rest.</summary>
    WattMinutesRestUndefined = 9,

    /// <summary>No interval type defined.</summary>
    None = 255,
}

/// <summary>
/// Specifies the unit of measurement for workout duration.
/// </summary>
public enum DurationType : byte
{
    /// <summary>Duration measured in time.</summary>
    Time = 0,

    /// <summary>Duration measured in calories.</summary>
    Calories = 0x40,

    /// <summary>Duration measured in distance.</summary>
    Distance = 0x80,

    /// <summary>Duration measured in watts.</summary>
    Watts = 0xC0,
}

/// <summary>
/// Represents the overall state of the performance monitor machine.
/// </summary>
public enum MachineState : byte
{
    /// <summary>The machine is in an error state.</summary>
    Error = 0,

    /// <summary>The machine is ready and waiting.</summary>
    Ready = 1,

    /// <summary>The machine is idle.</summary>
    Idle = 2,

    /// <summary>The machine has an assigned identifier.</summary>
    HaveId = 3,

    /// <summary>The machine is currently in use.</summary>
    InUse = 5,

    /// <summary>The machine is paused.</summary>
    Paused = 6,

    /// <summary>The machine has finished a workout.</summary>
    Finished = 7,

    /// <summary>The machine is in manual mode.</summary>
    Manual = 8,

    /// <summary>The machine is offline.</summary>
    OffLine = 9,
}

/// <summary>
/// Identifies the type of ErgNet Performance Monitor hardware.
/// </summary>
public enum MonitorType : byte
{
    /// <summary>Performance Monitor 3. Supports USB only.</summary>
    PM3 = 3,

    /// <summary>Performance Monitor 4. Supports USB only.</summary>
    PM4 = 4,

    /// <summary>Performance Monitor 5. Supports USB, Bluetooth, and ANT+.</summary>
    PM5 = 5,
}

/// <summary>
/// Identifies the type of ErgNet ergometer machine.
/// </summary>
public enum ErgMachineType : byte
{
    /// <summary>Static indoor rower (RowErg / Model D / Model E).</summary>
    StaticRower = 0,

    /// <summary>Indoor cross-country ski trainer (SkiErg).</summary>
    SkiErg = 1,

    /// <summary>Indoor stationary bike (BikeErg).</summary>
    BikeErg = 2,

    /// <summary>Dynamic indoor rower with slides.</summary>
    Dynamic = 3,

    /// <summary>Unknown or unrecognised ergometer type.</summary>
    Unknown = 255,
}
