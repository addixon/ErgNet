# Contributing to ErgNet

Thank you for considering contributing to ErgNet! This guide covers the development workflow, coding conventions, and release process.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) or later
- A Git client
- (Optional) An IDE such as Visual Studio, VS Code, or JetBrains Rider

## Getting Started

```bash
git clone https://github.com/addixon/ErgNet.git
cd ErgNet
dotnet restore
dotnet build
dotnet test
```

## Project Structure

```
ErgNet/
├── src/ErgNet/                    # Main library
│   ├── Models/                    # Data models and enums
│   ├── Protocol/
│   │   ├── Ant/                   # ANT+ FE-C protocol parsing
│   │   ├── Bluetooth/             # BLE GATT notification parsing
│   │   └── Csafe/                 # CSAFE frame builder, parser, and command registry
│   └── Transport/                 # Transport abstractions and implementations
├── tests/ErgNet.Tests/            # xUnit test project
├── .github/
│   ├── workflows/ci.yml           # CI/CD pipeline (build, test, publish)
│   ├── workflows/promote.yml      # Production promotion workflow
│   └── dependabot.yml             # Automated dependency updates
├── Directory.Build.props          # Shared MSBuild properties
├── GitVersion.yml                 # Automatic semantic versioning configuration
└── ErgNet.slnx                    # Solution file
```

## Development Workflow

### Branching Strategy

This project uses **GitHub Flow**:

