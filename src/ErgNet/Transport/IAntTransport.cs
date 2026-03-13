namespace ErgNet.Transport;

/// <summary>
/// Extended transport for ANT+ wireless connections that supports receiving
/// ANT+ Fitness Equipment data page broadcasts for real-time data streaming.
/// </summary>
/// <remarks>
/// The PM5 broadcasts workout data via ANT+ FE-C (Fitness Equipment Control) protocol.
/// ANT+ is a receive-only data channel — CSAFE commands cannot be sent over ANT+.
/// Only the PM5 supports ANT+; PM3 and PM4 do not have ANT+ capability.
/// </remarks>
public interface IAntTransport : ITransport
{
    /// <summary>
    /// Subscribes to ANT+ data page broadcasts from the PM5.
    /// Each yielded byte array is a complete 8-byte ANT+ data page payload.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable stream of 8-byte data page payloads.</returns>
    IAsyncEnumerable<byte[]> SubscribeToDataPagesAsync(
        CancellationToken cancellationToken = default);
}
