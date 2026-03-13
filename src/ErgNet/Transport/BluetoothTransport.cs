using System.Runtime.CompilerServices;
using System.Threading.Channels;
using ErgNet.Protocol.Bluetooth;

namespace ErgNet.Transport;

/// <summary>
/// Bluetooth Low Energy transport for communicating with ErgNet Performance Monitors.
/// </summary>
/// <remarks>
/// This transport uses the ErgNet BLE GATT profile for communication.
/// CSAFE commands are written to the PM Receive characteristic (<c>CE060021</c>) on the
/// Rowing Control Service (<c>CE060020</c>), and responses are received via notifications
/// on the PM Transmit characteristic (<c>CE060022</c>).
/// To use, provide an implementation of <see cref="IBleDevice"/> that wraps your platform's
/// BLE API (e.g., Windows.Devices.Bluetooth, Plugin.BLE, etc.).
/// </remarks>
public sealed class BluetoothTransport : IBluetoothTransport
{
    private readonly IBleDevice _device;
    private readonly object _connectionLock = new();
    private CancellationTokenSource? _responseCts;
    private Channel<byte[]>? _responseChannel;
    private Task? _listenerTask;
    private bool _disposed;

    /// <summary>
    /// Initializes a new <see cref="BluetoothTransport"/> with the given BLE device.
    /// </summary>
    /// <param name="device">
    /// An <see cref="IBleDevice"/> implementation wrapping the ErgNet PM BLE device.
    /// </param>
    public BluetoothTransport(IBleDevice device)
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

        lock (_connectionLock)
        {
            if (_responseCts != null)
            {
                throw new InvalidOperationException("Transport is already connected or connecting.");
            }

            // Start listening for CSAFE responses on the PM Transmit characteristic.
            _responseCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _responseChannel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(16)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = true,
            });
        }

        try
        {
            await _device.ConnectAsync(cancellationToken).ConfigureAwait(false);
            _listenerTask = ListenForResponsesAsync(_responseCts.Token);
        }
        catch
        {
            // Clean up on connection failure
            lock (_connectionLock)
            {
                _responseCts?.Cancel();
                _responseCts?.Dispose();
                _responseCts = null;
                _responseChannel = null;
                _listenerTask = null;
            }
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        StopResponseListener();
        await _device.DisconnectAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_device.IsConnected)
        {
            throw new InvalidOperationException("Transport is not connected.");
        }

        await _device.WriteCharacteristicAsync(
            BleConstants.RowingControlService,
            BleConstants.PmReceive,
            data,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<byte[]> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_responseChannel is null)
        {
            throw new InvalidOperationException("Transport is not connected.");
        }

        return await _responseChannel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<byte[]> SubscribeToCharacteristicAsync(
        Guid characteristicId,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_device.IsConnected)
        {
            throw new InvalidOperationException("Transport is not connected.");
        }

        return _device.SubscribeAsync(
            BleConstants.RowingPrimaryService,
            characteristicId,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<byte[]> ReadCharacteristicAsync(
        Guid characteristicId,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_device.IsConnected)
        {
            throw new InvalidOperationException("Transport is not connected.");
        }

        return await _device.ReadCharacteristicAsync(
            BleConstants.RowingPrimaryService,
            characteristicId,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            StopResponseListener();

            // Wait for the listener task to complete
            if (_listenerTask != null)
            {
                try
                {
                    await _listenerTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancelling
                }
            }

            await _device.DisconnectAsync().ConfigureAwait(false);
            _device.Dispose();
        }
    }

    private async Task ListenForResponsesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var data in _device.SubscribeAsync(
                BleConstants.RowingControlService,
                BleConstants.PmTransmit,
                cancellationToken).ConfigureAwait(false))
            {
                _responseChannel?.Writer.TryWrite(data);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when disconnecting.
        }
        finally
        {
            _responseChannel?.Writer.TryComplete();
        }
    }

    private void StopResponseListener()
    {
        if (_responseCts is not null)
        {
            _responseCts.Cancel();
            _responseCts.Dispose();
            _responseCts = null;
        }

        _responseChannel?.Writer.TryComplete();
        _responseChannel = null;
    }
}
