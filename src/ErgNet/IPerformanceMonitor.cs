using ErgNet.Models;
using ErgNet.Protocol.Csafe;

namespace ErgNet;

/// <summary>
/// Interface for communicating with a ErgNet Performance Monitor (PM3/PM4/PM5).
/// Provides methods for workout programming, data retrieval, and real-time data streaming.
/// </summary>
/// <remarks>
/// PM3 and PM4 support USB only. PM5 supports USB, Bluetooth, and ANT+.
/// ANT+ is a receive-only data channel for real-time workout metrics; CSAFE
/// command/response operations require USB or Bluetooth.
/// </remarks>
public interface IPerformanceMonitor : IAsyncDisposable
{
    /// <summary>Whether the monitor is currently connected.</summary>
    bool IsConnected { get; }

    /// <summary>Connects to the performance monitor.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>Disconnects from the performance monitor.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    // ──── Device Information ────

    /// <summary>Gets device information including serial number, firmware version, etc.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<DeviceInfo> GetDeviceInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets the current machine state.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<MachineState> GetStatusAsync(CancellationToken cancellationToken = default);

    // ──── Data Retrieval (polling) ────

    /// <summary>Gets a snapshot of current rowing data.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<RowingData> GetRowingDataAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets current stroke data including force curve.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<StrokeData> GetStrokeDataAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets the current drag factor.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<int> GetDragFactorAsync(CancellationToken cancellationToken = default);

    // ──── Workout Programming ────

    /// <summary>Programs a workout on the performance monitor.</summary>
    /// <param name="workout">The workout configuration to program.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetWorkoutAsync(WorkoutConfiguration workout, CancellationToken cancellationToken = default);

    /// <summary>Sets the PM to ready state, preparing for a new workout.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task GoReadyAsync(CancellationToken cancellationToken = default);

    /// <summary>Sets the PM to idle state.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task GoIdleAsync(CancellationToken cancellationToken = default);

    /// <summary>Resets the performance monitor.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ResetAsync(CancellationToken cancellationToken = default);

    // ──── Streaming ────

    /// <summary>
    /// Subscribes to a continuous stream of rowing data updates.
    /// </summary>
    /// <param name="pollingInterval">
    /// The interval between data samples. When connected via Bluetooth, the PM pushes data
    /// via BLE notifications and this interval acts as a minimum throttle between yielded
    /// snapshots. When connected via ANT+, the PM broadcasts data pages and this interval
    /// acts as a minimum throttle. When connected via USB, the library polls the PM via
    /// CSAFE commands at this interval. Pass <see langword="null"/> to use the default
    /// interval (200 ms for USB, or unthrottled for Bluetooth/ANT+).
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable stream of <see cref="RowingData"/> snapshots.</returns>
    /// <remarks>
    /// This method works identically regardless of whether the underlying transport is USB,
    /// Bluetooth, or ANT+. The same calling code can be used with any transport type.
    /// </remarks>
    IAsyncEnumerable<RowingData> StreamRowingDataAsync(
        TimeSpan? pollingInterval = null,
        CancellationToken cancellationToken = default);

    // ──── Raw CSAFE ────

    /// <summary>
    /// Sends raw CSAFE commands and returns the parsed response.
    /// For advanced users who need access to commands not covered by the high-level API.
    /// </summary>
    /// <param name="commands">The CSAFE commands to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<CsafeResponse> SendCsafeCommandsAsync(
        IEnumerable<CsafeCommand> commands,
        CancellationToken cancellationToken = default);
}
