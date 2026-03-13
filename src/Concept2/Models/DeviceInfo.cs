namespace Concept2.Models;

/// <summary>
/// Represents device information retrieved from the Concept2 performance monitor.
/// </summary>
/// <param name="SerialNumber">The serial number of the device.</param>
/// <param name="FirmwareVersion">The firmware version installed on the device.</param>
/// <param name="HardwareVersion">The hardware revision of the device.</param>
/// <param name="ManufacturerName">The name of the device manufacturer.</param>
/// <param name="ModelNumber">The model number of the device.</param>
public sealed record DeviceInfo(
    string SerialNumber,
    string FirmwareVersion,
    string HardwareVersion,
    string ManufacturerName,
    string ModelNumber);
