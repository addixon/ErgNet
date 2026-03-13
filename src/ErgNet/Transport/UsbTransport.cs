namespace ErgNet.Transport;

/// <summary>
/// USB HID transport for communicating with ErgNet Performance Monitors via USB.
/// This implementation sends and receives CSAFE frames via HID reports.
/// </summary>
/// <remarks>
/// To use this transport, provide an implementation of <see cref="IHidDevice"/> or use
/// a HID library such as HidSharp or HidApi.Net.
/// The ErgNet PM5 uses USB Vendor ID 0x17A4 and Product ID 0x0001.
/// HID report sizes:
/// <list type="bullet">
///   <item>Report ID 0x01 – short report, up to 21 data bytes</item>
///   <item>Report ID 0x04 – medium report, up to 63 data bytes</item>
///   <item>Report ID 0x02 – long report, up to 121 data bytes</item>
/// </list>
/// </remarks>
public sealed class UsbTransport : ITransport
{
    /// <summary>ErgNet USB Vendor ID.</summary>
    public static readonly int VendorId = 0x17A4;

    /// <summary>ErgNet USB Product ID.</summary>
    public static readonly int ProductId = 0x0001;

    // Report ID → max data bytes (excludes the report ID byte itself).
    private const byte ShortReportId = 0x01;
    private const int ShortReportMaxData = 21;

    private const byte MediumReportId = 0x04;
    private const int MediumReportMaxData = 63;

    private const byte LongReportId = 0x02;
    private const int LongReportMaxData = 121;

    private readonly IHidDevice _device;
    private bool _disposed;

    /// <summary>
    /// Initializes a new <see cref="UsbTransport"/> with the given HID device.
    /// </summary>
    /// <param name="device">
    /// An <see cref="IHidDevice"/> implementation wrapping the ErgNet PM USB HID device.
    /// </param>
    public UsbTransport(IHidDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);
        _device = device;
    }

    /// <inheritdoc />
    public bool IsConnected => _device.IsOpen;

    /// <inheritdoc />
    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        if (!_device.IsOpen)
        {
            _device.Open();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_device.IsOpen)
        {
            _device.Close();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        if (!_device.IsOpen)
        {
            throw new InvalidOperationException("Transport is not connected.");
        }

        var (reportId, reportSize) = SelectReport(data.Length);

        // Report layout: [ReportID][Data...][Padding...]
        // Total write size = 1 (report ID) + reportSize (max data capacity).
        var report = new byte[1 + reportSize];
        report[0] = reportId;
        data.Span.CopyTo(report.AsSpan(1));
        // Remaining bytes are already zero-padded by array initialisation.

        _device.Write(report);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<byte[]> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        if (!_device.IsOpen)
        {
            throw new InvalidOperationException("Transport is not connected.");
        }

        // Allocate for the largest possible report.
        var buffer = new byte[1 + LongReportMaxData];
        int bytesRead = _device.Read(buffer);

        if (bytesRead <= 1)
        {
            return Task.FromResult(Array.Empty<byte>());
        }

        // Strip the report ID byte and return only the payload.
        var payload = new byte[bytesRead - 1];
        Buffer.BlockCopy(buffer, 1, payload, 0, payload.Length);
        return Task.FromResult(payload);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            _device.Dispose();
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Selects the smallest HID report type that can carry <paramref name="dataLength"/> bytes.
    /// </summary>
    private static (byte ReportId, int MaxDataBytes) SelectReport(int dataLength)
    {
        if (dataLength <= ShortReportMaxData)
        {
            return (ShortReportId, ShortReportMaxData);
        }

        if (dataLength <= MediumReportMaxData)
        {
            return (MediumReportId, MediumReportMaxData);
        }

        if (dataLength <= LongReportMaxData)
        {
            return (LongReportId, LongReportMaxData);
        }

        throw new ArgumentException(
            $"Data length {dataLength} exceeds maximum HID report capacity of {LongReportMaxData} bytes.",
            nameof(dataLength));
    }
}
