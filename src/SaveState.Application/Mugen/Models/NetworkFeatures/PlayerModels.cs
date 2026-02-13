namespace SaveState.Application.Mugen.Models.NetworkFeatures;

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
/// Player achievement.
/// </summary>
public record Achievement(
    string Name,
    string Description,
    DateTime UnlockedAt,
    AchievementRarity Rarity);

/// <summary>
/// Achievement rarity levels.
/// </summary>
public enum AchievementRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

/// <summary>
/// Player statistics for matchmaking.
/// </summary>
public class PlayerMatchmakingStats
{
    public string PlayerId { get; set; } = default!;
    public int Rating { get; set; } = default!;
    public decimal WinRate { get; set; } = default!;
    public int TotalMatches { get; set; } = default!;
    public IReadOnlyList<string> PreferredCharacters { get; set; } = default!;
    public decimal RecentPerformance { get; set; } = default!;
}

/// <summary>
/// Player network profile for connection management.
/// </summary>
public class PlayerNetworkProfile
{
    public string PlayerId { get; set; } = default!;
    public string ConnectionId { get; set; } = default!;
    public string IpAddress { get; set; } = default!;
    public int Port { get; set; }
    public string Region { get; set; } = default!;
    public NetworkQuality LastKnownQuality { get; set; }
    public DateTime ConnectedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
}
