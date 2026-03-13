using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Concept2.Models;
using Concept2.Protocol.Csafe;
using Concept2.Transport;

namespace Concept2.Tests;

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
}
