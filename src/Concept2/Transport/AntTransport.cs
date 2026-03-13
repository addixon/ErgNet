using System.Runtime.CompilerServices;
using Concept2.Protocol.Ant;

namespace Concept2.Transport;

/// <summary>
/// ANT+ wireless transport for receiving real-time data from Concept2 PM5 Performance Monitors.
/// </summary>
/// <remarks>
/// This transport uses the ANT+ Fitness Equipment (FE-C) device profile to receive
/// broadcast data pages from the PM5. The PM5 broadcasts general fitness data (page 0x10)
/// and rower-specific data (page 0x16) at approximately 4 Hz.
/// <para>
/// ANT+ is a receive-only data channel for workout metrics. CSAFE command/response
/// communication is <b>not</b> supported over ANT+. Calling <see cref="SendAsync"/> or
/// <see cref="ReceiveAsync"/> will throw <see cref="NotSupportedException"/>.
/// </para>
/// <para>
/// To use, provide an implementation of <see cref="IAntDevice"/> that wraps your platform's
/// ANT+ radio API (e.g., a Dynastream/Garmin ANT+ USB stick SDK).
/// </para>
/// </remarks>
public sealed class AntTransport : IAntTransport
{
    private readonly IAntDevice _device;
    private bool _disposed;

    /// <summary>
    /// Initializes a new <see cref="AntTransport"/> with the given ANT+ device.
    /// </summary>
    /// <param name="device">
    /// An <see cref="IAntDevice"/> implementation wrapping the ANT+ radio hardware.
    /// </param>
    public AntTransport(IAntDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);
        _device = device;
    }

    /// <inheritdoc />
    public bool IsConnected => _device.IsConnected;

    /// <inheritdoc />
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _device.ConnectAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _device.DisconnectAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Not supported. CSAFE commands cannot be sent over ANT+.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown. Use USB or Bluetooth for CSAFE commands.</exception>
    public Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "CSAFE commands cannot be sent over ANT+. Use USB or Bluetooth transport for command communication.");
    }

    /// <summary>
    /// Not supported. CSAFE responses are not available over ANT+.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown. Use USB or Bluetooth for CSAFE commands.</exception>
    public Task<byte[]> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "CSAFE responses are not available over ANT+. Use USB or Bluetooth transport for command communication.");
    }

    /// <inheritdoc />
    public IAsyncEnumerable<byte[]> SubscribeToDataPagesAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_device.IsConnected)
        {
            throw new InvalidOperationException("Transport is not connected.");
        }

        return _device.SubscribeAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            await _device.DisconnectAsync().ConfigureAwait(false);
            _device.Dispose();
        }
    }
}