1. Create a feature branch from `main`
2. Make changes and commit using [Conventional Commits](#commit-messages)
3. Open a pull request targeting `main`
4. CI runs automatically (build, test, security scan)
5. After review and merge, a pre-release package is published to NuGet.org (unlisted)
6. Production releases are promoted via the manual "Promote to Production" workflow

### Commit Messages

This project uses [Conventional Commits](https://www.conventionalcommits.org/) for automatic semantic versioning:

| Prefix | Version Bump | Example |
|--------|-------------|---------|
| `feat:` | Minor | `feat: add workout pause support` |
| `feat!:` or `BREAKING CHANGE` | Major | `feat!: rename ITransport.Send to SendAsync` |
| `fix:` | Patch | `fix: correct BLE notification parsing` |
| `perf:` | Patch | `perf: reduce CSAFE frame allocation` |
| `docs:` | None | `docs: update README examples` |
| `test:` | None | `test: add BLE parser edge cases` |
| `chore:` | None | `chore: update dependencies` |
| `ci:` | None | `ci: add code coverage step` |
| `refactor:` | None | `refactor: extract CSAFE checksum logic` |

### Building

```bash
dotnet build                                    # Debug build
dotnet build --configuration Release            # Release build
```

### Testing

```bash
dotnet test                                     # Run all tests
dotnet test --verbosity detailed                # Run with detailed output
dotnet test --filter "FullyQualifiedName~Csafe" # Run specific tests
```

### Packing

```bash
dotnet pack --configuration Release --output ./nupkg
```

## Coding Conventions

### General

- **Target framework**: .NET 10 (`net10.0`)
- **Nullable reference types**: Enabled globally
- **Implicit usings**: Enabled globally
- **Warnings as errors**: Enabled via `Directory.Build.props`
- Follow the `.editorconfig` for formatting rules

### C# Style

- Use **file-scoped namespaces** (`namespace ErgNet;`)
- Use **`var`** when the type is apparent
- Use **expression-bodied members** for single-line properties and methods
- Sort `using` directives with `System` namespaces first
- Use **4-space indentation** (no tabs)
- Use **LF line endings**

### Naming

- **Library name**: `ErgNet` — used for the NuGet package, root namespace, and assembly
- **Hardware references**: Use `Concept2` when referring to the hardware brand (e.g., `IsConcept2Device`, `ManufacturerName: "Concept2"`)
- Do not use `ErgNet` to describe Concept2 hardware — `ErgNet` is the library, `Concept2` is the manufacturer

### XML Documentation

- All public types and members must have XML documentation comments
- Use `<inheritdoc />` on interface implementations
- Include `<param>`, `<returns>`, and `<remarks>` tags where appropriate

### Testing

- **Framework**: xUnit
- **Pattern**: `MethodName_Scenario_ExpectedBehavior` (e.g., `ParseRowerData_TooShort_Throws`)
- **Fakes over mocks**: Use hand-written fake implementations (e.g., `FakeUsbTransport`) instead of mocking libraries
- Include byte-level comments in protocol parser tests to explain data structure
- Global `using Xunit;` is provided via the test project's `.csproj`

## Architecture

### Transport Abstraction

Users implement device-specific interfaces (`IHidDevice`, `IBleDevice`, `IAntDevice`) to integrate their preferred USB, BLE, or ANT+ library. The library wraps these in transport implementations (`UsbTransport`, `BluetoothTransport`, `AntTransport`).

### Protocol Layers

1. **Transport** — sends/receives raw bytes
2. **CSAFE** — frame building, parsing, command registry (USB and BLE)
3. **BLE** — parses GATT characteristic notification payloads
4. **ANT+** — parses FE-C data page broadcasts
5. **PerformanceMonitor** — unified high-level API

### Key Design Decisions

- **`IAsyncEnumerable<RowingData>`** for streaming: allows `await foreach` consumption with built-in cancellation
- **`SemaphoreSlim`** guards concurrent transport access in `PerformanceMonitor`
- **`Channel<T>`** merges multiple BLE notification streams
- **ANT+ is receive-only**: `SendAsync`/`ReceiveAsync` throw `NotSupportedException` on `AntTransport`

## Versioning

Semantic versioning is handled automatically by [GitVersion](https://gitversion.net/) using Conventional Commits:

- **`main` branch**: Pre-release versions with `alpha` label (e.g., `0.2.0-alpha.1`)
- **`release/*` branches**: Release candidate versions with `rc` label (e.g., `0.2.0-rc.1`)
- **Pull requests**: Pre-release versions with `alpha` label
- **Production promotion**: Stable versions (e.g., `0.2.0`) via manual workflow dispatch

## CI/CD Pipeline

| Workflow | Trigger | Actions |
|----------|---------|---------|
| **CI/CD** (`ci.yml`) | Push to `main`/`release/*`, PRs | Build → Test → Security scan → Pack. On push (merge) only: publish unlisted pre-release to NuGet.org. |
| **Promote** (`promote.yml`) | Manual dispatch | Build → Test → Pack → Publish to NuGet.org → Sign (repository) → Publish to GitHub Packages → Tag |

Both workflows use **OIDC trusted publishing** via `NuGet/login@v1` — no NuGet API keys are stored as secrets. NuGet.org applies its own **repository signature** to all packages automatically. GitHub Packages receives a **repository-signed** package (owner: `ergSoft`) — see [NuGet Package Signing](#nuget-package-signing) for setup.

## NuGet Package Signing

All packages published to NuGet.org receive a **repository signature** applied automatically by NuGet.org. For GitHub Packages, the promote workflow applies a **repository signature** with `ergSoft` as the package owner before publishing.

### Required GitHub Secrets

Configure the following secrets in the `production` GitHub environment:

| Secret | Description |
|--------|-------------|
| `NUGET_SIGNING_CERT` | Base64-encoded `.pfx` (PKCS #12) code signing certificate issued to `ergSoft`. Generate with `base64 -w0 cert.pfx` (Linux) or `base64 -i cert.pfx` (macOS). |
| `NUGET_SIGNING_CERT_PASSWORD` | Password for the `.pfx` file. |

### NuGet.org Certificate Registration

1. Export the **public key** from the signing certificate as a `.cer` (DER-encoded) file.
2. On [NuGet.org → Account Settings → Certificates](https://www.nuget.org/account/certificates), upload the `.cer` file.
3. On the [ErgNet package page → Manage](https://www.nuget.org/packages/ErgNet/Manage), verify the package owner is set to `ergSoft`.

NuGet.org applies a repository signature to every package. GitHub Packages receives a repository-signed package with `ergSoft` recorded as the package owner.

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
