using SaveState.Core.Common;

namespace SaveState.Core.TournamentManagement.Models;

/// <summary>
/// Represents a tournament.
/// </summary>
public record Tournament
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string GameId { get; init; } = string.Empty;
    public string GameName { get; init; } = string.Empty;
    public TournamentFormat Format { get; init; }
    public TournamentStatus Status { get; init; } = TournamentStatus.Draft;
    public DateTime RegistrationStart { get; init; }
    public DateTime RegistrationEnd { get; init; }
    public DateTime TournamentStart { get; init; }
    public DateTime? TournamentEnd { get; init; }
    public int MaxParticipants { get; init; }
    public int CurrentParticipants { get; init; }
    public string OrganizerId { get; init; } = string.Empty;
    public string OrganizerName { get; init; } = string.Empty;
    public PrizePool? PrizePool { get; init; }
    public TournamentRules Rules { get; init; } = new();
    public IReadOnlyList<TournamentParticipant> Participants { get; init; } = Array.Empty<TournamentParticipant>();
    public IReadOnlyList<TournamentMatch> Matches { get; init; } = Array.Empty<TournamentMatch>();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Tournament formats.
/// </summary>
public enum TournamentFormat
{
    SingleElimination,
    DoubleElimination,
    RoundRobin,
    Swiss,
    League,
    GroupStage,
    BattleRoyale,
    Custom
}

/// <summary>
/// Tournament status.
/// </summary>
public enum TournamentStatus
{
    Draft,
    RegistrationOpen,
    RegistrationClosed,
    InProgress,
    Paused,
    Completed,
    Cancelled
}

/// <summary>
/// Represents a tournament participant.
/// </summary>
public record TournamentParticipant
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string UserId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? TeamId { get; init; }
    public string? TeamName { get; init; }
    public int Seed { get; init; }
    public ParticipantStatus Status { get; init; } = ParticipantStatus.Registered;
    public DateTime RegisteredAt { get; init; } = DateTime.UtcNow;
    public IReadOnlyDictionary<string, object> Stats { get; init; } = new Dictionary<string, object>();
}

/// <summary>
/// Participant status.
/// </summary>
public enum ParticipantStatus
{
    Registered,
    CheckedIn,
    Disqualified,
    Withdrawn,
    Active,
    Eliminated
}

/// <summary>
/// Represents a tournament match.
/// </summary>
public record TournamentMatch
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string TournamentId { get; init; } = string.Empty;
    public int Round { get; init; }
    public int MatchNumber { get; init; }
    public string? BracketSection { get; init; }
    public MatchStatus Status { get; init; } = MatchStatus.Scheduled;
    public string? Participant1Id { get; init; }
    public string? Participant2Id { get; init; }
    public string? Participant1Name { get; init; }
    public string? Participant2Name { get; init; }
    public int? Participant1Score { get; init; }
    public int? Participant2Score { get; init; }
    public string? WinnerId { get; init; }
    public string? LoserId { get; init; }
    public DateTime? ScheduledTime { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? NextMatchId { get; init; }
    public string? NextMatchSlot { get; init; }
    public IReadOnlyList<GameResult> GameResults { get; init; } = Array.Empty<GameResult>();
    public string? StreamUrl { get; init; }
}

/// <summary>
/// Match status.
/// </summary>
public enum MatchStatus
{
    Scheduled,
    Ready,
    InProgress,
    Paused,
    Completed,
    Cancelled,
    Forfeited,
    Bye
}

/// <summary>
/// Represents a game result within a match.
/// </summary>
public record GameResult
{
    public int GameNumber { get; init; }
    public string? WinnerId { get; init; }
    public int? Participant1Score { get; init; }
    public int? Participant2Score { get; init; }
    public string? ReplayUrl { get; init; }
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}

