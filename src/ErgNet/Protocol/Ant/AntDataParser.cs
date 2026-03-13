namespace ErgNet.Protocol.Ant;

/// <summary>
/// Parsed General FE Data from ANT+ data page 0x10.
/// This page is broadcast by all fitness equipment types and contains
/// core real-time metrics.
/// </summary>
/// <param name="EquipmentType">The ANT+ equipment type identifier.</param>
/// <param name="ElapsedTimeIncrement">Elapsed time increment in seconds (0.25 s resolution, rolls over at 64 s).</param>
/// <param name="DistanceIncrement">Distance increment in meters (rolls over at 256 m). Only valid when <paramref name="DistanceEnabled"/> is <c>true</c>.</param>
/// <param name="InstantaneousSpeed">Instantaneous speed in meters per second.</param>
/// <param name="HeartRate">Instantaneous heart rate in beats per minute. 0xFF indicates invalid.</param>
/// <param name="DistanceEnabled">Whether distance accumulation is enabled.</param>
/// <param name="State">The fitness equipment state.</param>
public readonly record struct GeneralFEData(
    byte EquipmentType,
    double ElapsedTimeIncrement,
    byte DistanceIncrement,
    double InstantaneousSpeed,
    byte HeartRate,
    bool DistanceEnabled,
    AntEquipmentState State);

/// <summary>
/// Parsed Rower Data from ANT+ data page 0x16.
/// This page is specific to rowing ergometers and contains
/// stroke count, cadence, and instantaneous power.
/// </summary>
/// <param name="StrokeCountIncrement">Stroke count increment (rolls over at 256).</param>
/// <param name="Cadence">Cadence in strokes per minute. 0xFF indicates invalid.</param>
/// <param name="InstantaneousPower">Instantaneous power in watts. 0xFFFF indicates invalid.</param>
/// <param name="State">The fitness equipment state.</param>
public readonly record struct RowerData(
    byte StrokeCountIncrement,
    byte Cadence,
    int InstantaneousPower,
    AntEquipmentState State);

/// <summary>
/// Parsed General Metabolic Data from ANT+ data page 0x12.
/// </summary>
/// <param name="InstantaneousMET">Instantaneous metabolic equivalent (0.01 resolution).</param>
/// <param name="CaloricBurnRate">Caloric burn rate in kilocalories per hour (0.1 resolution).</param>
/// <param name="CaloricIncrement">Accumulated calorie increment (rolls over at 256).</param>
public readonly record struct MetabolicData(
    double InstantaneousMET,
    double CaloricBurnRate,
    byte CaloricIncrement);

/// <summary>
/// Parsed Nordic Skier Data from ANT+ data page 0x18.
/// This page is specific to the SkiErg and contains stride count, cadence, and instantaneous power.
/// </summary>
/// <param name="StrideCountIncrement">Stride count increment (rolls over at 256).</param>
/// <param name="Cadence">Cadence in strides per minute. 0xFF indicates invalid.</param>
/// <param name="InstantaneousPower">Instantaneous power in watts. 0xFFFF indicates invalid.</param>
/// <param name="State">The fitness equipment state.</param>
public readonly record struct NordicSkierData(
    byte StrideCountIncrement,
    byte Cadence,
    int InstantaneousPower,
    AntEquipmentState State);

/// <summary>
/// Represents the state of the fitness equipment as reported in ANT+ data pages.
/// </summary>
public enum AntEquipmentState : byte
{
    /// <summary>State is unknown or not reported.</summary>
    Unknown = 0,

    /// <summary>Equipment is asleep or powered off.</summary>
    AsleepOrOff = 1,

    /// <summary>Equipment is ready for use.</summary>
    Ready = 2,

    /// <summary>Equipment is currently in use.</summary>
    InUse = 3,

    /// <summary>Equipment has finished or is paused.</summary>
    FinishedOrPaused = 4,
}

/// <summary>
/// Provides methods for parsing raw ANT+ Fitness Equipment data pages
/// broadcast by the ErgNet PM5 performance monitor.
/// </summary>
public static class AntDataParser
{
    /// <summary>
    /// Parses a General FE Data page (0x10).
    /// </summary>
    /// <param name="data">The raw 8-byte ANT+ data page payload.</param>
    /// <returns>A <see cref="GeneralFEData"/> containing the parsed values.</returns>
    /// <exception cref="ArgumentException">Thrown when the data is shorter than 8 bytes.</exception>
    public static GeneralFEData ParseGeneralFEData(ReadOnlySpan<byte> data)
    {
        ValidatePageLength(data, AntConstants.DataPageSize);

        byte equipmentType = data[1];
        double elapsedTimeIncrement = data[2] * 0.25;
        byte distanceIncrement = data[3];
        double instantaneousSpeed = ((uint)data[4] | ((uint)data[5] << 8)) * 0.001;
        byte heartRate = data[6];
        bool distanceEnabled = (data[7] & 0x04) != 0;
        var state = ParseState(data[7]);

        return new GeneralFEData(
            EquipmentType: equipmentType,
            ElapsedTimeIncrement: elapsedTimeIncrement,
            DistanceIncrement: distanceIncrement,
            InstantaneousSpeed: instantaneousSpeed,
            HeartRate: heartRate,
            DistanceEnabled: distanceEnabled,
            State: state);
    }

