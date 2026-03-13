using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Concept2.Models;
using Concept2.Protocol.Bluetooth;
using Concept2.Protocol.Csafe;
using Concept2.Transport;

namespace Concept2;

/// <summary>
/// Main implementation of <see cref="IPerformanceMonitor"/> for communicating with
/// Concept2 Performance Monitors (PM3/PM4/PM5).
/// </summary>
/// <remarks>
/// Construct with any <see cref="ITransport"/>. If the transport also implements
/// <see cref="IBluetoothTransport"/>, BLE streaming features become available.
/// </remarks>
public sealed class PerformanceMonitor : IPerformanceMonitor
{
    private readonly ITransport _transport;
    private readonly IBluetoothTransport? _bleTransport;
    private readonly SemaphoreSlim _transportLock = new(1, 1);
    private bool _disposed;

    /// <summary>
    /// Initializes a new <see cref="PerformanceMonitor"/> with the given transport.
    /// </summary>
    /// <param name="transport">
    /// The transport layer used to communicate with the performance monitor.
    /// If the transport implements <see cref="IBluetoothTransport"/>, BLE streaming
    /// features are enabled automatically.
    /// </param>
    public PerformanceMonitor(ITransport transport)
    {
        ArgumentNullException.ThrowIfNull(transport);
        _transport = transport;
        _bleTransport = transport as IBluetoothTransport;
    }

    /// <inheritdoc />
    public bool IsConnected => _transport.IsConnected;

