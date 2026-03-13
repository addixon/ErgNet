using System.Collections.Immutable;

namespace ErgNet.Protocol.Csafe;

/// <summary>
/// Stores metadata for a single CSAFE command including its name, command identifier,
/// and the byte layouts for request and response data.
/// </summary>
/// <param name="Name">Human-readable name of the command.</param>
/// <param name="CommandId">The command byte identifier.</param>
/// <param name="RequestDataBytes">
/// Number of bytes for each request argument.
/// An empty array means the command sends no additional data.
/// A zero entry indicates variable-length data.
/// </param>
/// <param name="ResponseDataBytes">
/// Number of bytes for each response field.
/// Negative values indicate ASCII string fields whose absolute value is the string length.
/// </param>
/// <param name="WrapperCommand">
/// Optional wrapper command byte (e.g., 0x1A for PM-specific commands).
/// When set, the command must be wrapped inside this outer command frame.
/// </param>
public sealed record CsafeCommandDefinition(
    string Name,
    byte CommandId,
    ImmutableArray<int> RequestDataBytes,
    ImmutableArray<int> ResponseDataBytes,
    byte? WrapperCommand = null);
