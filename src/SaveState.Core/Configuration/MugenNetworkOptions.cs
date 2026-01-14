using System.ComponentModel.DataAnnotations;

namespace SaveState.Core.Configuration;

/// <summary>
/// Configuration options for MUGEN Network features.
/// </summary>
public sealed class MugenNetworkOptions
{
    public const string SectionName = "MugenNetwork";

    /// <summary>
    /// Gets or sets whether MUGEN Network features are enabled.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the MUGEN Network API base URL.
    /// </summary>
    [Required]
    public string ApiBaseUrl { get; set; } = "https://api.mugen-community.net/v1/";

    /// <summary>
    /// Gets or sets the API key for MUGEN Network services.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API timeout in milliseconds.
    /// </summary>
    public int ApiTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Gets or sets whether to use the community workshop.
    /// </summary>
    public bool EnableWorkshop { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable online multiplayer matchmaking.
    /// </summary>
    public bool EnableMatchmaking { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable leaderboards.
    /// </summary>
    public bool EnableLeaderboards { get; set; } = true;

    /// <summary>
    /// Gets or sets the preferred matchmaking region.
    /// </summary>
    public string PreferredRegion { get; set; } = "Auto";

    /// <summary>
    /// Gets or sets the maximum ping for matchmaking (ms).
    /// </summary>
    public int MaxMatchmakingPing { get; set; } = 100;

    /// <summary>
    /// Gets or sets workshop content cache directory.
    /// </summary>
    public string WorkshopCacheDirectory { get; set; } = "MugenWorkshop";

    /// <summary>
    /// Gets or sets whether to auto-update workshop content.
    /// </summary>
    public bool AutoUpdateWorkshopContent { get; set; } = true;

    /// <summary>
    /// Gets or sets the user's player profile ID.
    /// </summary>
    public string PlayerProfileId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to participate in community events.
    /// </summary>
    public bool ParticipateInCommunityEvents { get; set; } = true;
}