/// <summary>
/// Represents tournament rules.
/// </summary>
public record TournamentRules
{
    public int BestOf { get; init; } = 3;
    public TimeSpan? MatchTimeLimit { get; init; }
    public bool AllowDraws { get; init; } = false;
    public int MaxRounds { get; init; }
    public IReadOnlyList<string> AllowedMaps { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> BannedItems { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> RequiredCheckIn { get; init; } = Array.Empty<string>();
    public string? TiebreakerRules { get; init; }
    public IReadOnlyDictionary<string, string> CustomRules { get; init; } = new Dictionary<string, string>();
}

/// <summary>
/// Represents a prize pool.
/// </summary>
public record PrizePool
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "USD";
    public PrizeDistributionType DistributionType { get; init; } = PrizeDistributionType.Standard;
    public IReadOnlyList<PrizeAllocation> Allocations { get; init; } = Array.Empty<PrizeAllocation>();
    public IReadOnlyList<PrizeContributor> Contributors { get; init; } = Array.Empty<PrizeContributor>();
}

/// <summary>
/// Prize distribution types.
/// </summary>
public enum PrizeDistributionType
{
    Standard,
    WinnerTakeAll,
    Equal,
    Custom
}

/// <summary>
/// Represents a prize allocation.
/// </summary>
public record PrizeAllocation
{
    public int Place { get; init; }
    public decimal Amount { get; init; }
    public string? ItemName { get; init; }
    public string? ItemDescription { get; init; }
    public string? RecipientId { get; init; }
}

/// <summary>
/// Represents a prize pool contributor.
/// </summary>
public record PrizeContributor
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string UserId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public DateTime ContributedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a tournament bracket.
/// </summary>
public record TournamentBracket
{
    public string TournamentId { get; init; } = string.Empty;
    public TournamentFormat Format { get; init; }
    public IReadOnlyList<BracketRound> Rounds { get; init; } = Array.Empty<BracketRound>();
    public IReadOnlyList<TournamentMatch> Matches { get; init; } = Array.Empty<TournamentMatch>();
    public BracketPosition? CurrentPosition { get; init; }
}

/// <summary>
/// Represents a bracket round.
/// </summary>
public record BracketRound
{
    public int RoundNumber { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsWinnersBracket { get; init; } = true;
    public int MatchesCount { get; init; }
    public bool IsFinal { get; init; } = false;
}

/// <summary>
/// Current position in bracket.
/// </summary>
public record BracketPosition
{
    public int CurrentRound { get; init; }
    public int TotalRounds { get; init; }
    public int MatchesCompleted { get; init; }
    public int MatchesRemaining { get; init; }
}

/// <summary>
/// Request to create a tournament.
/// </summary>
public record CreateTournamentRequest
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string GameId { get; init; } = string.Empty;
    public TournamentFormat Format { get; init; }
    public DateTime RegistrationStart { get; init; }
    public DateTime RegistrationEnd { get; init; }
    public DateTime TournamentStart { get; init; }
    public int MaxParticipants { get; init; }
    public TournamentRules Rules { get; init; } = new();
    public PrizePool? InitialPrizePool { get; init; }
}

/// <summary>
/// Represents tournament standings.
/// </summary>
public record TournamentStandings
{
    public string TournamentId { get; init; } = string.Empty;
    public IReadOnlyList<StandingEntry> Entries { get; init; } = Array.Empty<StandingEntry>();
}

/// <summary>
/// Individual standing entry.
/// </summary>
public record StandingEntry
{
    public int Position { get; init; }
    public string ParticipantId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public int Wins { get; init; }
    public int Losses { get; init; }
    public int Draws { get; init; }
    public int Points { get; init; }
    public decimal? PrizeAmount { get; init; }
}

/// <summary>
/// Represents a tournament schedule.
/// </summary>
public record TournamentSchedule
{
    public string TournamentId { get; init; } = string.Empty;
    public IReadOnlyList<ScheduledMatch> ScheduledMatches { get; init; } = Array.Empty<ScheduledMatch>();
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Scheduled match details.
/// </summary>
public record ScheduledMatch
{
    public string MatchId { get; init; } = string.Empty;
    public DateTime ScheduledTime { get; init; }
    public int Round { get; init; }
    public string? Participant1Name { get; init; }
    public string? Participant2Name { get; init; }
    public string? StreamUrl { get; init; }
}
