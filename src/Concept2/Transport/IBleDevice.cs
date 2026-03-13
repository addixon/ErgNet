namespace Concept2.Transport;

/// <summary>
/// Abstraction for a Bluetooth Low Energy device, allowing integration with various BLE libraries
/// such as Windows.Devices.Bluetooth, Plugin.BLE, or platform-specific APIs.
/// </summary>
public interface IBleDevice : IDisposable
{
    /// <summary>Whether the BLE device is currently connected.</summary>
    bool IsConnected { get; }

    /// <summary>Establishes a BLE connection to the device.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>Disconnects from the BLE device.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>Writes data to a GATT characteristic on the specified service.</summary>
    /// <param name="serviceId">The GATT service UUID.</param>
    /// <param name="characteristicId">The GATT characteristic UUID.</param>
    /// <param name="data">The data to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task WriteCharacteristicAsync(
        Guid serviceId,
        Guid characteristicId,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default);

    /// <summary>Reads data from a GATT characteristic on the specified service.</summary>
    /// <param name="serviceId">The GATT service UUID.</param>
    /// <param name="characteristicId">The GATT characteristic UUID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The characteristic value as a byte array.</returns>
    Task<byte[]> ReadCharacteristicAsync(
        Guid serviceId,
        Guid characteristicId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to notifications on a GATT characteristic.
    /// </summary>
    /// <param name="serviceId">The GATT service UUID.</param>
    /// <param name="characteristicId">The GATT characteristic UUID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable stream of notification payloads.</returns>
    IAsyncEnumerable<byte[]> SubscribeAsync(
        Guid serviceId,
        Guid characteristicId,
        CancellationToken cancellationToken = default);
}
