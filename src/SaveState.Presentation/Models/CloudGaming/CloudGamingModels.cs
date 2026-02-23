namespace SaveState.Presentation.Models.CloudGaming;

/// <summary>
/// Cloud gaming service providers.
/// </summary>
public enum CloudProvider
{
    /// <summary>NVIDIA GeForce Now.</summary>
    GeForceNow,

    /// <summary>Xbox Cloud Gaming (formerly xCloud).</summary>
    XboxCloudGaming,

    /// <summary>Amazon Luna.</summary>
    AmazonLuna,

    /// <summary>Boosteroid.</summary>
    Boosteroid,

    /// <summary>Shadow PC.</summary>
    ShadowPC,

    /// <summary>Parsec (self-hosted).</summary>
    Parsec,

    /// <summary>Moonlight (open source).</summary>
    Moonlight,

    /// <summary>Google Stadia (deprecated).</summary>
    GoogleStadia
}

/// <summary>
/// Status of a cloud game.
/// </summary>
public enum CloudGameStatus
{
    /// <summary>Game is available to play.</summary>
    Available,

    /// <summary>Game is currently installing.</summary>
    Installing,

    /// <summary>Game is updating.</summary>
    Updating,

    /// <summary>Game is under maintenance.</summary>
    Maintenance,

    /// <summary>Game is unavailable.</summary>
    Unavailable,

    /// <summary>Game coming soon.</summary>
    ComingSoon
}

/// <summary>
/// Streaming quality preset.
/// </summary>
public enum SessionQuality
{
    /// <summary>720p 30fps - Low bandwidth.</summary>
    Low,

    /// <summary>1080p 30fps - Balanced.</summary>
    Medium,

    /// <summary>1080p 60fps - High quality.</summary>
    High,

    /// <summary>4K 60fps - Ultra quality.</summary>
    Ultra,

    /// <summary>Dynamic based on connection.</summary>
    Adaptive
}

/// <summary>
/// Represents a cloud gaming title.
/// </summary>
public class CloudGame
{
    /// <summary>Unique identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Game title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Game description.</summary>
    public string? Description { get; set; }

    /// <summary>Cover image URL.</summary>
    public string? CoverImage { get; set; }

    /// <summary>Cloud provider.</summary>
    public CloudProvider Provider { get; set; }

    /// <summary>Current availability status.</summary>
    public CloudGameStatus Status { get; set; }

    /// <summary>Game genres.</summary>
    public List<string> Genres { get; set; } = new();

    /// <summary>Estimated play time.</summary>
    public TimeSpan? EstimatedPlayTime { get; set; }

    /// <summary>When added to library.</summary>
    public DateTime? AddedToLibrary { get; set; }

    /// <summary>Last played timestamp.</summary>
    public DateTime? LastPlayed { get; set; }

    /// <summary>Total play time.</summary>
    public TimeSpan TotalPlayTime { get; set; }

    /// <summary>Whether game is favorited.</summary>
    public bool IsFavorite { get; set; }

    /// <summary>User rating (0-5).</summary>
    public float? UserRating { get; set; }

    /// <summary>Metacritic score (0-100).</summary>
    public int? MetacriticScore { get; set; }
}

/// <summary>
/// Represents a cloud gaming session.
/// </summary>
public class CloudSession
{
    /// <summary>Session ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Cloud provider.</summary>
    public CloudProvider Provider { get; set; }

    /// <summary>Game being played.</summary>
    public CloudGame? Game { get; set; }

    /// <summary>Session start time.</summary>
    public DateTime StartedAt { get; set; }

    /// <summary>Session duration.</summary>
    public TimeSpan Duration { get; set; }

    /// <summary>Stream quality setting.</summary>
    public SessionQuality Quality { get; set; }

    /// <summary>Average latency in ms.</summary>
    public float AverageLatency { get; set; }

    /// <summary>Packet loss percentage.</summary>
    public float PacketLoss { get; set; }

    /// <summary>Average bitrate in Mbps.</summary>
    public float AverageBitrate { get; set; }

    /// <summary>Stream resolution width.</summary>
    public int ResolutionWidth { get; set; }

    /// <summary>Stream resolution height.</summary>
    public int ResolutionHeight { get; set; }

    /// <summary>Current frame rate.</summary>
    public int FrameRate { get; set; }

