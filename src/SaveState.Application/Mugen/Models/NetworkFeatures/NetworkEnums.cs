namespace SaveState.Application.Mugen.Models.NetworkFeatures;

/// <summary>
/// Matchmaking modes for online play.
/// </summary>
public enum MatchmakingMode
{
    Ranked,
    Casual,
    Tournament,
    Private
}

/// <summary>
/// Lobby status states.
/// </summary>
public enum LobbyStatus
{
    Waiting,
    Starting,
    InProgress,
    Finished
}

/// <summary>
/// Network quality levels for connection monitoring.
/// </summary>
public enum NetworkQuality
{
    Excellent,
    Good,
    Fair,
    Poor,
    Critical
}

/// <summary>
/// Leaderboard types.
/// </summary>
public enum LeaderboardType
{
    Global,
    Regional,
    CharacterSpecific,
    Tournament,
    Seasonal
}

/// <summary>
/// Reasons for reporting a player.
/// </summary>
public enum ReportReason
{
    Cheating,
    Harassment,
    InappropriateBehavior,
    Spam,
    Griefing,
    Other
}

/// <summary>
/// Reputation tiers.
/// </summary>
public enum ReputationTier
{
    Toxic,
    Poor,
    Neutral,
    Good,
    Excellent
}

/// <summary>
/// Friendship actions.
/// </summary>
public enum FriendshipAction
{
    Add,
    Remove,
    Block,
    Unblock
}

/// <summary>
/// Friendship status.
/// </summary>
public enum FriendshipStatus
{
    Pending,
    Accepted,
    Blocked,
    Declined,
    Removed
}

/// <summary>
/// Player online status enumeration.
/// </summary>
public enum PlayerOnlineStatus
{
    Offline,
    Online,
    Away,
    InGame,
    Busy
}

/// <summary>
/// Chat channels.
/// </summary>
public enum ChatChannel
{
    Global,
    Lobby,
    Party,
    Whisper
}

/// <summary>
/// Report status.
/// </summary>
public enum ReportStatus
{
    Pending,
    Investigating,
    Resolved,
    Dismissed
}

/// <summary>
/// Network session states.
/// </summary>
public enum NetworkSessionState
{
    Initializing,
    Connecting,
    Connected,
    Reconnecting,
    Disconnected,
    Error
}

/// <summary>
/// Relay server regions.
/// </summary>
public enum RelayRegion
{
    NorthAmerica,
    Europe,
    Asia,
    SouthAmerica,
    Oceania,
    Global
}
