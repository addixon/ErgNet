# ErgNet

A modern .NET library for communicating with Concept2 Performance Monitors (PM3/PM4/PM5) over USB, Bluetooth Low Energy, and ANT+.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![NuGet](https://img.shields.io/nuget/v/ErgNet.svg)](https://www.nuget.org/packages/ErgNet)

## Features

- **USB, Bluetooth & ANT+** — same API regardless of connection type
- **Real-time streaming** — subscribe to continuous rowing data at up to ~10 Hz (BLE), ~4 Hz (ANT+), or configurable polling intervals (USB)
- **Workout programming** — configure fixed-time, fixed-distance, fixed-calorie, and interval workouts
- **CSAFE protocol** — full frame builder/parser with byte stuffing, checksums, and PM-specific extensions
- **Async & cancellable** — all I/O is `async`/`await` with `CancellationToken` support
- **Pluggable transports** — bring your own HID, BLE, or ANT+ library via simple interfaces

## Quick Start

```csharp
using ErgNet;
using ErgNet.Transport;

// USB connection
await using var transport = new UsbTransport(myHidDevice);
await using var pm = new PerformanceMonitor(transport);
await pm.ConnectAsync();

// Poll current data
var data = await pm.GetRowingDataAsync();
Console.WriteLine($"Distance: {data.DistanceMeters}m  Pace: {data.CurrentPace}");

// Stream continuous data (works with USB, Bluetooth, and ANT+)
await foreach (var snapshot in pm.StreamRowingDataAsync(pollingInterval: TimeSpan.FromMilliseconds(200)))
{
    Console.WriteLine($"{snapshot.ElapsedTime} | {snapshot.DistanceMeters}m | {snapshot.StrokeRate} spm");
}
```

```csharp
// Bluetooth connection — same API
await using var transport = new BluetoothTransport(myBleDevice);
await using var pm = new PerformanceMonitor(transport);
await pm.ConnectAsync();

// Identical streaming call — automatically uses BLE notifications
await foreach (var snapshot in pm.StreamRowingDataAsync())
{
    Console.WriteLine($"{snapshot.ElapsedTime} | {snapshot.DistanceMeters}m | {snapshot.StrokeRate} spm");
}
```

```csharp
// ANT+ connection — same API, receive-only data streaming (PM5 only)
await using var transport = new AntTransport(myAntDevice);
await using var pm = new PerformanceMonitor(transport);
await pm.ConnectAsync();

// Identical streaming call — automatically uses ANT+ data page broadcasts
await foreach (var snapshot in pm.StreamRowingDataAsync())
{
    Console.WriteLine($"{snapshot.ElapsedTime} | {snapshot.DistanceMeters}m | {snapshot.StrokeRate} spm");
}
```

> **Note:** ANT+ is a receive-only data channel for real-time workout metrics. CSAFE command/response operations (e.g., `GetRowingDataAsync`, `SetWorkoutAsync`) require USB or Bluetooth.

## Installation

```bash
dotnet add package ErgNet
```

> **Note:** This library targets **.NET 10**. Make sure your project uses `net10.0` or later.

## Architecture

### Transport Layer

The library uses a transport abstraction so you can plug in any USB HID, BLE, or ANT+ library:

| Interface | Purpose |
|-----------|---------|
| `ITransport` | Unified send/receive interface used by USB, Bluetooth, and ANT+ |
| `IBluetoothTransport` | Extends `ITransport` with BLE-specific GATT notification support |
| `IAntTransport` | Extends `ITransport` with ANT+ data page broadcast streaming |
| `IHidDevice` | Plug in any USB HID library (e.g. HidSharp, HidApi.Net) |
| `IBleDevice` | Plug in any BLE library (e.g. Plugin.BLE, Windows.Devices.Bluetooth) |
| `IAntDevice` | Plug in any ANT+ radio library (e.g. Dynastream/Garmin ANT+ USB stick SDK) |

**Implementing `IHidDevice`:**

```csharp
public class MyHidDevice : IHidDevice
{
    public bool IsOpen { get; private set; }
    public void Open() { /* open HID handle */ }
    public void Close() { /* close HID handle */ }
    public void Write(ReadOnlySpan<byte> data) { /* write HID report */ }
    public int Read(Span<byte> buffer, int timeout = 2000) { /* read HID report */ }
    public void Dispose() { Close(); }
}
```

**Implementing `IBleDevice`:**

```csharp
public class MyBleDevice : IBleDevice
{
    public bool IsConnected { get; private set; }
    public Task ConnectAsync(CancellationToken ct = default) { /* BLE connect */ }
    public Task DisconnectAsync(CancellationToken ct = default) { /* BLE disconnect */ }
    public Task WriteCharacteristicAsync(Guid service, Guid characteristic, ReadOnlyMemory<byte> data, CancellationToken ct = default) { /* GATT write */ }
    public Task<byte[]> ReadCharacteristicAsync(Guid service, Guid characteristic, CancellationToken ct = default) { /* GATT read */ }
    public IAsyncEnumerable<byte[]> SubscribeAsync(Guid service, Guid characteristic, CancellationToken ct = default) { /* GATT notify */ }
    public void Dispose() { /* cleanup */ }
}
```

**Implementing `IAntDevice`:**

```csharp
public class MyAntDevice : IAntDevice
{
    public bool IsConnected { get; private set; }
    public Task ConnectAsync(CancellationToken ct = default) { /* open ANT+ channel */ }
    public Task DisconnectAsync(CancellationToken ct = default) { /* close ANT+ channel */ }
    public IAsyncEnumerable<byte[]> SubscribeAsync(CancellationToken ct = default) { /* receive data pages */ }
    public void Dispose() { /* cleanup */ }
}
```

### USB Device IDs

| Field | Value |
|-------|-------|
| Vendor ID | `0x17A4` |
| Product ID | `0x0001` |

### BLE UUIDs

All Concept2 BLE services and characteristics use the base UUID `CE060000-43E5-11E4-916C-0800200C9A66`:

| Service/Characteristic | UUID |
|----------------------|------|
| Device Info Service | `CE060010-43E5-11E4-916C-0800200C9A66` |
| Rowing Control Service | `CE060020-43E5-11E4-916C-0800200C9A66` |
| PM Receive (write) | `CE060021-43E5-11E4-916C-0800200C9A66` |
| PM Transmit (notify) | `CE060022-43E5-11E4-916C-0800200C9A66` |
| Rowing Primary Service | `CE060030-43E5-11E4-916C-0800200C9A66` |
| General Status | `CE060031-43E5-11E4-916C-0800200C9A66` |
| Additional Status | `CE060032-43E5-11E4-916C-0800200C9A66` |

### ANT+ Constants

The PM5 broadcasts using the ANT+ Fitness Equipment (FE-C) device profile:

| Field | Value |
|-------|-------|
| Device Type | `17` (Fitness Equipment) |
| Channel Period | `8192` (~4 Hz) |
| RF Frequency | `57` (2457 MHz) |
| Transmission Type | `5` |

**ANT+ Data Pages:**

| Page | Code | Description |
|------|------|-------------|
| General FE Data | `0x10` | Broadcast by all fitness equipment types |
| General Metabolic Data | `0x12` | Caloric and metabolic data |
| Rower Data | `0x16` | Stroke count, cadence, and power |
| Nordic Skier Data | `0x18` | Stride count and cadence (SkiErg) |

## API Reference

### `IPerformanceMonitor`

The primary interface for interacting with a PM. All methods share the same signature regardless of whether the underlying transport is USB, Bluetooth, or ANT+.

#### Connection

```csharp
Task ConnectAsync(CancellationToken cancellationToken = default);
Task DisconnectAsync(CancellationToken cancellationToken = default);
bool IsConnected { get; }
```

#### Device Information

```csharp
Task<DeviceInfo> GetDeviceInfoAsync(CancellationToken cancellationToken = default);
Task<MachineState> GetStatusAsync(CancellationToken cancellationToken = default);
```

#### Data Retrieval (Polling)

```csharp
Task<RowingData> GetRowingDataAsync(CancellationToken cancellationToken = default);
Task<StrokeData> GetStrokeDataAsync(CancellationToken cancellationToken = default);
Task<int> GetDragFactorAsync(CancellationToken cancellationToken = default);
```

#### Data Streaming

```csharp
IAsyncEnumerable<RowingData> StreamRowingDataAsync(
    TimeSpan? pollingInterval = null,
    CancellationToken cancellationToken = default);
```

The `pollingInterval` parameter controls data rate:
- **USB**: The library polls the PM via CSAFE commands at this interval (default: 200ms)
- **Bluetooth**: BLE notifications are pushed by the PM; this interval acts as a minimum throttle between yielded snapshots (default: unthrottled — every notification is yielded)
- **ANT+**: ANT+ data page broadcasts are received from the PM; this interval acts as a minimum throttle between yielded snapshots (default: unthrottled — every broadcast is yielded)

#### Workout Programming

```csharp
Task SetWorkoutAsync(WorkoutConfiguration workout, CancellationToken cancellationToken = default);
Task GoReadyAsync(CancellationToken cancellationToken = default);
Task GoIdleAsync(CancellationToken cancellationToken = default);
Task ResetAsync(CancellationToken cancellationToken = default);
```

#### Raw CSAFE Commands

```csharp
Task<CsafeResponse> SendCsafeCommandsAsync(
    IEnumerable<CsafeCommand> commands,
    CancellationToken cancellationToken = default);
```

### Workout Configuration

```csharp
var workout = new WorkoutConfiguration
{
    WorkoutType = WorkoutType.FixedDistanceSplits,
    TargetDistanceMeters = 2000,
    SplitDistanceMeters = 500,
};
workout.Validate(); // Throws if invalid

await pm.SetWorkoutAsync(workout);
await pm.GoReadyAsync(); // Transition PM to ready state
```

### Device Discovery

```csharp
// Check if a BLE device is a Concept2 PM
bool isConcept2 = PerformanceMonitorDiscovery.IsConcept2Device(advertisedServiceUuids);

// Check if an ANT+ device is a Concept2 PM
bool isConcept2Ant = PerformanceMonitorDiscovery.IsConcept2AntDevice(antDeviceType);

// USB identification
int vendorId = PerformanceMonitorDiscovery.UsbVendorId;   // 0x17A4
int productId = PerformanceMonitorDiscovery.UsbProductId;  // 0x0001

// ANT+ identification
byte antDeviceType = PerformanceMonitorDiscovery.AntFitnessEquipmentDeviceType; // 17
```

## Data Models

### `RowingData`

| Property | Type | Description |
|----------|------|-------------|
| `ElapsedTime` | `TimeSpan` | Total elapsed workout time |
| `DistanceMeters` | `double` | Total distance rowed in meters |
| `StrokeRate` | `int` | Current stroke rate (strokes/min) |
| `HeartRate` | `int` | Current heart rate (bpm) |
| `CurrentPace` | `TimeSpan` | Current split pace (per 500m) |
| `AveragePace` | `TimeSpan` | Average split pace (per 500m) |
| `AveragePowerWatts` | `int` | Average power output (watts) |
| `TotalCalories` | `int` | Total calories burned |
| `SpeedMetersPerSecond` | `double` | Current speed (m/s) |
| `WorkoutState` | `WorkoutState` | Current workout state |
| `RowingState` | `RowingState` | Active or inactive |
| `StrokeState` | `StrokeState` | Current phase of the stroke |
| `DragFactor` | `int` | Current drag factor |
| `Timestamp` | `DateTimeOffset` | When this snapshot was captured |

### `StrokeData`

| Property | Type | Description |
|----------|------|-------------|
| `StrokeCount` | `int` | Cumulative stroke count |
| `DriveTimeSeconds` | `double` | Drive phase duration |
| `RecoveryTimeSeconds` | `double` | Recovery phase duration |
| `StrokeLengthMeters` | `double` | Stroke length |
| `PeakForceNewtons` | `double` | Peak force during drive |
| `AverageForceNewtons` | `double` | Average force during drive |
| `WorkPerStrokeJoules` | `double` | Work done per stroke |
| `ImpulseForceNewtonSeconds` | `double` | Impulse force of the stroke |
| `ForceCurve` | `int[]?` | Force curve data points |
| `Timestamp` | `DateTimeOffset` | When this stroke data was captured |

## CSAFE Protocol

The library provides a complete CSAFE frame builder and parser for advanced usage:

```csharp
using ErgNet.Protocol.Csafe;

// Build a frame with multiple commands
var commands = new[]
{
    new CsafeCommand(CsafeCommands.Short.GetTWork),
    new CsafeCommand(CsafeCommands.Short.GetHorizontal),
    new CsafeCommand(CsafeCommands.PmShort.PM_GetDragFactor,
        wrapperCommand: CsafeCommands.Long.SetUserCfg1),
};

byte[] frame = CsafeFrameBuilder.Build(commands);

// Parse a response frame
CsafeResponse response = CsafeFrameParser.Parse(responseBytes);

// Access parsed fields
if (response.Data.TryGetValue("GetTWork", out var twork))
{
    int hours = twork[0], minutes = twork[1], seconds = twork[2];
}
```

## Protocol References

- [PM5 Bluetooth Smart Communication Interface Definition](https://www.concept2.nl/files/pdf/us/monitors/PM5_BluetoothSmartInterfaceDefinition.pdf)
- [PM5 CSAFE Communication Definition](https://www.concept2.sg/files/pdf/us/monitors/PM5_CSAFECommunicationDefinition.pdf)
- [Concept2 Software Development Resources](https://www.concept2.com/service/software/software-development)
- [ANT+ Fitness Equipment Device Profile](https://www.thisisant.com/developer/ant-plus/device-profiles/#tabs-Fitness+Equipment)

## License

This project is licensed under the [MIT License](LICENSE).
