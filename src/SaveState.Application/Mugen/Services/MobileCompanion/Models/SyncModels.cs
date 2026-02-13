namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Device sync request.
/// </summary>
public class MobileCompanionServiceDeviceSyncRequest
{
    public IReadOnlyDictionary<string, object> Settings { get; set; } = default!;
    public IReadOnlyDictionary<string, object> ProgressData { get; set; } = default!;
    public IReadOnlyList<MobileCompanionServiceAchievement> Achievements { get; set; } = default!;
    public IReadOnlyDictionary<string, object>? FriendsData { get; set; } = default!;
    public IReadOnlyDictionary<string, object>? ContentData { get; set; } = default!;
}

/// <summary>
/// Device sync data.
/// </summary>
public class MobileCompanionServiceDeviceSyncData
{
    public string SessionId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public DateTime LastSyncTimestamp { get; set; } = default!;
    public MobileCompanionServiceSettingsSyncResult SettingsSync { get; set; } = default!;
    public MobileCompanionServiceProgressSyncResult ProgressSync { get; set; } = default!;
    public MobileCompanionServiceAchievementSyncResult AchievementsSync { get; set; } = default!;
    public MobileCompanionServiceFriendsSyncResult? FriendsSync { get; set; } = default!;
    public MobileCompanionServiceContentSyncResult? ContentSync { get; set; } = default!;
}

/// <summary>
/// Settings sync result.
/// </summary>
public class MobileCompanionServiceSettingsSyncResult
{
    public int SyncedSettings { get; set; } = default!;
    public int ConflictsResolved { get; set; } = default!;
    public string LastSyncVersion { get; set; } = default!;
}

/// <summary>
/// Progress sync result.
/// </summary>
public class MobileCompanionServiceProgressSyncResult
{
    public int SyncedAchievements { get; set; } = default!;
    public int SyncedStats { get; set; } = default!;
    public int NewHighScores { get; set; } = default!;
}

/// <summary>
/// Achievement sync result.
/// </summary>
public class MobileCompanionServiceAchievementSyncResult
{
    public int NewAchievements { get; set; } = default!;
    public int UpdatedAchievements { get; set; } = default!;
    public int TotalAchievements { get; set; } = default!;
}

/// <summary>
/// Friends sync result.
/// </summary>
public class MobileCompanionServiceFriendsSyncResult
{
    public int NewFriends { get; set; } = default!;
    public int UpdatedFriends { get; set; } = default!;
    public int FriendRequests { get; set; } = default!;
}

/// <summary>
/// Content sync result.
/// </summary>
public class MobileCompanionServiceContentSyncResult
{
    public int DownloadedContent { get; set; } = default!;
    public int PendingDownloads { get; set; } = default!;
    public int SyncConflicts { get; set; } = default!;
}
