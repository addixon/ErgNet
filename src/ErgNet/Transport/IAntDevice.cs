namespace ErgNet.Transport;

/// <summary>
/// Abstraction for an ANT+ radio device, allowing integration with various ANT+ libraries
/// and USB sticks (e.g., Dynastream/Garmin ANT+ USB sticks).
/// </summary>
/// <remarks>
/// Implementers should handle ANT channel configuration, opening, and data reception.
/// The ErgNet PM5 broadcasts as an ANT+ Fitness Equipment (FE-C) device with
/// device type 17, channel period 8192, and RF frequency 57 (2457 MHz).
/// </remarks>
public interface IAntDevice : IDisposable
{
    /// <summary>Whether the ANT+ device is currently connected and receiving data.</summary>
    bool IsConnected { get; }

    /// <summary>
    /// Connects to the ANT+ radio and opens a channel to receive
    /// Fitness Equipment data from the PM5.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>Disconnects from the ANT+ radio and closes the channel.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to ANT+ data page broadcasts from the fitness equipment.
    /// Each yielded byte array is a complete 8-byte ANT+ data page payload.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable stream of 8-byte data page payloads.</returns>
    IAsyncEnumerable<byte[]> SubscribeAsync(CancellationToken cancellationToken = default);
}
