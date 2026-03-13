namespace ErgNet.Protocol.Bluetooth;

/// <summary>
/// Contains Bluetooth Low Energy (BLE) UUIDs and constants for the ErgNet performance monitor.
/// </summary>
public static class BleConstants
{
    /// <summary>The base UUID for all ErgNet BLE services and characteristics.</summary>
    public static readonly Guid BaseUuid = new("CE060000-43E5-11E4-916C-0800200C9A66");

    // ── Services ──────────────────────────────────────────────────────────

    /// <summary>The Device Information service UUID.</summary>
    public static readonly Guid DeviceInfoService = new("CE060010-43E5-11E4-916C-0800200C9A66");

    /// <summary>The Rowing Control service UUID used for CSAFE command communication.</summary>
    public static readonly Guid RowingControlService = new("CE060020-43E5-11E4-916C-0800200C9A66");

    /// <summary>The Rowing Primary service UUID for real-time rowing data notifications.</summary>
    public static readonly Guid RowingPrimaryService = new("CE060030-43E5-11E4-916C-0800200C9A66");

    // ── Device Info Characteristics ───────────────────────────────────────

    /// <summary>The Serial Number characteristic UUID.</summary>
    public static readonly Guid SerialNumber = new("CE060012-43E5-11E4-916C-0800200C9A66");

    /// <summary>The Hardware Revision characteristic UUID.</summary>
    public static readonly Guid HardwareRevision = new("CE060013-43E5-11E4-916C-0800200C9A66");

    /// <summary>The Firmware Revision characteristic UUID.</summary>
    public static readonly Guid FirmwareRevision = new("CE060014-43E5-11E4-916C-0800200C9A66");

    /// <summary>The Manufacturer Name characteristic UUID.</summary>
    public static readonly Guid ManufacturerName = new("CE060015-43E5-11E4-916C-0800200C9A66");

    // ── Control Characteristics ──────────────────────────────────────────

    /// <summary>The PM Receive characteristic UUID (Write) for sending CSAFE commands to the device.</summary>
    public static readonly Guid PmReceive = new("CE060021-43E5-11E4-916C-0800200C9A66");

    /// <summary>The PM Transmit characteristic UUID (Notify) for receiving CSAFE responses from the device.</summary>
    public static readonly Guid PmTransmit = new("CE060022-43E5-11E4-916C-0800200C9A66");

    // ── Rowing Data Characteristics ──────────────────────────────────────

    /// <summary>The General Status characteristic UUID providing core workout metrics.</summary>
    public static readonly Guid GeneralStatus = new("CE060031-43E5-11E4-916C-0800200C9A66");

    /// <summary>The Additional Status characteristic UUID providing supplementary workout metrics.</summary>
    public static readonly Guid AdditionalStatus = new("CE060032-43E5-11E4-916C-0800200C9A66");

    /// <summary>The Split/Interval Data characteristic UUID.</summary>
    public static readonly Guid SplitIntervalData = new("CE060037-43E5-11E4-916C-0800200C9A66");

    /// <summary>The Split/Interval Data 2 characteristic UUID with additional interval details.</summary>
    public static readonly Guid SplitIntervalData2 = new("CE060038-43E5-11E4-916C-0800200C9A66");

    /// <summary>The End of Workout Summary characteristic UUID.</summary>
    public static readonly Guid EndOfWorkoutSummary = new("CE060039-43E5-11E4-916C-0800200C9A66");

    /// <summary>The End of Workout Summary 2 characteristic UUID with additional summary details.</summary>
    public static readonly Guid EndOfWorkoutSummary2 = new("CE06003A-43E5-11E4-916C-0800200C9A66");
}
