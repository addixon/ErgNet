using System.Runtime.CompilerServices;

namespace Concept2.Transport;

/// <summary>
/// Extended transport for Bluetooth Low Energy connections that supports GATT characteristic notifications
/// for real-time data streaming.
/// </summary>
public interface IBluetoothTransport : ITransport
{
    /// <summary>
    /// Subscribes to BLE characteristic notifications for real-time data.
    /// </summary>
    /// <param name="characteristicId">The GATT characteristic UUID to subscribe to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable stream of byte arrays from the characteristic.</returns>
    IAsyncEnumerable<byte[]> SubscribeToCharacteristicAsync(
        Guid characteristicId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a value from a specific BLE characteristic.
    /// </summary>
    /// <param name="characteristicId">The GATT characteristic UUID to read.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The characteristic value as a byte array.</returns>
    Task<byte[]> ReadCharacteristicAsync(
        Guid characteristicId,
        CancellationToken cancellationToken = default);
}
