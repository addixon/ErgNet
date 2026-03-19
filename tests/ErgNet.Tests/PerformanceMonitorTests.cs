using System.Runtime.CompilerServices;
using System.Threading.Channels;
using ErgNet.Models;
using ErgNet.Protocol.Ant;
using ErgNet.Protocol.Csafe;
using ErgNet.Transport;

namespace ErgNet.Tests;

public class PerformanceMonitorTests
{
    // ──── StreamRowingDataAsync ────

    [Fact]
    public async Task StreamRowingDataAsync_WithUsbTransport_PollsViaCsafe()
    {
        // Arrange: a USB (non-BLE) transport that returns valid CSAFE responses.
        var transport = new FakeUsbTransport(responseCount: 3);
        await using var pm = new PerformanceMonitor(transport);
        await pm.ConnectAsync();

        // Act
        var results = new List<RowingData>();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await foreach (var data in pm.StreamRowingDataAsync(
            pollingInterval: TimeSpan.FromMilliseconds(50),
            cancellationToken: cts.Token))
        {
            results.Add(data);
            if (results.Count >= 3)
            {
                break;
            }
        }

        // Assert
        Assert.Equal(3, results.Count);
        Assert.True(transport.SendCallCount >= 3, "Expected at least 3 CSAFE send calls for polling");
    }

    [Fact]
    public async Task StreamRowingDataAsync_WithBleTransport_StreamsViaNotifications()
    {
        // Arrange: a BLE transport that pushes data via characteristic notifications.
        var transport = new FakeBleTransport();
        await using var pm = new PerformanceMonitor(transport);
        await pm.ConnectAsync();

        // Act
        var results = new List<RowingData>();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await foreach (var data in pm.StreamRowingDataAsync(
            cancellationToken: cts.Token))
        {
            results.Add(data);
            if (results.Count >= 2)
            {
                break;
            }
        }

        // Assert: should get data from BLE notifications, not CSAFE polling.
        Assert.Equal(2, results.Count);
        // BLE transport should NOT have received CSAFE polling commands.
        Assert.Equal(0, transport.CsafeSendCount);
    }

    [Fact]
    public async Task StreamRowingDataAsync_SignatureIsIdentical_ForBothTransportTypes()
    {
        // Verify that the method signature is identical—both calls compile and run
        // with the same parameters.
        var usbTransport = new FakeUsbTransport(responseCount: 1);
        var bleTransport = new FakeBleTransport();

        await using var usbPm = new PerformanceMonitor(usbTransport);
        await using var blePm = new PerformanceMonitor(bleTransport);
        await usbPm.ConnectAsync();
        await blePm.ConnectAsync();

        var interval = TimeSpan.FromMilliseconds(50);
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Same signature used for both transports.
        IAsyncEnumerable<RowingData> usbStream = usbPm.StreamRowingDataAsync(interval, cts.Token);
        IAsyncEnumerable<RowingData> bleStream = blePm.StreamRowingDataAsync(interval, cts.Token);

        // Both return IAsyncEnumerable<RowingData> — compile-time proof of same signature.
        Assert.NotNull(usbStream);
        Assert.NotNull(bleStream);
    }

    [Fact]
    public async Task GetRowingDataAsync_WorksWithBothTransports()
    {
        // Arrange
        var usbTransport = new FakeUsbTransport(responseCount: 1);
        var bleTransport = new FakeBleTransport();

        await using var usbPm = new PerformanceMonitor(usbTransport);
        await using var blePm = new PerformanceMonitor(bleTransport);
        await usbPm.ConnectAsync();
        await blePm.ConnectAsync();

        // Act — identical method signature for both.
        var usbData = await usbPm.GetRowingDataAsync();
        var bleData = await blePm.GetRowingDataAsync();

        // Assert — both return RowingData.
        Assert.NotNull(usbData);
        Assert.NotNull(bleData);
    }

    [Fact]
    public async Task GoReadyAsync_WorksWithBothTransports()
    {
        var usbTransport = new FakeUsbTransport(responseCount: 1);
        var bleTransport = new FakeBleTransport();

        await using var usbPm = new PerformanceMonitor(usbTransport);
        await using var blePm = new PerformanceMonitor(bleTransport);
        await usbPm.ConnectAsync();
        await blePm.ConnectAsync();

        // Same signature for both transports.
        await usbPm.GoReadyAsync();
        await blePm.GoReadyAsync();
    }

    [Fact]
    public async Task SendCsafeCommandsAsync_WorksWithBothTransports()
    {
        var usbTransport = new FakeUsbTransport(responseCount: 1);
        var bleTransport = new FakeBleTransport();

        await using var usbPm = new PerformanceMonitor(usbTransport);
        await using var blePm = new PerformanceMonitor(bleTransport);
        await usbPm.ConnectAsync();
        await blePm.ConnectAsync();

        var commands = new[] { new CsafeCommand(CsafeCommands.Short.GetStatus) };

        // Same signature for both transports.
        var usbResponse = await usbPm.SendCsafeCommandsAsync(commands);
        var bleResponse = await blePm.SendCsafeCommandsAsync(commands);

        Assert.NotNull(usbResponse);
        Assert.NotNull(bleResponse);
    }

    [Fact]
    public async Task StreamRowingDataAsync_WithAntTransport_StreamsViaDataPages()
    {
        // Arrange: an ANT+ transport that pushes data via FE-C data pages.
        var transport = new FakeAntTransport();
        await using var pm = new PerformanceMonitor(transport);
        await pm.ConnectAsync();

        // Act
        var results = new List<RowingData>();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await foreach (var data in pm.StreamRowingDataAsync(
            cancellationToken: cts.Token))
        {
            results.Add(data);
            if (results.Count >= 2)
            {
                break;
            }
        }

        // Assert: should get data from ANT+ data pages, not CSAFE polling.
        Assert.Equal(2, results.Count);
        Assert.Equal(0, transport.CsafeSendCount);
    }

