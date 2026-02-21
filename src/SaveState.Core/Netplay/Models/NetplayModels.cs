namespace SaveState.Core.Netplay.Models;

public enum NetplayRegion
{
    NorthAmerica,
    Europe,
    Asia,
    SouthAmerica,
    Oceania,
    Global
}

public enum SkillRating
{
    Bronze,
    Silver,
    Gold,
    Platinum,
    Diamond,
    Master,
    GrandMaster
}

public record MatchmakingRequest
{
    public required string GameId { get; init; }
    public required string RomHash { get; init; }
    public required NetplayRegion Region { get; init; }
    public required SkillRating Rating { get; init; }
    public required IReadOnlyList<string> PreferredRules { get; init; }
    public required TimeSpan MaxWaitTime { get; init; }
}

public record MatchmakingTicket
{
    public required string TicketId { get; init; }
    public required MatchmakingRequest Request { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required MatchmakingStatus Status { get; init; }
    public required string? MatchedPeerId { get; init; }
    public required DateTime? MatchedAt { get; init; }
}

public enum MatchmakingStatus
{
    Queued,
    Searching,
    Found,
    Expired,
    Cancelled
}

public record NetplaySession
{
    public required string SessionId { get; init; }
    public required string GameId { get; init; }
    public required IReadOnlyList<NetplayPeer> Peers { get; init; }
    public required DateTime StartedAt { get; init; }
    public required NetplaySessionStatus Status { get; init; }
    public required RollbackConfig RollbackConfig { get; init; }
}

public record NetplayPeer
{
    public required string PeerId { get; init; }
    public required string DisplayName { get; init; }
    public required SkillRating Rating { get; init; }
    public required int Ping { get; init; }
    public required string IpAddress { get; init; }
    public required int Port { get; init; }
}

public enum NetplaySessionStatus
{
    Connecting,
    Synchronizing,
    Active,
    Paused,
    Disconnected,
    Finished
}

public record RollbackConfig
{
    public required int InputDelay { get; init; }
    public required int MaxRollbackFrames { get; init; }
    public required int LocalInputDelay { get; init; }
}

public record RomHashVerification
{
    public required string GameId { get; init; }
    public required string RomHash { get; init; }
    public required bool IsValid { get; init; }
    public required DateTime VerifiedAt { get; init; }
}

public record LeaderboardEntry
{
    public required string PlayerId { get; init; }
    public required string DisplayName { get; init; }
    public required int Rank { get; init; }
    public required int Rating { get; init; }
    public required int Wins { get; init; }
    public required int Losses { get; init; }
    public required int WinStreak { get; init; }
}
