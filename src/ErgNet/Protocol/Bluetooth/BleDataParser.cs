using ErgNet.Models;

namespace ErgNet.Protocol.Bluetooth;

/// <summary>
/// Parsed general status data from the ErgNet BLE General Status characteristic.
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
/// Parsed additional status data from the ErgNet BLE Additional Status characteristic.
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
/// Provides methods for parsing raw BLE notification data from ErgNet rowing characteristics.
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

        // Validate enum values before casting
        var workoutType = Enum.IsDefined(typeof(WorkoutType), data[6])
            ? (WorkoutType)data[6]
            : throw new ArgumentException($"Invalid WorkoutType value: {data[6]}", nameof(data));

        var intervalType = Enum.IsDefined(typeof(IntervalType), data[7])
            ? (IntervalType)data[7]
            : throw new ArgumentException($"Invalid IntervalType value: {data[7]}", nameof(data));

        var workoutState = Enum.IsDefined(typeof(WorkoutState), data[8])
            ? (WorkoutState)data[8]
            : throw new ArgumentException($"Invalid WorkoutState value: {data[8]}", nameof(data));

        var rowingState = Enum.IsDefined(typeof(RowingState), data[9])
            ? (RowingState)data[9]
            : throw new ArgumentException($"Invalid RowingState value: {data[9]}", nameof(data));

        var strokeState = Enum.IsDefined(typeof(StrokeState), data[10])
            ? (StrokeState)data[10]
            : throw new ArgumentException($"Invalid StrokeState value: {data[10]}", nameof(data));

        var durationType = Enum.IsDefined(typeof(DurationType), data[14])
            ? (DurationType)data[14]
            : throw new ArgumentException($"Invalid DurationType value: {data[14]}", nameof(data));

        return new GeneralStatusData(
            ElapsedTime: TimeSpan.FromMilliseconds(elapsedTimeCentiseconds * 10.0),
            DistanceMeters: distanceTenths / 10.0,
            WorkoutType: workoutType,
            IntervalType: intervalType,
            WorkoutState: workoutState,
            RowingState: rowingState,
            StrokeState: strokeState,
            TotalWorkDistanceMeters: totalWorkDistance,
            WorkoutDurationType: durationType,
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
        var speedThousandths = (uint)data[3] | ((uint)data[4] << 8);
        var currentPaceCentiseconds = (uint)data[7] | ((uint)data[8] << 8);
        var averagePaceCentiseconds = (uint)data[9] | ((uint)data[10] << 8);
        var restDistance = (int)((uint)data[11] | ((uint)data[12] << 8));
        var restTimeCentiseconds = ReadUInt24(data[13..]);
        var averagePower = (int)((uint)data[16] | ((uint)data[17] << 8));

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
        return (uint)data[0] | ((uint)data[1] << 8) | ((uint)data[2] << 16);
    }
}
