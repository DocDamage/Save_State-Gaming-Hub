using SaveState.Core.Common;

namespace SaveState.Core.Mugen.Services;

/// <summary>
/// Service for network features including online play, matchmaking, and community.
/// </summary>
public interface INetworkFeaturesService
{
    /// <summary>
    /// Finds and joins online matches.
/// </summary>
    Task<Result<MatchmakingResult>> FindMatchAsync(MatchmakingRequest request, CancellationToken ct = default);

    /// <summary>
    /// Creates a custom online lobby.
/// </summary>
    Task<Result<LobbyInfo>> CreateLobbyAsync(LobbyCreationRequest request, CancellationToken ct = default);

    /// <summary>
    /// Joins an existing lobby.
/// </summary>
    Task<Result<LobbyInfo>> JoinLobbyAsync(string lobbyCode, CancellationToken ct = default);

    /// <summary>
    /// Gets available lobbies.
/// </summary>
    Task<Result<IReadOnlyList<LobbyInfo>>> GetAvailableLobbiesAsync(LobbyFilter filter, CancellationToken ct = default);

    /// <summary>
    /// Starts spectating a match.
/// </summary>
    Task<Result<SpectatorSession>> StartSpectatingAsync(string matchId, CancellationToken ct = default);

    /// <summary>
    /// Gets global leaderboards.
/// </summary>
    Task<Result<IReadOnlyList<LeaderboardEntry>>> GetLeaderboardsAsync(LeaderboardType type, CancellationToken ct = default);

    /// <summary>
    /// Reports a player for inappropriate behavior.
/// </summary>
    Task<Result> ReportPlayerAsync(string playerId, ReportReason reason, string description, CancellationToken ct = default);

    /// <summary>
    /// Gets player reputation and statistics.
/// </summary>
    Task<Result<PlayerProfile>> GetPlayerProfileAsync(string playerId, CancellationToken ct = default);

    /// <summary>
    /// Manages friend relationships.
/// </summary>
    Task<Result> ManageFriendshipAsync(string friendId, FriendshipAction action, CancellationToken ct = default);

    /// <summary>
    /// Gets friend list.
/// </summary>
    Task<Result<IReadOnlyList<FriendInfo>>> GetFriendsAsync(CancellationToken ct = default);

    /// <summary>
    /// Sends a message in chat.
/// </summary>
    Task<Result> SendChatMessageAsync(string message, ChatChannel channel, string? targetId, CancellationToken ct = default);

    /// <summary>
    /// Gets recent chat messages.
/// </summary>
    Task<Result<IReadOnlyList<ChatMessage>>> GetChatMessagesAsync(ChatChannel channel, int count, CancellationToken ct = default);

    /// <summary>
    /// Shares replay data.
/// </summary>
    Task<Result<string>> ShareReplayAsync(string matchId, CancellationToken ct = default);

    /// <summary>
    /// Downloads a shared replay.
/// </summary>
    Task<Result<ReplayData>> DownloadReplayAsync(string replayId, CancellationToken ct = default);
}

/// <summary>
/// Request for matchmaking.
/// </summary>
public record MatchmakingRequest(
    string PlayerId,
    string CharacterName,
    MatchmakingMode Mode,
    MatchmakingPreferences Preferences,
    TimeSpan Timeout);

/// <summary>
/// Matchmaking modes.
/// </summary>
public enum MatchmakingMode
{
    Ranked,
    Casual,
    Tournament,
    Private
}

/// <summary>
/// Player matchmaking preferences.
/// </summary>
public record MatchmakingPreferences(
    int? MinRating,
    int? MaxRating,
    IReadOnlyList<string> PreferredCharacters,
    IReadOnlyList<string> AvoidedCharacters,
    bool AllowCrossplay,
    string Region);

/// <summary>
/// Result of matchmaking.
/// </summary>
public record MatchmakingResult(
    bool MatchFound,
    string? MatchId,
    string? OpponentId,
    string? OpponentName,
    TimeSpan? WaitTime,
    string? ErrorMessage);

