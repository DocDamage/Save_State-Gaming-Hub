using SaveState.Core.Common.Base;
using SaveState.Core.Sync.Services.DTOs;

namespace SaveState.Core.Sync.Entities;

/// <summary>
/// Represents a historical record of network quality measurements for cloud gaming.
/// Provides persistence for network quality data across application restarts.
/// </summary>
public class NetworkQualityHistory : EntityBase
{
    /// <summary>
    /// Gets the network latency in milliseconds.
    /// </summary>
    public int LatencyMs { get; private set; }

    /// <summary>
    /// Gets the network jitter in milliseconds.
    /// </summary>
    public int JitterMs { get; private set; }

    /// <summary>
    /// Gets the packet loss percentage.
    /// </summary>
    public int PacketLossPercent { get; private set; }

    /// <summary>
    /// Gets the estimated bandwidth in Mbps.
    /// </summary>
    public int BandwidthMbps { get; private set; }

    /// <summary>
    /// Gets the quality level based on the measurements.
    /// </summary>
    public QualityLevel Level { get; private set; }

    /// <summary>
    /// Gets the timestamp when the measurement was taken.
    /// </summary>
    public DateTime MeasuredAt { get; private set; }

    /// <summary>
    /// Gets the session identifier for grouping related measurements.
    /// </summary>
    public Guid? SessionId { get; private set; }

    private NetworkQualityHistory() { }

    /// <summary>
    /// Creates a new network quality history record from a NetworkQuality DTO.
    /// </summary>
    /// <param name="quality">The network quality measurement to record.</param>
    /// <param name="sessionId">Optional session identifier for grouping.</param>
    /// <returns>A new NetworkQualityHistory entity.</returns>
    public static NetworkQualityHistory Create(NetworkQuality quality, Guid? sessionId = null)
    {
        return new NetworkQualityHistory
        {
            Id = Guid.NewGuid(),
            LatencyMs = quality.LatencyMs,
            JitterMs = quality.JitterMs,
            PacketLossPercent = quality.PacketLossPercent,
            BandwidthMbps = quality.BandwidthMbps,
            Level = quality.Level,
            MeasuredAt = quality.MeasuredAt,
            SessionId = sessionId
        };
    }

    /// <summary>
    /// Converts this entity to a NetworkQuality DTO.
    /// </summary>
    /// <returns>A NetworkQuality DTO representing this history record.</returns>
    public NetworkQuality ToDto()
    {
        return new NetworkQuality(
            LatencyMs: LatencyMs,
            JitterMs: JitterMs,
            PacketLossPercent: PacketLossPercent,
            BandwidthMbps: BandwidthMbps,
            Level: Level,
            MeasuredAt: MeasuredAt
        );
    }
}
