namespace Concept2.Transport;

/// <summary>
/// Abstraction for a USB HID device, allowing integration with various HID libraries
/// such as HidSharp or HidApi.Net.
/// </summary>
public interface IHidDevice : IDisposable
{
    /// <summary>Whether the HID device handle is currently open.</summary>
    bool IsOpen { get; }

    /// <summary>Opens the HID device for communication.</summary>
    void Open();

    /// <summary>Closes the HID device handle.</summary>
    void Close();

    /// <summary>Writes a raw HID report to the device.</summary>
    /// <param name="data">The report data to write, including the report ID as the first byte.</param>
    void Write(ReadOnlySpan<byte> data);

    /// <summary>Reads a raw HID report from the device.</summary>
    /// <param name="buffer">The buffer to receive the report data.</param>
    /// <param name="timeout">Read timeout in milliseconds. Defaults to 2000 ms.</param>
    /// <returns>The number of bytes actually read.</returns>
    int Read(Span<byte> buffer, int timeout = 2000);
}