    /// <summary>
    /// Parses a Rower Data page (0x16).
    /// </summary>
    /// <param name="data">The raw 8-byte ANT+ data page payload.</param>
    /// <returns>A <see cref="RowerData"/> containing the parsed values.</returns>
    /// <exception cref="ArgumentException">Thrown when the data is shorter than 8 bytes.</exception>
    public static RowerData ParseRowerData(ReadOnlySpan<byte> data)
    {
        ValidatePageLength(data, AntConstants.DataPageSize);

        byte strokeCountIncrement = data[3];
        byte cadence = data[4];
        int instantaneousPower = (int)((uint)data[5] | ((uint)data[6] << 8));
        var state = ParseState(data[7]);

        return new RowerData(
            StrokeCountIncrement: strokeCountIncrement,
            Cadence: cadence,
            InstantaneousPower: instantaneousPower,
            State: state);
    }

    /// <summary>
    /// Parses a General Metabolic Data page (0x12).
    /// </summary>
    /// <param name="data">The raw 8-byte ANT+ data page payload.</param>
    /// <returns>A <see cref="MetabolicData"/> containing the parsed values.</returns>
    /// <exception cref="ArgumentException">Thrown when the data is shorter than 8 bytes.</exception>
    public static MetabolicData ParseMetabolicData(ReadOnlySpan<byte> data)
    {
        ValidatePageLength(data, AntConstants.DataPageSize);

        double instantaneousMET = ((uint)data[2] | ((uint)data[3] << 8)) * 0.01;
        double caloricBurnRate = ((uint)data[4] | ((uint)data[5] << 8)) * 0.1;
        byte caloricIncrement = data[6];

        return new MetabolicData(
            InstantaneousMET: instantaneousMET,
            CaloricBurnRate: caloricBurnRate,
            CaloricIncrement: caloricIncrement);
    }

    /// <summary>
    /// Parses a Nordic Skier Data page (0x18).
    /// </summary>
    /// <param name="data">The raw 8-byte ANT+ data page payload.</param>
    /// <returns>A <see cref="NordicSkierData"/> containing the parsed values.</returns>
    /// <exception cref="ArgumentException">Thrown when the data is shorter than 8 bytes.</exception>
    public static NordicSkierData ParseNordicSkierData(ReadOnlySpan<byte> data)
    {
        ValidatePageLength(data, AntConstants.DataPageSize);

        byte strideCountIncrement = data[3];
        byte cadence = data[4];
        int instantaneousPower = (int)((uint)data[5] | ((uint)data[6] << 8));
        var state = ParseState(data[7]);

        return new NordicSkierData(
            StrideCountIncrement: strideCountIncrement,
            Cadence: cadence,
            InstantaneousPower: instantaneousPower,
            State: state);
    }

    /// <summary>
    /// Extracts the data page number from an ANT+ data page payload.
    /// </summary>
    /// <param name="data">The raw ANT+ data page payload.</param>
    /// <returns>The data page number byte.</returns>
    /// <exception cref="ArgumentException">Thrown when the data is empty.</exception>
    public static byte GetDataPageNumber(ReadOnlySpan<byte> data)
    {
        if (data.Length < 1)
        {
            throw new ArgumentException("Data page must contain at least 1 byte.", nameof(data));
        }

        return data[AntConstants.DataPageNumberIndex];
    }

    /// <summary>
    /// Extracts and validates the equipment state from the state byte.
    /// The state is encoded in bits 4-6 of the byte (mask 0x70).
    /// </summary>
    /// <param name="stateByte">The raw state byte from the ANT+ data page.</param>
    /// <returns>The parsed equipment state, or Unknown if the value is undefined.</returns>
    private static AntEquipmentState ParseState(byte stateByte)
    {
        // Equipment state is in bits 4-6 (0x70), right-shifted by 4 positions
        var stateValue = (byte)((stateByte >> 4) & 0x07);
        return Enum.IsDefined(typeof(AntEquipmentState), stateValue)
            ? (AntEquipmentState)stateValue
            : AntEquipmentState.Unknown;
    }

    private static void ValidatePageLength(ReadOnlySpan<byte> data, int minLength)
    {
        if (data.Length < minLength)
        {
            throw new ArgumentException(
                $"ANT+ data page must be at least {minLength} bytes, but was {data.Length}.",
                nameof(data));
        }
    }
}
