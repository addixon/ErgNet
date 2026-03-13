namespace ErgNet.Transport;

/// <summary>
/// Abstraction over the physical communication layer (USB or Bluetooth) for sending and receiving CSAFE frames.
/// </summary>
public interface ITransport : IAsyncDisposable
{
    /// <summary>Whether the transport is currently connected to a device.</summary>
    bool IsConnected { get; }

    /// <summary>Sends a CSAFE frame to the performance monitor.</summary>
    /// <param name="data">The raw frame bytes to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);

    /// <summary>Receives a CSAFE response frame from the performance monitor.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The raw response frame bytes.</returns>
    Task<byte[]> ReceiveAsync(CancellationToken cancellationToken = default);

    /// <summary>Connects to the device.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>Disconnects from the device.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DisconnectAsync(CancellationToken cancellationToken = default);
}