/// <summary>
/// Request to create a lobby.
/// </summary>
public record LobbyCreationRequest(
    string HostId,
    string LobbyName,
    LobbySettings Settings,
    string? Password);

/// <summary>
/// Lobby settings.
/// </summary>
public record LobbySettings(
    int MaxPlayers,
    bool IsPrivate,
    string GameMode,
    string Rules,
    bool AllowSpectators,
    int TimeLimitMinutes);

/// <summary>
/// Information about a lobby.
/// </summary>
public record LobbyInfo(
    string LobbyId,
    string LobbyCode,
    string HostName,
    string LobbyName,
    LobbySettings Settings,
    IReadOnlyList<LobbyPlayer> Players,
    LobbyStatus Status);

/// <summary>
/// Player in a lobby.
/// </summary>
public record LobbyPlayer(
    string PlayerId,
    string PlayerName,
    string CharacterName,
    bool IsReady,
    bool IsHost);

/// <summary>
/// Lobby status.
/// </summary>
public enum LobbyStatus
{
    Waiting,
    Starting,
    InProgress,
    Finished
}

/// <summary>
/// Filter for lobby search.
/// </summary>
public record LobbyFilter(
    string? GameMode,
    bool? PrivateOnly,
    int? MinPlayers,
    int? MaxPlayers,
    string? Region);

/// <summary>
/// Spectator session.
/// </summary>
public record SpectatorSession(
    string SessionId,
    string MatchId,
    string StreamUrl,
    IReadOnlyList<SpectatorControls> Controls);

/// <summary>
/// Controls available to spectators.
/// </summary>
public record SpectatorControls(
    string ControlType,
    string Description,
    bool Enabled);

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
/// Entry in a leaderboard.
/// </summary>
public record LeaderboardEntry(
    int Rank,
    string PlayerId,
    string PlayerName,
    int Rating,
    int Wins,
    int Losses,
    decimal WinRate,
    string? CharacterName);

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
/// Player profile information.
/// </summary>
public record PlayerProfile(
    string PlayerId,
    string PlayerName,
    int Rating,
    string Rank,
    IReadOnlyList<Achievement> Achievements,
    PlayerStats Stats,
    Reputation Reputation,
    IReadOnlyList<string> FavoriteCharacters,
    string? StatusMessage,
    string? AvatarUrl,
    PlayerOnlineStatus Status,
    string? CurrentActivity,
    string? Region);

/// <summary>
/// Player statistics.
/// </summary>
public record PlayerStats(
    int TotalMatches,
    int Wins,
    int Losses,
    decimal WinRate,
    TimeSpan TotalPlayTime,
    IReadOnlyDictionary<string, CharacterSpecificStats> CharacterStats);

/// <summary>
/// Character-specific statistics.
/// </summary>
public record CharacterSpecificStats(
    int Matches,
    int Wins,
    int Losses,
    decimal WinRate,
    int FavoriteMove);

/// <summary>
/// Player reputation.
/// </summary>
public record Reputation(
    int Score,
    ReputationTier Tier,
    IReadOnlyList<string> Badges,
    DateTime LastReported);

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
/// Friend information.
/// </summary>
public record FriendInfo(
    string FriendId,
    string FriendName,
    FriendshipStatus Status,
    DateTime FriendsSince,
    bool IsOnline,
    string? CurrentActivity);

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
/// Chat message.
/// </summary>
public record ChatMessage(
    string MessageId,
    string SenderId,
    string SenderName,
    string Message,
    ChatChannel Channel,
    DateTime Timestamp,
    string? TargetId);

/// <summary>
/// Replay data.
/// </summary>
public record ReplayData(
    string ReplayId,
    string MatchId,
    string Player1Name,
    string Player2Name,
    string Player1Character,
    string Player2Character,
    byte[] Data,
    DateTime RecordedAt,
    TimeSpan Duration);
