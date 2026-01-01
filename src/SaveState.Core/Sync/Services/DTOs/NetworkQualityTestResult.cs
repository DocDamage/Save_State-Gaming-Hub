using SaveState.Core.Sync.Services.DTOs;

namespace SaveState.Core.Sync.Services.DTOs;

/// <summary>
/// Result of a comprehensive network quality test.
/// </summary>
public sealed record NetworkQualityTestResult(
    NetworkQuality CurrentQuality,
    IReadOnlyList<PingTestResult> PingTests,
    IReadOnlyList<SpeedTestResult> SpeedTests,
    IReadOnlyList<string> Recommendations,
    DateTime TestCompletedAt);

/// <summary>
/// Result of a ping test to a specific endpoint.
/// </summary>
public sealed record PingTestResult(
    string Endpoint,
    int LatencyMs,
    int PacketLossPercent,
    bool Success);

/// <summary>
/// Result of a speed test.
/// </summary>
public sealed record SpeedTestResult(
    string Server,
    int DownloadSpeedMbps,
    int UploadSpeedMbps,
    bool Success);