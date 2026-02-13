namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Mobile platform types.
/// </summary>
public enum MobileCompanionServiceMobilePlatform
{
    iOS,
    Android,
    Windows,
    macOS
}

/// <summary>
/// Permission types for mobile companion.
/// </summary>
public enum MobileCompanionServicePermission
{
    RemoteControl,
    RealTimeStats,
    Notifications,
    SocialFeatures,
    ContentManagement
}

/// <summary>
/// Session status values.
/// </summary>
public enum MobileCompanionServiceSessionStatus
{
    Active,
    Inactive,
    Suspended,
    Terminated
}

/// <summary>
/// Command types for remote control.
/// </summary>
public enum MobileCompanionServiceCommandType
{
    StartMatch,
    PauseGame,
    SelectCharacter,
    ChangeStage,
    SendChat,
    SpectateMatch
}

/// <summary>
/// Activity types.
/// </summary>
public enum MobileCompanionServiceActivityType
{
    MatchCompleted,
    AchievementUnlocked,
    ContentDownloaded,
    FriendRequest,
    TournamentJoined
}

/// <summary>
/// Notification types.
/// </summary>
public enum MobileCompanionServiceNotificationType
{
    Match,
    Tournament,
    Social,
    System,
    MobileCompanionServiceAchievement
}

/// <summary>
/// Notification priority levels.
/// </summary>
public enum MobileCompanionServiceNotificationPriority
{
    Low,
    Normal,
    High,
    Critical
}

/// <summary>
/// Social activity types.
/// </summary>
public enum MobileCompanionServiceSocialActivityType
{
    MatchResult,
    MobileCompanionServiceAchievement,
    StatusUpdate,
    ContentShare,
    TournamentResult
}

/// <summary>
/// Content types.
/// </summary>
public enum MobileCompanionServiceContentType
{
    Character,
    Stage,
    Music,
    Effect,
    Tutorial
}

/// <summary>
/// Download status values.
/// </summary>
public enum MobileCompanionServiceDownloadStatus
{
    Pending,
    Downloading,
    Paused,
    Completed,
    Failed
}

/// <summary>
/// Match event types.
/// </summary>
public enum MobileCompanionServiceMatchEventType
{
    Hit,
    Block,
    Throw,
    SpecialMove,
    SuperMove,
    RoundEnd
}

/// <summary>
/// Quick action types.
/// </summary>
public enum MobileCompanionServiceQuickActionType
{
    MobileCompanionServiceRemoteCommand,
    SocialAction,
    ContentAction,
    Navigation
}

/// <summary>
/// Device type enumeration.
/// </summary>
public enum MobileCompanionServiceDeviceType
{
    Phone,
    Tablet,
    Watch,
    Desktop,
    Web
}
