namespace SaveState.Application.Mugen.Models.LiveSync;

/// <summary>
/// Unified account data across all platforms.
/// </summary>
public class UnifiedAccount
{
    public string AccountId { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string? ProfilePictureUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }
    public AccountStatus Status { get; set; }
    public Dictionary<PlatformType, PlatformAccount> LinkedPlatforms { get; set; } = default!;
    public AccountPreferences Preferences { get; set; } = default!;
    public UnifiedStatistics Statistics { get; set; } = default!;
}

/// <summary>
/// Request to create a new unified account.
/// </summary>
public class UnifiedAccountRequest
{
    public string Email { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string? ProfilePictureUrl { get; set; }
}

/// <summary>
/// Platform account information.
/// </summary>
public class PlatformAccount
{
    public PlatformType PlatformType { get; set; }
    public string PlatformUserId { get; set; } = default!;
    public string PlatformUsername { get; set; } = default!;
    public DateTime LinkedAt { get; set; }
    public DateTime LastSyncAt { get; set; }
    public SyncStatus SyncStatus { get; set; }
    public string? AuthToken { get; set; }
}

/// <summary>
/// Request to link a platform account.
/// </summary>
public class PlatformAccountLinkRequest
{
    public PlatformType PlatformType { get; set; }
    public string PlatformUserId { get; set; } = default!;
    public string PlatformUsername { get; set; } = default!;
    public string AuthToken { get; set; } = default!;
}

/// <summary>
/// Account preferences.
/// </summary>
public class AccountPreferences
{
    public string Theme { get; set; } = "auto";
    public string Language { get; set; } = "en";
    public string TimeZone { get; set; } = "UTC";
    public PrivacySettings PrivacySettings { get; set; } = default!;
}

/// <summary>
/// Privacy settings.
/// </summary>
public class PrivacySettings
{
    public Visibility ProfileVisibility { get; set; }
    public Visibility ActivityVisibility { get; set; }
    public bool DataSharing { get; set; }
}

/// <summary>
/// Unified statistics across platforms.
/// </summary>
public class UnifiedStatistics
{
    public TimeSpan TotalPlayTime { get; set; }
    public IReadOnlyList<PlatformType> PlatformsUsed { get; set; } = default!;
    public string? FavoriteCharacter { get; set; }
    public int AchievementCount { get; set; }
    public int FriendCount { get; set; }
}

/// <summary>
/// Cross-platform statistics.
/// </summary>
public class CrossPlatformStats
{
    public string AccountId { get; set; } = default!;
    public TimeSpan TotalPlayTime { get; set; }
    public int PlatformsUsed { get; set; }
    public IReadOnlyDictionary<PlatformType, PlatformStats> PlatformBreakdown { get; set; } = default!;
    public IReadOnlyList<string> CrossPlatformAchievements { get; set; } = default!;
    public double SyncHealth { get; set; }
    public double DataCompleteness { get; set; }
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Platform-specific statistics.
/// </summary>
public class PlatformStats
{
    public TimeSpan PlayTime { get; set; }
    public int MatchesPlayed { get; set; }
    public int AchievementsUnlocked { get; set; }
    public DateTime LastActive { get; set; }
}

/// <summary>
/// Backup data for account restoration.
/// </summary>
public class AccountBackup
{
    public string BackupId { get; set; } = default!;
    public string AccountId { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public string AccountData { get; set; } = default!;
    public Dictionary<PlatformType, string> PlatformData { get; set; } = default!;
    public long TotalSize { get; set; }
    public string Checksum { get; set; } = default!;
}
