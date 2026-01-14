using System.ComponentModel.DataAnnotations;

namespace SaveState.Core.Configuration;

/// <summary>
/// Configuration options for cloud gaming services integration.
/// </summary>
public sealed class CloudGamingOptions
{
    public const string SectionName = "CloudGaming";

    /// <summary>
    /// Gets or sets whether cloud gaming features are enabled.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the default cloud gaming provider.
    /// </summary>
    public string DefaultProvider { get; set; } = "GeForceNow";

    /// <summary>
    /// Gets or sets GeForce NOW configuration.
    /// </summary>
    public GeForceNowOptions GeForceNow { get; set; } = new();

    /// <summary>
    /// Gets or sets Xbox Cloud Gaming configuration.
    /// </summary>
    public XboxCloudOptions XboxCloud { get; set; } = new();

    /// <summary>
    /// Gets or sets Amazon Luna configuration.
    /// </summary>
    public AmazonLunaOptions AmazonLuna { get; set; } = new();

    /// <summary>
    /// Gets or sets PlayStation Now configuration.
    /// </summary>
    public PlayStationNowOptions PlayStationNow { get; set; } = new();

    /// <summary>
    /// Gets or sets Shadow PC configuration.
    /// </summary>
    public ShadowPCOptions ShadowPC { get; set; } = new();

    /// <summary>
    /// Gets or sets network quality monitoring options.
    /// </summary>
    public NetworkMonitoringOptions NetworkMonitoring { get; set; } = new();
}

/// <summary>
/// Configuration for NVIDIA GeForce NOW service.
/// </summary>
public sealed class GeForceNowOptions
{
    /// <summary>
    /// Gets or sets whether GeForce NOW integration is enabled.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the GeForce NOW API key (if available).
    /// Note: GeForce NOW doesn't have a public API yet, this is for future use.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the preferred GeForce NOW region.
    /// </summary>
    public string PreferredRegion { get; set; } = "US-West";

    /// <summary>
    /// Gets or sets the quality preset (Competitive, Balanced, Quality, etc.).
    /// </summary>
    public string QualityPreset { get; set; } = "Balanced";
}

/// <summary>
/// Configuration for Xbox Cloud Gaming (xCloud) service.
/// </summary>
public sealed class XboxCloudOptions
{
    /// <summary>
    /// Gets or sets whether Xbox Cloud Gaming integration is enabled.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the Xbox Live account email.
    /// </summary>
    public string AccountEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether Xbox Game Pass Ultimate subscription is active.
    /// </summary>
    public bool HasGamePassUltimate { get; set; } = false;

    /// <summary>
    /// Gets or sets the preferred server region.
    /// </summary>
    public string PreferredRegion { get; set; } = "US-East";
}

/// <summary>
/// Configuration for Amazon Luna service.
/// </summary>
public sealed class AmazonLunaOptions
{
    /// <summary>
    /// Gets or sets whether Amazon Luna integration is enabled.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the Amazon Luna API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Luna+ subscription status.
    /// </summary>
    public bool HasLunaPlus { get; set; } = false;

    /// <summary>
    /// Gets or sets the preferred channel (Luna+, Ubisoft+, etc.).
    /// </summary>
    public string PreferredChannel { get; set; } = "Luna+";
}

/// <summary>
/// Configuration for PlayStation Now service.
/// </summary>
public sealed class PlayStationNowOptions
{
    /// <summary>
    /// Gets or sets whether PlayStation Now integration is enabled.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the PlayStation Network account ID.
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether PS Plus Premium subscription is active.
    /// </summary>
    public bool HasPSPlusPremium { get; set; } = false;
}

/// <summary>
/// Configuration for Shadow PC cloud gaming service.
/// </summary>
public sealed class ShadowPCOptions
{
    /// <summary>
    /// Gets or sets whether Shadow PC integration is enabled.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the Shadow PC account email.
    /// </summary>
    public string AccountEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Shadow PC API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Shadow PC subscription tier.
    /// </summary>
    public string SubscriptionTier { get; set; } = "Shadow PC";

    /// <summary>
    /// Gets or sets the preferred datacenter location.
    /// </summary>
    public string PreferredDatacenter { get; set; } = string.Empty;
}

/// <summary>
/// Configuration for network quality monitoring.
/// </summary>
public sealed class NetworkMonitoringOptions
{
    /// <summary>
    /// Gets or sets whether continuous network monitoring is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the monitoring interval in seconds.
    /// </summary>
    public int MonitoringIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets whether to store historical network quality data.
    /// </summary>
    public bool StoreHistoricalData { get; set; } = true;

    /// <summary>
    /// Gets or sets how many days of historical data to retain.
    /// </summary>
    public int HistoricalDataRetentionDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets the minimum latency threshold (ms) for acceptable quality.
    /// </summary>
    public int MinimumLatencyMs { get; set; } = 100;

    /// <summary>
    /// Gets or sets the minimum bandwidth (Mbps) for acceptable quality.
    /// </summary>
    public int MinimumBandwidthMbps { get; set; } = 10;

    /// <summary>
    /// Gets or sets the maximum packet loss (%) for acceptable quality.
    /// </summary>
    public double MaximumPacketLossPercent { get; set; } = 2.0;
}