    [Fact]
    public async Task GetDeviceInfoAsync_ReturnsCorrectManufacturerName()
    {
        var transport = new FakeUsbTransport(responseCount: 1);
        await using var pm = new PerformanceMonitor(transport);
        await pm.ConnectAsync();

        var info = await pm.GetDeviceInfoAsync();

        Assert.Equal("Concept2", info.ManufacturerName);
    }

    [Fact]
    public async Task AntTransport_SendAsync_ThrowsNotSupported()
    {
        var transport = new AntTransport(new FakeAntDevice());
        await transport.ConnectAsync();
        await Assert.ThrowsAsync<NotSupportedException>(
            () => transport.SendAsync(new byte[] { 0xF1 }));
    }

    [Fact]
    public async Task AntTransport_ReceiveAsync_ThrowsNotSupported()
    {
        var transport = new AntTransport(new FakeAntDevice());
        await transport.ConnectAsync();
        await Assert.ThrowsAsync<NotSupportedException>(
            () => transport.ReceiveAsync());
    }

    // ──── Fakes ────

    /// <summary>
    /// Fake USB transport that returns minimal valid CSAFE response frames.
    /// </summary>
    private sealed class FakeUsbTransport : ITransport
    {
        private readonly int _responseCount;
        private int _responsesReturned;

        public FakeUsbTransport(int responseCount)
        {
            _responseCount = responseCount;
        }

        public bool IsConnected { get; private set; }
        public int SendCallCount { get; private set; }

        public Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            IsConnected = true;
            return Task.CompletedTask;
        }

        public Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            IsConnected = false;
            return Task.CompletedTask;
        }

        public Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            SendCallCount++;
            return Task.CompletedTask;
        }

        public Task<byte[]> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            _responsesReturned++;
            // Return a minimal valid CSAFE response: [StartFlag, Status, Checksum, StopFlag]
            // Status = 0x01 (Ready state), Checksum = XOR of status = 0x01
            return Task.FromResult(new byte[] { 0xF1, 0x01, 0x01, 0xF2 });
        }

        public ValueTask DisposeAsync()
        {
            IsConnected = false;
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Fake BLE transport that simulates BLE notifications.
    /// </summary>
    private sealed class FakeBleTransport : IBluetoothTransport
    {
        public bool IsConnected { get; private set; }
        public int CsafeSendCount { get; private set; }

        public Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            IsConnected = true;
            return Task.CompletedTask;
        }

        public Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            IsConnected = false;
            return Task.CompletedTask;
        }

        public Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            CsafeSendCount++;
            return Task.CompletedTask;
        }

        public Task<byte[]> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            // Minimal CSAFE response.
            return Task.FromResult(new byte[] { 0xF1, 0x01, 0x01, 0xF2 });
        }

        public async IAsyncEnumerable<byte[]> SubscribeToCharacteristicAsync(
            Guid characteristicId,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // Emit a few fake BLE notification payloads.
            for (int i = 0; i < 5; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(10, cancellationToken);
                yield return CreateFakeNotificationPayload(characteristicId);
            }
        }

        public Task<byte[]> ReadCharacteristicAsync(
            Guid characteristicId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new byte[20]);
        }

        public ValueTask DisposeAsync()
        {
            IsConnected = false;
            return ValueTask.CompletedTask;
        }

        private static byte[] CreateFakeNotificationPayload(Guid characteristicId)
        {
            // GeneralStatus characteristic: 18 bytes minimum
            // AdditionalStatus characteristic: 18 bytes minimum
            return new byte[20]; // All zeros — represents zero elapsed time, zero distance, etc.
        }
    }

    /// <summary>
    /// Fake ANT+ transport that simulates ANT+ FE-C data page broadcasts.
    /// </summary>
    private sealed class FakeAntTransport : IAntTransport
    {
        public bool IsConnected { get; private set; }
        public int CsafeSendCount { get; private set; }

        public Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            IsConnected = true;
            return Task.CompletedTask;
        }

        public Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            IsConnected = false;
            return Task.CompletedTask;
        }

        public Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            CsafeSendCount++;
            throw new NotSupportedException("CSAFE not supported over ANT+.");
        }

        public Task<byte[]> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("CSAFE not supported over ANT+.");
        }

        public async IAsyncEnumerable<byte[]> SubscribeToDataPagesAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // Emit alternating General FE and Rower data pages.
            for (int i = 0; i < 10; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(10, cancellationToken);

                if (i % 2 == 0)
                {
                    // General FE Data page (0x10)
                    yield return new byte[]
                    {
                        AntConstants.GeneralFEDataPage, 0x16, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x30, // InUse state
                    };
                }
                else
                {
                    // Rower Data page (0x16)
                    yield return new byte[]
                    {
                        AntConstants.RowerDataPage, 0xFF, 0xFF, 0x00,
                        28, 0xC8, 0x00, 0x30, // 28 spm, 200W, InUse
                    };
                }
            }
        }

        public ValueTask DisposeAsync()
        {
            IsConnected = false;
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Fake ANT+ device for testing AntTransport directly.
    /// </summary>
    private sealed class FakeAntDevice : IAntDevice
    {
        public bool IsConnected { get; private set; }

        public Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            IsConnected = true;
            return Task.CompletedTask;
        }

        public Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            IsConnected = false;
            return Task.CompletedTask;
        }

        public async IAsyncEnumerable<byte[]> SubscribeAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken);
            yield return new byte[] { 0x10, 0x16, 0x00, 0x00, 0x00, 0x00, 0x00, 0x30 };
        }

        public void Dispose() { }
    }
}
