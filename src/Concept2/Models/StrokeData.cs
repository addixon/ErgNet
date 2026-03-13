namespace Concept2.Models;

/// <summary>
/// Represents detailed data for a single rowing stroke.
/// </summary>
public sealed class StrokeData
{
    /// <summary>Gets the cumulative stroke count.</summary>
    public int StrokeCount { get; init; }

    /// <summary>Gets the duration of the drive phase in seconds.</summary>
    public double DriveTimeSeconds { get; init; }

    /// <summary>Gets the duration of the recovery phase in seconds.</summary>
    public double RecoveryTimeSeconds { get; init; }

    /// <summary>Gets the length of the stroke in meters.</summary>
    public double StrokeLengthMeters { get; init; }

    /// <summary>Gets the peak force applied during the stroke in newtons.</summary>
    public double PeakForceNewtons { get; init; }

    /// <summary>Gets the average force applied during the stroke in newtons.</summary>
    public double AverageForceNewtons { get; init; }

    /// <summary>Gets the work performed during the stroke in joules.</summary>
    public double WorkPerStrokeJoules { get; init; }

    /// <summary>Gets the impulse force of the stroke in newton-seconds.</summary>
    public double ImpulseForceNewtonSeconds { get; init; }

    /// <summary>Gets the force curve data points for the stroke, if available.</summary>
    public int[]? ForceCurve { get; init; }

    /// <summary>Gets the timestamp when this stroke data was captured.</summary>
    public DateTimeOffset Timestamp { get; init; }
}
