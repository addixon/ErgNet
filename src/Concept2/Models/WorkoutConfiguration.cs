namespace Concept2.Models;

/// <summary>
/// Configuration for setting up a workout on the performance monitor.
/// </summary>
public sealed class WorkoutConfiguration
{
    /// <summary>Gets the type of workout to configure.</summary>
    public WorkoutType WorkoutType { get; init; } = WorkoutType.JustRowNoSplits;

    /// <summary>Gets the target time for fixed-time workouts.</summary>
    public TimeSpan? TargetTime { get; init; }

    /// <summary>Gets the target distance in meters for fixed-distance workouts.</summary>
    public int? TargetDistanceMeters { get; init; }

    /// <summary>Gets the target calories for fixed-calorie workouts.</summary>
    public int? TargetCalories { get; init; }

    /// <summary>Gets the time duration of each split for time-based workouts.</summary>
    public TimeSpan? SplitLength { get; init; }

    /// <summary>Gets the distance in meters of each split for distance-based workouts.</summary>
    public int? SplitDistanceMeters { get; init; }

    /// <summary>Gets the list of interval configurations for interval workouts.</summary>
    public IReadOnlyList<IntervalConfiguration>? Intervals { get; init; }

    /// <summary>
    /// Validates the workout configuration based on the specified workout type.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when the configuration is invalid for the specified workout type.
    /// </exception>
    public void Validate()
    {
        switch (WorkoutType)
        {
            case WorkoutType.FixedTimeNoSplits:
            case WorkoutType.FixedTimeSplits:
                if (TargetTime is null)
                {
                    throw new ArgumentException(
                        "TargetTime is required for fixed-time workouts.",
                        nameof(TargetTime));
                }

                break;

            case WorkoutType.FixedDistanceNoSplits:
            case WorkoutType.FixedDistanceSplits:
                if (TargetDistanceMeters is null)
                {
                    throw new ArgumentException(
                        "TargetDistanceMeters is required for fixed-distance workouts.",
                        nameof(TargetDistanceMeters));
                }

                break;

            case WorkoutType.FixedCalorie:
            case WorkoutType.FixedCalsInterval:
                if (TargetCalories is null)
                {
                    throw new ArgumentException(
                        "TargetCalories is required for fixed-calorie workouts.",
                        nameof(TargetCalories));
                }

                break;

            case WorkoutType.FixedTimeInterval:
            case WorkoutType.FixedDistanceInterval:
            case WorkoutType.VariableInterval:
            case WorkoutType.VariableUndefinedRestInterval:
                if (Intervals is null || Intervals.Count == 0)
                {
                    throw new ArgumentException(
                        "Intervals are required for interval workouts.",
                        nameof(Intervals));
                }

                break;
        }
    }
}

/// <summary>
/// Configuration for a single interval within an interval workout.
/// </summary>
public sealed class IntervalConfiguration
{
    /// <summary>Gets the type of interval.</summary>
    public IntervalType Type { get; init; }

    /// <summary>Gets the duration for time-based intervals.</summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>Gets the target distance in meters for distance-based intervals.</summary>
    public int? DistanceMeters { get; init; }

    /// <summary>Gets the target calories for calorie-based intervals.</summary>
    public int? Calories { get; init; }

    /// <summary>Gets the rest duration between intervals.</summary>
    public TimeSpan? RestDuration { get; init; }
}
