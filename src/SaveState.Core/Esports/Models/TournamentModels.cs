using SaveState.Core.Common.Services;

namespace SaveState.Core.Esports.Models;

/// <summary>
/// Tournament bracket formats.
/// </summary>
public enum TournamentFormat
{
    SingleElimination,
    DoubleElimination,
    RoundRobin,
    Swiss,
    BattleRoyale,
    League
}

/// <summary>
/// Tournament lifecycle status.
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
/// Individual match status.
/// </summary>
public enum MatchStatus
{
    Scheduled,
    InProgress,
    Completed,
    Disputed,
    Forfeited,
    Cancelled
}

/// <summary>
/// Bracket section type.
/// </summary>
public enum BracketType
{
    Winners,
    Losers,
    GrandFinals
}

/// <summary>
/// Represents a gaming tournament.
/// </summary>
public record Tournament
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public GameInfo Game { get; set; } = null!;
    public TournamentFormat Format { get; set; }
    public TournamentStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime RegistrationDeadline { get; set; }
    public int MaxParticipants { get; set; }
    public int MinParticipants { get; set; }
    public List<Participant> Participants { get; set; } = new();
    public Bracket? Bracket { get; set; }
    public PrizePool? PrizePool { get; set; }
    public TournamentRules Rules { get; set; } = new();
    public List<Match> Matches { get; set; } = new();
    public string? StreamUrl { get; set; }
    public bool RequireCheckIn { get; set; }
    public TimeSpan CheckInWindow { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Represents a tournament participant.
/// </summary>
public record Participant
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public int? Seed { get; set; }
    public ParticipantStatus Status { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public string? CheckInCode { get; set; }
    public List<MatchResult> MatchHistory { get; set; } = new();
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Ties { get; set; }
}

/// <summary>
/// Participant status in the tournament.
/// </summary>
public enum ParticipantStatus
{
    Registered,
    CheckedIn,
    Competing,
    Eliminated,
    Disqualified,
    Withdrawn
}

/// <summary>
/// Represents a tournament match.
/// </summary>
public record Match
{
    public Guid Id { get; set; }
    public int Round { get; set; }
    public int? MatchNumber { get; set; }
    public MatchStatus Status { get; set; }
    public Participant? Player1 { get; set; }
    public Participant? Player2 { get; set; }
    public Participant? Winner { get; set; }
    public MatchResult? Result { get; set; }
    public DateTime? ScheduledTime { get; set; }
    public DateTime? StartedTime { get; set; }
    public DateTime? CompletedTime { get; set; }
    public string? StreamUrl { get; set; }
    public bool IsWinnersBracket { get; set; }
    public Guid? NextMatchWin { get; set; }
    public Guid? NextMatchLose { get; set; }
    public List<MatchGame> Games { get; set; } = new();
    public string? Notes { get; set; }
}

/// <summary>
/// Represents a single game within a match.
/// </summary>
public record MatchGame
{
    public int GameNumber { get; set; }
    public int? Player1Score { get; set; }
    public int? Player2Score { get; set; }
    public Participant? Winner { get; set; }
    public string? ReplayUrl { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? Stage { get; set; }
    public string? Characters { get; set; }
}

/// <summary>
/// Represents the result of a match.
/// </summary>
public record MatchResult
{
    public Participant Winner { get; set; } = null!;
    public int Player1Score { get; set; }
    public int Player2Score { get; set; }
    public MatchResultType Type { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Type of match result.
/// </summary>
public enum MatchResultType
{
    Normal,
    Forfeit,
    Disqualification,
    Draw
}

/// <summary>
/// Represents a tournament bracket.
/// </summary>
public record Bracket
{
    public Guid Id { get; set; }
    public List<BracketRound> Rounds { get; set; } = new();
    public List<Match> Matches { get; set; } = new();
    public int TotalRounds { get; set; }
    public Participant? Champion { get; set; }
}

/// <summary>
/// Represents a round within a bracket.
/// </summary>
public record BracketRound
{
    public int RoundNumber { get; set; }
    public string Name { get; set; } = string.Empty; // "Round 1", "Quarterfinals", etc.
    public BracketType Type { get; set; }
    public List<Match> Matches { get; set; } = new();
    public bool IsComplete { get; set; }
}

/// <summary>
/// Represents a tournament prize pool.
/// </summary>
public record PrizePool
{
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public List<PrizeDistribution> Distribution { get; set; } = new();
}

/// <summary>
/// Represents prize distribution for a specific placing.
/// </summary>
public record PrizeDistribution
{
    public int Place { get; set; } // 1st, 2nd, 3rd, etc.
    public decimal Amount { get; set; }
    public double Percentage { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Tournament rules configuration.
/// </summary>
public record TournamentRules
{
    public int BestOf { get; set; } = 3; // Best of 3, 5, etc.
    public TimeSpan TimeLimit { get; set; }
    public bool AllowCharacterSwitch { get; set; }
    public bool RandomStageSelect { get; set; }
    public List<string> BannedStages { get; set; } = new();
    public List<string> LegalStages { get; set; } = new();
    public string? CustomRules { get; set; }
}

/// <summary>
/// Game information for a tournament.
/// </summary>
public record GameInfo
{
    public Guid GameId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CoverImage { get; set; }
    public string Platform { get; set; } = string.Empty;
}

// Request/Response records for service operations

/// <summary>
/// Request to create a new tournament.
/// </summary>
public record CreateTournamentRequest(
    string Name,
    string Description,
    GameInfo Game,
    TournamentFormat Format,
    DateTime StartDate,
    DateTime RegistrationDeadline,
    int MaxParticipants,
    TournamentRules? Rules = null,
    PrizePool? PrizePool = null
);

/// <summary>
/// Request to update an existing tournament.
/// </summary>
public record UpdateTournamentRequest(
    string? Name = null,
    string? Description = null,
    DateTime? StartDate = null,
    DateTime? RegistrationDeadline = null,
    int? MaxParticipants = null,
    TournamentRules? Rules = null,
    string? StreamUrl = null
);

/// <summary>
/// Request to register a participant.
/// </summary>
public record RegisterParticipantRequest(
    string UserId,
    string DisplayName,
    int? Seed = null
);

/// <summary>
/// Options for bracket generation.
/// </summary>
public record BracketOptions(
    bool RandomizeSeeds = false,
    IReadOnlyList<string>? SeededPlayers = null
);

/// <summary>
/// Request to schedule a match.
/// </summary>
public record ScheduleMatchRequest(
    DateTime ScheduledTime,
    string? StreamUrl = null
);

/// <summary>
/// Request to report a match result.
/// </summary>
public record ReportMatchResultRequest(
    int Player1Score,
    int Player2Score,
    string? Notes = null,
    List<MatchGame>? Games = null
);

/// <summary>
/// Filter options for tournament queries.
/// </summary>
public record TournamentFilter(
    TournamentStatus? Status = null,
    TournamentFormat? Format = null,
    Guid? GameId = null,
    DateTime? StartDateFrom = null,
    DateTime? StartDateTo = null,
    string? CreatedBy = null,
    bool IncludeCompleted = false
);

/// <summary>
/// Tournament statistics.
/// </summary>
public record TournamentStatistics
{
    public int TotalMatches { get; set; }
    public int CompletedMatches { get; set; }
    public int RegisteredParticipants { get; set; }
    public int CheckedInParticipants { get; set; }
    public TimeSpan AverageMatchDuration { get; set; }
    public Dictionary<string, int> ResultsByType { get; set; } = new();
}