    /// <summary>Whether session is active.</summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// Represents a cloud provider account status.
/// </summary>
public class ProviderStatus
{
    /// <summary>Cloud provider.</summary>
    public CloudProvider Provider { get; set; }

    /// <summary>Whether account is connected.</summary>
    public bool IsConnected { get; set; }

    /// <summary>Username/email.</summary>
    public string? Username { get; set; }

    /// <summary>Subscription tier name.</summary>
    public string? SubscriptionTier { get; set; }

    /// <summary>Number of games in library.</summary>
    public int GamesInLibrary { get; set; }

    /// <summary>Hours played this month.</summary>
    public int HoursPlayedThisMonth { get; set; }

    /// <summary>Monthly hour limit (null for unlimited).</summary>
    public int? HourLimit { get; set; }

    /// <summary>Subscription expiration date.</summary>
    public DateTime? SubscriptionExpires { get; set; }

    /// <summary>Available data centers.</summary>
    public List<DataCenter> AvailableDataCenters { get; set; } = new();

    /// <summary>Currently selected data center.</summary>
    public DataCenter? CurrentDataCenter { get; set; }

    /// <summary>Last connection test results.</summary>
    public ConnectionTestResult? LastConnectionTest { get; set; }
}

/// <summary>
/// Represents a cloud gaming data center.
/// </summary>
public class DataCenter
{
    /// <summary>Data center ID.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Geographic region.</summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>Ping in milliseconds.</summary>
    public int Ping { get; set; }

    /// <summary>Whether recommended for user location.</summary>
    public bool IsRecommended { get; set; }
}

/// <summary>
/// Results from a connection test.
/// </summary>
public class ConnectionTestResult
{
    /// <summary>Test timestamp.</summary>
    public DateTime TestedAt { get; set; }

    /// <summary>Ping in ms.</summary>
    public int Ping { get; set; }

    /// <summary>Jitter in ms.</summary>
    public float Jitter { get; set; }

    /// <summary>Packet loss percentage.</summary>
    public float PacketLoss { get; set; }

    /// <summary>Download speed in Mbps.</summary>
    public float DownloadSpeed { get; set; }

    /// <summary>Upload speed in Mbps.</summary>
    public float UploadSpeed { get; set; }

    /// <summary>Recommended quality based on test.</summary>
    public SessionQuality RecommendedQuality { get; set; }

    /// <summary>Whether 4K streaming is possible.</summary>
    public bool CanStream4K { get; set; }
}

/// <summary>
/// Stream configuration settings.
/// </summary>
public class StreamSettings
{
    /// <summary>Quality preset.</summary>
    public SessionQuality Quality { get; set; } = SessionQuality.High;

    /// <summary>Enable V-Sync.</summary>
    public bool VSync { get; set; } = true;

    /// <summary>Reduce motion for accessibility.</summary>
    public bool ReduceMotion { get; set; } = false;

    /// <summary>Enable HDR if available.</summary>
    public bool HDR { get; set; } = false;

    /// <summary>Bitrate in Mbps.</summary>
    public int BitrateMbps { get; set; } = 50;

    /// <summary>Show performance overlay.</summary>
    public bool ShowPerformanceStats { get; set; } = false;

    /// <summary>Enable microphone in stream.</summary>
    public bool EnableMicrophone { get; set; } = false;

    /// <summary>Selected controller device.</summary>
    public string? SelectedController { get; set; }
}

/// <summary>
/// Filter options for cloud games.
/// </summary>
public enum CloudGameFilter
{
    /// <summary>All games.</summary>
    All,

    /// <summary>Favorited games.</summary>
    Favorites,

    /// <summary>Recently played.</summary>
    RecentlyPlayed,

    /// <summary>Installed/Available.</summary>
    Installed,

    /// <summary>By specific provider.</summary>
    ByProvider
}

/// <summary>
/// Sort options for cloud games.
/// </summary>
public enum CloudGameSort
{
    /// <summary>Sort by name.</summary>
    Name,

    /// <summary>Sort by last played.</summary>
    LastPlayed,

    /// <summary>Sort by date added.</summary>
    DateAdded,

    /// <summary>Sort by rating.</summary>
    Rating,

    /// <summary>Sort by play time.</summary>
    PlayTime
}