    /// <inheritdoc />
    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _transport.ConnectAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _transport.DisconnectAsync(cancellationToken);
    }

    // ──── Device Information ────

    /// <inheritdoc />
    public async Task<DeviceInfo> GetDeviceInfoAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var commands = new[]
        {
            new CsafeCommand(CsafeCommands.Short.GetSerial),
            new CsafeCommand(CsafeCommands.Short.GetVersion),
        };

        var response = await SendAndReceiveAsync(commands, cancellationToken).ConfigureAwait(false);

        string serial = ExtractString(response, "GetSerial");
        int[]? versionData = ExtractFields(response, "GetVersion");

        string firmware = versionData is { Length: >= 3 }
            ? $"{versionData[0]}.{versionData[1]}.{versionData[2]}"
            : "Unknown";
        string hardware = versionData is { Length: >= 5 }
            ? $"{versionData[3]}.{versionData[4]}"
            : "Unknown";

        return new DeviceInfo(
            SerialNumber: serial,
            FirmwareVersion: firmware,
            HardwareVersion: hardware,
            ManufacturerName: "Concept2",
            ModelNumber: "PM5");
    }

    /// <inheritdoc />
    public async Task<MachineState> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var commands = new[] { new CsafeCommand(CsafeCommands.Short.GetStatus) };
        var response = await SendAndReceiveAsync(commands, cancellationToken).ConfigureAwait(false);

        byte machineState = (byte)(response.Status & CsafeConstants.StatusMachineStateMask);
        return (MachineState)machineState;
    }

    // ──── Data Retrieval ────

    /// <inheritdoc />
    public async Task<RowingData> GetRowingDataAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var commands = new[]
        {
            new CsafeCommand(CsafeCommands.Short.GetTWork),
            new CsafeCommand(CsafeCommands.Short.GetHorizontal),
            new CsafeCommand(CsafeCommands.Short.GetPace),
            new CsafeCommand(CsafeCommands.Short.GetCadence),
            new CsafeCommand(CsafeCommands.Short.GetCalories),
            new CsafeCommand(CsafeCommands.Short.GetPower),
            new CsafeCommand(CsafeCommands.Short.GetHeartRateCurrent),
            new CsafeCommand(CsafeCommands.PmShort.PM_GetWorkoutState,
                wrapperCommand: CsafeCommands.Long.SetUserCfg1),
            new CsafeCommand(CsafeCommands.PmShort.PM_GetStrokeState,
                wrapperCommand: CsafeCommands.Long.SetUserCfg1),
        };

        var response = await SendAndReceiveAsync(commands, cancellationToken).ConfigureAwait(false);

        int[]? twork = ExtractFields(response, "GetTWork");
        int[]? horizontal = ExtractFields(response, "GetHorizontal");
        int[]? pace = ExtractFields(response, "GetPace");
        int[]? cadence = ExtractFields(response, "GetCadence");
        int[]? calories = ExtractFields(response, "GetCalories");
        int[]? power = ExtractFields(response, "GetPower");
        int[]? heartRate = ExtractFields(response, "GetHeartRateCurrent");
        int[]? workoutState = ExtractFields(response, "PM_GetWorkoutState");
        int[]? strokeState = ExtractFields(response, "PM_GetStrokeState");

        var elapsed = twork is { Length: >= 3 }
            ? new TimeSpan(twork[0], twork[1], twork[2])
            : TimeSpan.Zero;

        double distance = horizontal is { Length: >= 2 } ? horizontal[0] : 0;

        var currentPace = pace is { Length: >= 2 }
            ? TimeSpan.FromSeconds(pace[0] * 60 + pace[1])
            : TimeSpan.Zero;

        return new RowingData
        {
            ElapsedTime = elapsed,
            DistanceMeters = distance,
            CurrentPace = currentPace,
            AveragePace = currentPace,
            StrokeRate = cadence is { Length: >= 2 } ? cadence[0] : 0,
            TotalCalories = calories is { Length: >= 1 } ? calories[0] : 0,
            AveragePowerWatts = power is { Length: >= 2 } ? power[0] : 0,
            HeartRate = heartRate is { Length: >= 1 } ? heartRate[0] : 0,
            WorkoutState = workoutState is { Length: >= 1 } ? (WorkoutState)workoutState[0] : WorkoutState.WaitToBegin,
            StrokeState = strokeState is { Length: >= 1 } ? (StrokeState)strokeState[0] : StrokeState.WaitingForWheelToReachMinSpeed,
            Timestamp = DateTimeOffset.UtcNow,
        };
    }

    /// <inheritdoc />
    public async Task<StrokeData> GetStrokeDataAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var commands = new[]
        {
            new CsafeCommand(CsafeCommands.PmLong.PM_GetStrokeStats,
                data: [0x00],
                wrapperCommand: CsafeCommands.Long.SetUserCfg1),
            new CsafeCommand(CsafeCommands.PmLong.PM_GetForcePlotData,
                data: [0x20],
                wrapperCommand: CsafeCommands.Long.SetUserCfg1),
        };

        var response = await SendAndReceiveAsync(commands, cancellationToken).ConfigureAwait(false);

        int[]? stats = ExtractFields(response, "PM_GetStrokeStats");
        int[]? forcePlot = ExtractFields(response, "PM_GetForcePlotData");

        return new StrokeData
        {
            StrokeCount = stats is { Length: >= 1 } ? stats[0] : 0,
            DriveTimeSeconds = stats is { Length: >= 2 } ? stats[1] / 100.0 : 0,
            RecoveryTimeSeconds = stats is { Length: >= 3 } ? stats[2] / 100.0 : 0,
            StrokeLengthMeters = stats is { Length: >= 4 } ? stats[3] / 100.0 : 0,
            PeakForceNewtons = stats is { Length: >= 5 } ? stats[4] / 10.0 : 0,
            AverageForceNewtons = stats is { Length: >= 6 } ? stats[5] / 10.0 : 0,
            WorkPerStrokeJoules = stats is { Length: >= 7 } ? stats[6] / 10.0 : 0,
            ImpulseForceNewtonSeconds = stats is { Length: >= 8 } ? stats[7] / 10.0 : 0,
            ForceCurve = forcePlot,
            Timestamp = DateTimeOffset.UtcNow,
        };
    }

    /// <inheritdoc />
    public async Task<int> GetDragFactorAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var commands = new[]
        {
            new CsafeCommand(CsafeCommands.PmShort.PM_GetDragFactor,
                wrapperCommand: CsafeCommands.Long.SetUserCfg1),
        };

        var response = await SendAndReceiveAsync(commands, cancellationToken).ConfigureAwait(false);
        int[]? drag = ExtractFields(response, "PM_GetDragFactor");
        return drag is { Length: >= 1 } ? drag[0] : 0;
    }

    // ──── Workout Programming ────

    /// <inheritdoc />
    public async Task SetWorkoutAsync(WorkoutConfiguration workout, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(workout);
        workout.Validate();

        var commands = new List<CsafeCommand>
        {
            new(CsafeCommands.Long.SetProgram, [(byte)workout.WorkoutType, 0x00]),
        };

        switch (workout.WorkoutType)
        {
            case WorkoutType.FixedTimeNoSplits:
            case WorkoutType.FixedTimeSplits:
                var time = workout.TargetTime!.Value;
                commands.Add(new CsafeCommand(CsafeCommands.Long.SetTWork,
                    [(byte)time.Hours, (byte)time.Minutes, (byte)time.Seconds]));
                break;

            case WorkoutType.FixedDistanceNoSplits:
            case WorkoutType.FixedDistanceSplits:
                int dist = workout.TargetDistanceMeters!.Value;
                commands.Add(new CsafeCommand(CsafeCommands.Long.SetHorizontal,
                    [(byte)(dist & 0xFF), (byte)((dist >> 8) & 0xFF), 0x24]));
                break;

            case WorkoutType.FixedCalorie:
            case WorkoutType.FixedCalsInterval:
                int cals = workout.TargetCalories!.Value;
                commands.Add(new CsafeCommand(CsafeCommands.Long.SetCalories,
                    [(byte)(cals & 0xFF), (byte)((cals >> 8) & 0xFF)]));
                break;
        }

        await SendAndReceiveAsync(commands, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task GoReadyAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return SendAndReceiveAsync([new CsafeCommand(CsafeCommands.Short.GoReady)], cancellationToken);
    }

    /// <inheritdoc />
    public Task GoIdleAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return SendAndReceiveAsync([new CsafeCommand(CsafeCommands.Short.GoIdle)], cancellationToken);
    }

    /// <inheritdoc />
    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return SendAndReceiveAsync([new CsafeCommand(CsafeCommands.Short.Reset)], cancellationToken);
    }

    // ──── Streaming ────

    /// <inheritdoc />
    public async IAsyncEnumerable<RowingData> StreamRowingDataAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_bleTransport is null)
        {
            throw new NotSupportedException(
                "Streaming is only supported over Bluetooth Low Energy. " +
                "Use a transport that implements IBluetoothTransport.");
        }

        // Track the latest data from each characteristic so we can merge them.
        GeneralStatusData? latestGeneral = null;
        AdditionalStatusData? latestAdditional = null;

        var generalStream = _bleTransport.SubscribeToCharacteristicAsync(
            BleConstants.GeneralStatus, cancellationToken);
        var additionalStream = _bleTransport.SubscribeToCharacteristicAsync(
            BleConstants.AdditionalStatus, cancellationToken);

        // Merge both streams using a channel.
        var merged = MergeStreams(generalStream, additionalStream, cancellationToken);

        await foreach (var (source, data) in merged.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (source == StreamSource.General)
            {
                latestGeneral = BleDataParser.ParseGeneralStatus(data);
            }
            else
            {
                latestAdditional = BleDataParser.ParseAdditionalStatus(data);
            }

            // Only yield once we have data from both characteristics.
            if (latestGeneral is null || latestAdditional is null)
            {
                continue;
            }

            var general = latestGeneral.Value;
            var additional = latestAdditional.Value;

            yield return new RowingData
            {
                ElapsedTime = general.ElapsedTime,
                DistanceMeters = general.DistanceMeters,
                StrokeRate = additional.StrokeRate,
                HeartRate = additional.HeartRate,
                CurrentPace = additional.CurrentPace,
                AveragePace = additional.AveragePace,
                AveragePowerWatts = additional.AveragePowerWatts,
                SpeedMetersPerSecond = additional.SpeedMetersPerSecond,
                WorkoutState = general.WorkoutState,
                RowingState = general.RowingState,
                StrokeState = general.StrokeState,
                Timestamp = DateTimeOffset.UtcNow,
            };
        }
    }

    // ──── Raw CSAFE ────

    /// <inheritdoc />
    public Task<CsafeResponse> SendCsafeCommandsAsync(
        IEnumerable<CsafeCommand> commands,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(commands);
        return SendAndReceiveAsync(commands, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            _transportLock.Dispose();
            await _transport.DisposeAsync().ConfigureAwait(false);
        }
    }

    // ──── Helpers ────

    private async Task<CsafeResponse> SendAndReceiveAsync(
        IEnumerable<CsafeCommand> commands,
        CancellationToken cancellationToken)
    {
        byte[] frame = CsafeFrameBuilder.Build(commands);

        await _transportLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _transport.SendAsync(frame, cancellationToken).ConfigureAwait(false);
            byte[] responseBytes = await _transport.ReceiveAsync(cancellationToken).ConfigureAwait(false);
            return CsafeFrameParser.Parse(responseBytes);
        }
        finally
        {
            _transportLock.Release();
        }
    }

    private static int[]? ExtractFields(CsafeResponse response, string commandName)
    {
        return response.Data.TryGetValue(commandName, out var fields) ? fields : null;
    }

    private static string ExtractString(CsafeResponse response, string commandName)
    {
        if (response.Data.TryGetValue(commandName, out var fields) && fields.Length > 0)
        {
            var chars = new char[fields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                chars[i] = (char)fields[i];
            }

            return new string(chars);
        }

        return string.Empty;
    }

    private enum StreamSource { General, Additional }

    private static async IAsyncEnumerable<(StreamSource Source, byte[] Data)> MergeStreams(
        IAsyncEnumerable<byte[]> generalStream,
        IAsyncEnumerable<byte[]> additionalStream,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<(StreamSource, byte[])>();

        async Task PumpAsync(IAsyncEnumerable<byte[]> source, StreamSource tag)
        {
            await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                await channel.Writer.WriteAsync((tag, item), cancellationToken).ConfigureAwait(false);
            }
        }

        var pump1 = PumpAsync(generalStream, StreamSource.General);
        var pump2 = PumpAsync(additionalStream, StreamSource.Additional);

        // Complete the channel when both pumps finish.
        _ = Task.WhenAll(pump1, pump2).ContinueWith(
            _ => channel.Writer.TryComplete(),
            cancellationToken,
            TaskContinuationOptions.None,
            TaskScheduler.Default);

        await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }
}
