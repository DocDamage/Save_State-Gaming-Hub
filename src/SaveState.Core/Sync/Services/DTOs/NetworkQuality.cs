namespace SaveState.Core.Sync.Services.DTOs;

/// <summary>
/// Represents network quality metrics for cloud gaming.
/// </summary>
public sealed record NetworkQuality(
    int LatencyMs,
    int JitterMs,
    int PacketLossPercent,
    int BandwidthMbps,
    QualityLevel Level,
    DateTime MeasuredAt);

/// <summary>
/// Network quality level for cloud gaming compatibility.
/// </summary>
public enum QualityLevel
{
    Poor,
    Fair,
    Good,
    Excellent
}