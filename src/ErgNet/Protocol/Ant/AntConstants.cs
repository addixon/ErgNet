namespace ErgNet.Protocol.Ant;

/// <summary>
/// Constants for ANT+ Fitness Equipment (FE-C) communication with the ErgNet PM5.
/// The PM5 broadcasts workout data using the ANT+ Fitness Equipment device profile.
/// ANT+ protocols are defined by Garmin/Dynastream and documented at
/// <see href="https://www.thisisant.com"/>.
/// </summary>
public static class AntConstants
{
    // ── Channel Configuration ────────────────────────────────────────────

    /// <summary>ANT+ device type for Fitness Equipment (FE-C).</summary>
    public const byte FitnessEquipmentDeviceType = 17;

    /// <summary>
    /// Channel period for Fitness Equipment profile (8192 counts ≈ 4 Hz).
    /// ANT+ channel period = 32768 / message_rate_hz. For 4 Hz: 32768 / 4 = 8192.
    /// </summary>
    public const ushort ChannelPeriod = 8192;

    /// <summary>RF frequency for ANT+ (2457 MHz, standard ANT+ public network frequency).</summary>
    public const byte RfFrequency = 57;

    /// <summary>Default transmission type for ANT+ Fitness Equipment.</summary>
    public const byte TransmissionType = 5;

    /// <summary>ANT+ network key index (public ANT+ network).</summary>
    public const byte NetworkNumber = 0;

    // ── Data Page Numbers ────────────────────────────────────────────────

    /// <summary>General FE Data page (0x10) — broadcast by all fitness equipment types.</summary>
    public const byte GeneralFEDataPage = 0x10;

    /// <summary>General Settings page (0x11) — equipment settings and configuration.</summary>
    public const byte GeneralSettingsPage = 0x11;

    /// <summary>General Metabolic Data page (0x12) — caloric and metabolic data.</summary>
    public const byte GeneralMetabolicDataPage = 0x12;

    /// <summary>Rower-specific Data page (0x16) — stroke count, cadence, and power.</summary>
    public const byte RowerDataPage = 0x16;

    /// <summary>Nordic Skier Data page (0x18) — stride count and cadence for SkiErg.</summary>
    public const byte NordicSkierDataPage = 0x18;

    // ── Equipment Type Identifiers ───────────────────────────────────────

    /// <summary>Equipment type identifier for Rower (RowErg).</summary>
    public const byte EquipmentTypeRower = 0x16;

    /// <summary>Equipment type identifier for Nordic Skier (SkiErg).</summary>
    public const byte EquipmentTypeNordicSkier = 0x18;

    // ── Data Page Layout ─────────────────────────────────────────────────

    /// <summary>Standard ANT+ data page payload size in bytes.</summary>
    public const int DataPageSize = 8;

    /// <summary>Index of the data page number byte within a data page.</summary>
    public const int DataPageNumberIndex = 0;

    // ── Rollover Constants ───────────────────────────────────────────────

    /// <summary>
    /// Maximum value for 8-bit rollover counters (stroke count, distance, calorie increment).
    /// ANT+ uses unsigned 8-bit counters that roll over at 256.
    /// </summary>
    public const int RolloverCounterMax = 256;
}
