namespace Concept2.Models;

/// <summary>
/// Represents consolidated real-time rowing data from the performance monitor.
/// </summary>
public sealed class RowingData
{
    /// <summary>Gets the elapsed time since the workout started.</summary>
    public TimeSpan ElapsedTime { get; init; }

    /// <summary>Gets the total distance rowed in meters.</summary>
    public double DistanceMeters { get; init; }

    /// <summary>Gets the current stroke rate in strokes per minute.</summary>
    public int StrokeRate { get; init; }

    /// <summary>Gets the current heart rate in beats per minute.</summary>
    public int HeartRate { get; init; }

    /// <summary>Gets the current pace as time per 500 meters.</summary>
    public TimeSpan CurrentPace { get; init; }

    /// <summary>Gets the average pace as time per 500 meters.</summary>
    public TimeSpan AveragePace { get; init; }

    /// <summary>Gets the average power output in watts.</summary>
    public int AveragePowerWatts { get; init; }

    /// <summary>Gets the total calories burned during the workout.</summary>
    public int TotalCalories { get; init; }

    /// <summary>Gets the current speed in meters per second.</summary>
    public double SpeedMetersPerSecond { get; init; }

    /// <summary>Gets the current state of the workout session.</summary>
    public WorkoutState WorkoutState { get; init; }

    /// <summary>Gets whether the rower is actively rowing.</summary>
    public RowingState RowingState { get; init; }

    /// <summary>Gets the current phase of the rowing stroke cycle.</summary>
    public StrokeState StrokeState { get; init; }

    /// <summary>Gets the current drag factor of the flywheel.</summary>
    public int DragFactor { get; init; }

    /// <summary>Gets the timestamp when this data was captured.</summary>
    public DateTimeOffset Timestamp { get; init; }
}
