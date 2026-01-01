namespace SaveState.Core.Sync.Services.DTOs;

/// <summary>
/// Detailed network diagnostics information.
/// </summary>
public sealed record NetworkDiagnostics(
    string PublicIpAddress,
    string LocalIpAddress,
    string DnsServers,
    string Gateway,
    string SubnetMask,
    string NetworkAdapter,
    bool IsVpnActive,
    string? VpnProvider,
    IReadOnlyList<OpenPort> OpenPorts);

/// <summary>
/// Information about an open network port.
/// </summary>
public sealed record OpenPort(
    int PortNumber,
    string Protocol,
    string Service);

/// <summary>
/// Event arguments for network quality changes.
/// </summary>
public sealed class NetworkQualityChangedEventArgs : EventArgs
{
    public NetworkQuality PreviousQuality { get; init; } = null!;
    public NetworkQuality CurrentQuality { get; init; } = null!;
    public QualityChangeType ChangeType { get; init; }
}

/// <summary>
/// Type of network quality change.
/// </summary>
public enum QualityChangeType
{
    Improved,
    Degraded,
    SignificantDrop,
    Recovered
}