using Concept2.Models;

namespace Concept2.Protocol.Bluetooth;

/// <summary>
/// Parsed general status data from the Concept2 BLE General Status characteristic.
/// </summary>
/// <param name="ElapsedTime">The elapsed workout time.</param>
/// <param name="DistanceMeters">The distance rowed in meters.</param>
/// <param name="WorkoutType">The type of workout being performed.</param>
/// <param name="IntervalType">The type of the current interval.</param>
/// <param name="WorkoutState">The current workout state.</param>
/// <param name="RowingState">Whether the rower is active or inactive.</param>
/// <param name="StrokeState">The current phase of the stroke cycle.</param>
/// <param name="TotalWorkDistanceMeters">The total work distance in meters.</param>
/// <param name="WorkoutDurationType">The unit of measurement for the workout duration.</param>
/// <param name="WorkoutDuration">The raw workout duration value in the units specified by <paramref name="WorkoutDurationType"/>.</param>
public readonly record struct GeneralStatusData(
    TimeSpan ElapsedTime,
    double DistanceMeters,
    WorkoutType WorkoutType,
    IntervalType IntervalType,
    WorkoutState WorkoutState,
    RowingState RowingState,
    StrokeState StrokeState,
    double TotalWorkDistanceMeters,
    DurationType WorkoutDurationType,
    uint WorkoutDuration);

/// <summary>
/// Parsed additional status data from the Concept2 BLE Additional Status characteristic.
/// </summary>
/// <param name="ElapsedTime">The elapsed workout time.</param>
/// <param name="SpeedMetersPerSecond">The current rowing speed in meters per second.</param>
/// <param name="StrokeRate">The current stroke rate in strokes per minute.</param>
/// <param name="HeartRate">The current heart rate in beats per minute.</param>
/// <param name="CurrentPace">The current pace as time per 500 meters.</param>
/// <param name="AveragePace">The average pace as time per 500 meters.</param>
/// <param name="RestDistanceMeters">The remaining rest distance in meters.</param>
/// <param name="RestTime">The remaining rest time.</param>
/// <param name="AveragePowerWatts">The average power output in watts.</param>
public readonly record struct AdditionalStatusData(
    TimeSpan ElapsedTime,
    double SpeedMetersPerSecond,
    int StrokeRate,
    int HeartRate,
    TimeSpan CurrentPace,
    TimeSpan AveragePace,
    int RestDistanceMeters,
    TimeSpan RestTime,
    int AveragePowerWatts);

/// <summary>
/// Provides methods for parsing raw BLE notification data from Concept2 rowing characteristics.
/// </summary>
public static class BleDataParser
{
    private const int GeneralStatusMinLength = 18;
    private const int AdditionalStatusMinLength = 18;

    /// <summary>
    /// Parses raw BLE data from the General Status characteristic (CE060031).
    /// </summary>
    /// <param name="data">The raw byte data received from the BLE notification.</param>
    /// <returns>A <see cref="GeneralStatusData"/> containing the parsed values.</returns>
    /// <exception cref="ArgumentException">Thrown when the data is shorter than the expected 18 bytes.</exception>
    public static GeneralStatusData ParseGeneralStatus(ReadOnlySpan<byte> data)
    {
        if (data.Length < GeneralStatusMinLength)
        {
            throw new ArgumentException(
                $"General status data must be at least {GeneralStatusMinLength} bytes, but was {data.Length}.",
                nameof(data));
        }

        var elapsedTimeCentiseconds = ReadUInt24(data);
        var distanceTenths = ReadUInt24(data[3..]);

        var totalWorkDistance = ReadUInt24(data[11..]);
        var workoutDuration = ReadUInt24(data[15..]);

        return new GeneralStatusData(
            ElapsedTime: TimeSpan.FromMilliseconds(elapsedTimeCentiseconds * 10.0),
            DistanceMeters: distanceTenths / 10.0,
            WorkoutType: (WorkoutType)data[6],
            IntervalType: (IntervalType)data[7],
            WorkoutState: (WorkoutState)data[8],
            RowingState: (RowingState)data[9],
            StrokeState: (StrokeState)data[10],
            TotalWorkDistanceMeters: totalWorkDistance,
            WorkoutDurationType: (DurationType)data[14],
            WorkoutDuration: workoutDuration);
    }

    /// <summary>
    /// Parses raw BLE data from the Additional Status characteristic (CE060032).
    /// </summary>
    /// <param name="data">The raw byte data received from the BLE notification.</param>
    /// <returns>An <see cref="AdditionalStatusData"/> containing the parsed values.</returns>
    /// <exception cref="ArgumentException">Thrown when the data is shorter than the expected 18 bytes.</exception>
    public static AdditionalStatusData ParseAdditionalStatus(ReadOnlySpan<byte> data)
    {
        if (data.Length < AdditionalStatusMinLength)
        {
            throw new ArgumentException(
                $"Additional status data must be at least {AdditionalStatusMinLength} bytes, but was {data.Length}.",
                nameof(data));
        }

        var elapsedTimeCentiseconds = ReadUInt24(data);
        var speedThousandths = (uint)(data[3] | (data[4] << 8));
        var currentPaceCentiseconds = (uint)(data[7] | (data[8] << 8));
        var averagePaceCentiseconds = (uint)(data[9] | (data[10] << 8));
        var restDistance = (int)(data[11] | (data[12] << 8));
        var restTimeCentiseconds = ReadUInt24(data[13..]);
        var averagePower = (int)(data[16] | (data[17] << 8));

        return new AdditionalStatusData(
            ElapsedTime: TimeSpan.FromMilliseconds(elapsedTimeCentiseconds * 10.0),
            SpeedMetersPerSecond: speedThousandths / 1000.0,
            StrokeRate: data[5],
            HeartRate: data[6],
            CurrentPace: TimeSpan.FromMilliseconds(currentPaceCentiseconds * 10.0),
            AveragePace: TimeSpan.FromMilliseconds(averagePaceCentiseconds * 10.0),
            RestDistanceMeters: restDistance,
            RestTime: TimeSpan.FromMilliseconds(restTimeCentiseconds * 10.0),
            AveragePowerWatts: averagePower);
    }

    /// <summary>
    /// Reads a 24-bit unsigned integer (little-endian) from the given byte span.
    /// </summary>
    private static uint ReadUInt24(ReadOnlySpan<byte> data)
    {
        return (uint)(data[0] | (data[1] << 8) | (data[2] << 16));
    }
}
