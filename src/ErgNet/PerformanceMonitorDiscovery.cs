using ErgNet.Protocol.Ant;
using ErgNet.Protocol.Bluetooth;

namespace ErgNet;

/// <summary>
/// Helper for discovering Concept2 Performance Monitors connected via USB, Bluetooth, or ANT+.
/// </summary>
public static class PerformanceMonitorDiscovery
{
    /// <summary>Concept2 USB Vendor ID.</summary>
    public const int UsbVendorId = 0x17A4;

    /// <summary>Concept2 USB Product ID.</summary>
    public const int UsbProductId = 0x0001;

    /// <summary>The Concept2 BLE base UUID used for device identification in scan results.</summary>
    public static readonly Guid BleBaseUuid = new("CE060000-43E5-11E4-916C-0800200C9A66");

    /// <summary>The ANT+ Fitness Equipment device type used by the PM5.</summary>
    public const byte AntFitnessEquipmentDeviceType = AntConstants.FitnessEquipmentDeviceType;

    /// <summary>
    /// Checks if a BLE device is a Concept2 Performance Monitor by checking
    /// the advertised service UUIDs against known Concept2 services.
    /// </summary>
    /// <param name="advertisedServiceUuids">The service UUIDs advertised by the BLE device.</param>
    /// <returns><see langword="true"/> if the device appears to be a Concept2 PM; otherwise <see langword="false"/>.</returns>
    public static bool IsConcept2Device(IEnumerable<Guid> advertisedServiceUuids)
    {
        ArgumentNullException.ThrowIfNull(advertisedServiceUuids);

        return advertisedServiceUuids.Any(uuid =>
            uuid == BleConstants.RowingPrimaryService ||
            uuid == BleConstants.RowingControlService ||
            uuid == BleConstants.DeviceInfoService);
    }

    /// <summary>
    /// Checks if an ANT+ device is a Concept2 Performance Monitor by comparing
    /// its device type against the ANT+ Fitness Equipment type.
    /// </summary>
    /// <param name="deviceType">The ANT+ device type reported during channel assignment.</param>
    /// <returns><see langword="true"/> if the device type matches Fitness Equipment (PM5); otherwise <see langword="false"/>.</returns>
    public static bool IsConcept2AntDevice(byte deviceType)
    {
        return deviceType == AntConstants.FitnessEquipmentDeviceType;
    }
}
