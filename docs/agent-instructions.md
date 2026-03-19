# ErgNet — AI Agent Instructions

Universal guide for AI coding agents working on the ErgNet repository. This document is agent-agnostic — it applies equally to GitHub Copilot, Cursor, Windsurf, Cline, Aider, and any other AI assistant.

> **Agent-specific configuration files** are also available at the repository root:
> `.github/copilot-instructions.md`, `.cursorrules`, `.windsurfrules`, `.clinerules`

---

## 1. Project Overview

**ErgNet** is a .NET library for communicating with Concept2 Performance Monitors (PM3/PM4/PM5) over USB, Bluetooth Low Energy (BLE), and ANT+.

| Field              | Value                                          |
|--------------------|-------------------------------------------------|
| Package            | [`ErgNet`](https://www.nuget.org/packages/ErgNet) |
| License            | MIT                                             |
| Target Framework   | `net10.0`                                       |
| Solution File      | `ErgNet.slnx`                                   |
| Root Namespace     | `ErgNet`                                         |
| Repository         | <https://github.com/addixon/ErgNet>              |

---

## 2. Build, Test, and Pack

```bash
# Restore dependencies
dotnet restore

# Build (Release)
dotnet build --configuration Release

# Run tests
dotnet test tests/ErgNet.Tests/ErgNet.Tests.csproj --logger "console;verbosity=detailed"

# Create NuGet package
dotnet pack --configuration Release --output ./nupkg
```

**Important:** `TreatWarningsAsErrors` is enabled in `Directory.Build.props`. The build must produce **zero warnings**.

---

## 3. Repository Structure

```
ErgNet/
├── src/ErgNet/                        # Main library project
│   ├── IPerformanceMonitor.cs         # Primary public interface
│   ├── PerformanceMonitor.cs          # Main implementation (orchestrator)
│   ├── PerformanceMonitorDiscovery.cs # Device discovery helpers
│   ├── Models/                        # Data models: RowingData, StrokeData, DeviceInfo, enums
│   ├── Protocol/
│   │   ├── Ant/                       # ANT+ FE-C data page parsing
│   │   ├── Bluetooth/                 # BLE GATT notification parsing
│   │   └── Csafe/                     # CSAFE frame builder, parser, command registry
│   └── Transport/                     # Transport abstractions and implementations
├── tests/ErgNet.Tests/                # xUnit test project (mirrors src/ structure)
├── docs/                              # Documentation
│   └── agent-instructions.md          # This file — universal agent guide
├── .github/
│   ├── copilot-instructions.md        # GitHub Copilot agent instructions
│   ├── workflows/ci.yml              # CI/CD pipeline
│   ├── workflows/promote.yml         # Production promotion workflow
│   └── dependabot.yml                # Automated dependency updates
├── .cursorrules                       # Cursor AI configuration
├── .windsurfrules                     # Windsurf AI configuration
├── .clinerules                        # Cline AI configuration
├── CONTRIBUTING.md                    # Contributor guide
├── Directory.Build.props              # Shared MSBuild properties
├── GitVersion.yml                     # Automatic versioning config
├── .editorconfig                      # Code style rules
└── ErgNet.slnx                        # Solution file
```

---

## 4. Naming Conventions

| Term         | Usage                                                                 |
|--------------|-----------------------------------------------------------------------|
| **ErgNet**   | Library/package name. Used for namespaces, assembly, NuGet package.    |
| **Concept2** | Hardware manufacturer. Used only for hardware references.             |

- ✅ `IsConcept2Device`, `ManufacturerName: "Concept2"`, "Concept2 Performance Monitor"
- ❌ Never say "ErgNet hardware" or "ErgNet Performance Monitor" — ErgNet is the software library, Concept2 is the manufacturer.

---

## 5. Coding Conventions

### C# Style

- **File-scoped namespaces**: `namespace ErgNet;`
- **`var`** when the type is apparent from the right-hand side
- **Expression-bodied members** for single-line properties and methods
- **4-space indentation**, LF line endings, UTF-8 encoding
- Sort `using` directives with `System` namespaces first
- Follow all `.editorconfig` rules

### Patterns Used Throughout

| Pattern                              | Where / Why                                                    |
|--------------------------------------|----------------------------------------------------------------|
| `async`/`await` + `CancellationToken` | All I/O methods                                                |
| `IAsyncEnumerable<T>`               | Data streaming with `[EnumeratorCancellation]`                  |
| `SemaphoreSlim`                      | Guards concurrent transport access in `PerformanceMonitor`      |
| `Channel<T>`                         | Merges multiple BLE notification streams                        |
| `ObjectDisposedException.ThrowIf`    | Disposed-check at start of public methods                       |
| `ArgumentNullException.ThrowIfNull`  | Null parameter validation                                       |
| Transport abstraction                | Users implement `IHidDevice`/`IBleDevice`/`IAntDevice`          |

### XML Documentation

- All public types and members **must** have XML doc comments
- Use `<inheritdoc />` on concrete implementations of interface methods
- Include `<param>`, `<returns>`, and `<remarks>` where helpful

### Error Handling

| Exception                    | When to throw                                            |
|------------------------------|----------------------------------------------------------|
| `ArgumentException`          | Invalid configurations (e.g., `WorkoutConfiguration.Validate()`) |
| `NotSupportedException`      | Operations unsupported by a transport (e.g., CSAFE over ANT+)  |
| `InvalidOperationException`  | State errors (e.g., missing transport)                   |
| `CsafeFrameException`       | CSAFE protocol-level errors                              |

---

## 6. Testing Conventions

| Rule                        | Detail                                                                      |
|-----------------------------|-----------------------------------------------------------------------------|
| Framework                   | xUnit 2.9.3                                                                |
| Naming                      | `MethodName_Scenario_ExpectedBehavior` (e.g., `ParseRowerData_TooShort_Throws`) |
| Fakes over mocks            | Hand-written fakes (`FakeUsbTransport`, `FakeBleTransport`, etc.)           |
| Global using                | `using Xunit;` via test `.csproj` — do **not** add it to test files         |
| Protocol tests              | Include byte-level comments explaining the data structure being tested      |
| File location               | Test files mirror source file paths (e.g., `Protocol/Csafe/` → `Protocol/Csafe/`) |

---

## 7. Architecture

### Layer Diagram

```
┌────────────────────────────────────────────┐
│         IPerformanceMonitor API            │  ← Unified public API
├────────────────────────────────────────────┤
│         PerformanceMonitor                 │  ← Orchestrator
├──────────┬─────────────┬───────────────────┤
│  CSAFE   │   BLE       │   ANT+           │  ← Protocol parsers
│  Builder │   Parser    │   Parser          │
│  Parser  │             │                   │
│  Registry│             │                   │
├──────────┴─────────────┴───────────────────┤
│    ITransport / IBluetoothTransport /      │  ← Transport abstraction
│    IAntTransport                           │
├──────────┬─────────────┬───────────────────┤
│ UsbTrans │ BleTrans    │ AntTrans          │  ← Concrete transports
├──────────┼─────────────┼───────────────────┤
│ IHidDev  │ IBleDev     │ IAntDev           │  ← User-provided devices
└──────────┴─────────────┴───────────────────┘
```

### Key Design Decisions

- **Transport polymorphism**: `PerformanceMonitor` detects transport type via `as` checks and selects the appropriate streaming strategy (USB polling, BLE notifications, or ANT+ broadcasts).
- **ANT+ is receive-only**: `AntTransport.SendAsync()`/`ReceiveAsync()` throw `NotSupportedException`. Only `SubscribeToDataPagesAsync()` is supported.
- **CSAFE command wrapping**: PM-specific commands are wrapped inside `SetUserCfg1` (0x1A). Multiple PM commands in a single frame are coalesced under a single wrapper.
- **8-bit rollover counters**: ANT+ stroke/stride counts use 8-bit rollover with `AntConstants.RolloverCounterMax = 256`.

---

## 8. Versioning and CI/CD

### Versioning

Automatic semantic versioning via [GitVersion 6.x](https://gitversion.net/) with Conventional Commits:

| Branch        | Label   | Example Version      |
|---------------|---------|----------------------|
| `main`        | `alpha` | `0.2.0-alpha.3`      |
| `release/*`   | `rc`    | `0.2.0-rc.1`         |
| Pull requests | `alpha` | `0.2.0-alpha.5`      |
| Production    | *(none)*| `0.2.0` (via promote)|

- Version tags are prefixed with `v` (e.g., `v0.2.0`)
- No hardcoded version in `.csproj` — version passed via `-p:Version=` and `-p:PackageVersion=` in CI

### CI/CD Pipelines

| Workflow          | Trigger                          | What it does                                                                     |
|-------------------|----------------------------------|----------------------------------------------------------------------------------|
| `ci.yml`          | Push to `main`/`release/*`, PRs  | Build → Test → Security scan → Pack. On push only: publish unlisted pre-release to NuGet.org. |
| `promote.yml`     | Manual `workflow_dispatch`       | Build → Test → Pack → Publish to NuGet.org → Sign (repository) → Publish to GitHub Packages → Git tag. |

- **Preproduction publish** runs only on `push` events (merged PRs), not on PR open/update.
- **OIDC trusted publishing** via `NuGet/login@v1` — no NuGet API keys stored as secrets.
- **Repository-signed packages**: NuGet.org applies its own repository signature automatically. GitHub Packages receives a repository-signed package (`--package-owner ergSoft`) using `NUGET_SIGNING_CERT` / `NUGET_SIGNING_CERT_PASSWORD` secrets (production environment only).
- **Dependabot** runs weekly for NuGet packages and GitHub Actions.

---

## 9. Protocol Reference

### CSAFE

| Field              | Value                                                          |
|--------------------|----------------------------------------------------------------|
| Frame format       | `[StartFlag] [Status] [Payload...] [Checksum] [StopFlag]`      |
| Start flags        | `0xF0` (extended) or `0xF1` (standard)                         |
| Stop flag          | `0xF2`                                                         |
| Byte stuffing      | `0xF3` + `(original − 0xF0)` for bytes `0xF0`–`0xF3`          |
| Checksum           | XOR of all payload bytes                                       |
| Max frame size     | 96 bytes                                                       |
| Short commands     | `≥ 0x80`                                                       |
| Long commands      | `< 0x80`                                                       |

### BLE

| Field              | Value                                                          |
|--------------------|----------------------------------------------------------------|
| Base UUID          | `CE060000-43E5-11E4-916C-0800200C9A66`                         |
| Control service    | `CE060020` (write: `CE060021`, notify: `CE060022`)              |
| General Status     | `CE060031` — elapsed time, distance, workout/rowing/stroke state|
| Additional Status  | `CE060032` — speed, stroke rate, heart rate, pace, power        |
| Data encoding      | Little-endian; time in centiseconds, distance in tenths of meters |

### ANT+

| Field              | Value                                                          |
|--------------------|----------------------------------------------------------------|
| Device type        | 17 (Fitness Equipment)                                         |
| Channel period     | 8192 (~4 Hz)                                                   |
| RF frequency       | 57 (2457 MHz)                                                  |
| Data page `0x10`   | General FE — speed, distance, heart rate, equipment state       |
| Data page `0x12`   | Metabolic — MET, caloric burn rate                              |
| Data page `0x16`   | Rower — stroke count, cadence, power                            |
| Data page `0x18`   | Nordic Skier — stride count, cadence, power                     |
| Payload size       | 8 bytes; equipment state in bits 4–6 of final byte              |
