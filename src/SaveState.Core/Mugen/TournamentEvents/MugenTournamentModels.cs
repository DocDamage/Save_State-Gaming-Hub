using SaveState.Core.Common.Base;

namespace SaveState.Core.Mugen.TournamentEvents;

/// <summary>
/// Represents a tournament bracket.
/// </summary>
public class TournamentEvent : EntityBase
{
    /// <summary>
    /// Tournament name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Tournament description.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Tournament format.
    /// </summary>
    public TournamentFormat Format { get; set; } = TournamentFormat.SingleElimination;
    
    /// <summary>
    /// Current status of the tournament.
    /// </summary>
    public TournamentStatus Status { get; set; } = TournamentStatus.Pending;
    
    /// <summary>
    /// Maximum number of participants.
    /// </summary>
    public int MaxParticipants { get; set; }
    
    /// <summary>
    /// Current number of registered participants.
    /// </summary>
    public int RegisteredParticipants => Participants.Count;
    
    /// <summary>
    /// Tournament participants.
    /// </summary>
    public List<TournamentParticipant> Participants { get; set; } = new();
    
    /// <summary>
    /// All matches in the tournament.
    /// </summary>
    public List<TournamentMatch> Matches { get; set; } = new();
    
    /// <summary>
    /// Tournament rounds.
    /// </summary>
    public List<TournamentRound> Rounds { get; set; } = new();
    
    /// <summary>
    /// When the tournament was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// When the tournament starts.
    /// </summary>
    public DateTime? ScheduledStart { get; set; }
    
    /// <summary>
    /// When the tournament actually started.
    /// </summary>
    public DateTime? StartedAt { get; set; }
    
    /// <summary>
    /// When the tournament ended.
    /// </summary>
    public DateTime? EndedAt { get; set; }
    
    /// <summary>
    /// Tournament organizer/creator.
    /// </summary>
    public string Organizer { get; set; } = string.Empty;
    
    /// <summary>
    /// Tournament rules.
    /// </summary>
    public TournamentRules Rules { get; set; } = new();
    
    /// <summary>
    /// Tournament settings.
    /// </summary>
    public TournamentSettings Settings { get; set; } = new();
    
    /// <summary>
    /// Tournament winner.
    /// </summary>
    public TournamentParticipant? Winner { get; set; }
    
    /// <summary>
    /// Runner-up.
    /// </summary>
    public TournamentParticipant? RunnerUp { get; set; }
    
    /// <summary>
    /// Third place winner (if applicable).
    /// </summary>
    public TournamentParticipant? ThirdPlace { get; set; }
    
    /// <summary>
    /// Whether the tournament is public.
    /// </summary>
    public bool IsPublic { get; set; } = true;
    
    /// <summary>
    /// Whether registration is open.
    /// </summary>
    public bool RegistrationOpen { get; set; } = true;
    
    /// <summary>
    /// Registration deadline.
    /// </summary>
    public DateTime? RegistrationDeadline { get; set; }
    
    /// <summary>
    /// Tournament tags/categories.
    /// </summary>
    public List<string> Tags { get; set; } = new();
    
    /// <summary>
    /// Stream URL if being broadcast.
    /// </summary>
    public string? StreamUrl { get; set; }
    
    /// <summary>
    /// Discord/webhook for notifications.
    /// </summary>
    public string? DiscordWebhook { get; set; }
    
    /// <summary>
    /// Current round number.
    /// </summary>
    public int CurrentRound { get; set; } = 0;
    
    /// <summary>
    /// Total number of rounds.
    /// </summary>
    public int TotalRounds => Rounds.Count;
    
    /// <summary>
    /// Prize pool information.
    /// </summary>
    public PrizePool? PrizePool { get; set; }
    
    /// <summary>
    /// Tournament statistics.
    /// </summary>
    public TournamentStatistics Statistics { get; set; } = new();
}

/// <summary>
/// Tournament format types.
/// </summary>
public enum TournamentFormat
{
    SingleElimination,
    DoubleElimination,
    RoundRobin,
    Swiss,
    SingleRoundRobin,
    DoubleRoundRobin
}

/// <summary>
/// Tournament status.
/// </summary>
public enum TournamentStatus
{
    Pending,
    RegistrationOpen,
    RegistrationClosed,
    InProgress,
    Paused,
    Completed,
    Cancelled
}

/// <summary>
/// Tournament participant.
/// </summary>
public class TournamentParticipant : EntityBase
{
    /// <summary>
    /// Tournament ID.
    /// </summary>
    public Guid TournamentId { get; set; }
    
    /// <summary>
    /// Participant name/gamertag.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// User ID if registered user.
    /// </summary>
    public string? UserId { get; set; }
    
    /// <summary>
    /// Seed/rank in tournament.
    /// </summary>
    public int Seed { get; set; }
    
    /// <summary>
    /// Initial seed before any matches.
    /// </summary>
    public int InitialSeed { get; set; }
    
    /// <summary>
    /// Registration timestamp.
    /// </summary>
    public DateTime RegisteredAt { get; set; }
    
    /// <summary>
    /// Whether participant is checked in.
    /// </summary>
    public bool IsCheckedIn { get; set; }
    
    /// <summary>
    /// Check-in timestamp.
    /// </summary>
    public DateTime? CheckedInAt { get; set; }
    
    /// <summary>
    /// Whether participant is eliminated.
    /// </summary>
    public bool IsEliminated { get; set; }
    
    /// <summary>
    /// Elimination round (if double elimination: -1 for losers bracket).
    /// </summary>
    public int? EliminatedInRound { get; set; }
    
    /// <summary>
    /// Placement in tournament.
    /// </summary>
    public int? Placement { get; set; }
    
    /// <summary>
    /// Participant statistics.
    /// </summary>
    public ParticipantStatistics Statistics { get; set; } = new();
    
    /// <summary>
    /// Discord/tag for contact.
    /// </summary>
    public string? ContactInfo { get; set; }
    
    /// <summary>
    /// Stream URL if participant is streaming.
    /// </summary>
    public string? StreamUrl { get; set; }
    
    /// <summary>
    /// Country/region.
    /// </summary>
    public string? Country { get; set; }
    
    /// <summary>
    /// Team/clan affiliation.
    /// </summary>
    public string? Team { get; set; }
    
    /// <summary>
    /// Character being played (if locked).
    /// </summary>
    public string? Character { get; set; }
    
    /// <summary>
    /// Whether character is locked in.
    /// </summary>
    public bool CharacterLocked { get; set; }
    
    /// <summary>
    /// Additional notes.
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Tournament match.
/// </summary>
public class TournamentMatch : EntityBase
{
    /// <summary>
    /// Tournament ID.
    /// </summary>
    public Guid TournamentId { get; set; }
    
    /// <summary>
    /// Round number.
    /// </summary>
    public int Round { get; set; }
    
    /// <summary>
    /// Match number within round.
    /// </summary>
    public int MatchNumber { get; set; }
    
    /// <summary>
    /// Bracket position (Winners/Losers/Grand Finals).
    /// </summary>
    public BracketPosition BracketPosition { get; set; } = BracketPosition.Winners;
    
    /// <summary>
    /// First participant.
    /// </summary>
    public TournamentParticipant? Participant1 { get; set; }
    
    /// <summary>
    /// Second participant.
    /// </summary>
    public TournamentParticipant? Participant2 { get; set; }
    
    /// <summary>
    /// Winner of the match.
    /// </summary>
    public TournamentParticipant? Winner { get; set; }
    
    /// <summary>
    /// Loser of the match.
    /// </summary>
    public TournamentParticipant? Loser { get; set; }
    
    /// <summary>
    /// Current status.
    /// </summary>
    public MatchStatus Status { get; set; } = MatchStatus.Pending;
    
    /// <summary>
    /// Match result.
    /// </summary>
    public MatchResult? Result { get; set; }
    
    /// <summary>
    /// Scheduled start time.
    /// </summary>
    public DateTime? ScheduledTime { get; set; }
    
    /// <summary>
    /// When match started.
    /// </summary>
    public DateTime? StartedAt { get; set; }
    
    /// <summary>
    /// When match ended.
    /// </summary>
    public DateTime? EndedAt { get; set; }
    
    /// <summary>
    /// Stream/station assignment.
    /// </summary>
    public string? Station { get; set; }
    
    /// <summary>
    /// Whether match is being streamed.
    /// </summary>
    public bool IsStreamed { get; set; }
    
    /// <summary>
    /// Match notes.
    /// </summary>
    public string? Notes { get; set; }
    
    /// <summary>
    /// ID of next match for winner.
    /// </summary>
    public Guid? NextMatchForWinnerId { get; set; }
    
    /// <summary>
    /// ID of next match for loser (double elimination).
    /// </summary>
    public Guid? NextMatchForLoserId { get; set; }
    
    /// <summary>
    /// IDs of previous matches that feed into this one.
    /// </summary>
    public List<Guid> PreviousMatchIds { get; set; } = new();
    
    /// <summary>
    /// Match identifier (e.g., "W-R2-M3" for Winners Round 2 Match 3).
    /// </summary>
    public string MatchIdentifier { get; set; } = string.Empty;
}

/// <summary>
/// Bracket position.
/// </summary>
public enum BracketPosition
{
    Winners,
    Losers,
    GrandFinals,
    BracketReset
}

/// <summary>
/// Match status.
/// </summary>
public enum MatchStatus
{
    Pending,
    Ready,
    InProgress,
    Completed,
    Bye,
    Walkover,
    Disqualified
}

/// <summary>
/// Match result.
/// </summary>
public class MatchResult
{
    /// <summary>
    /// Participant 1 score.
    /// </summary>
    public int Score1 { get; set; }
    
    /// <summary>
    /// Participant 2 score.
    /// </summary>
    public int Score2 { get; set; }
    
    /// <summary>
    /// Round-by-round results.
    /// </summary>
    public List<RoundResult> RoundResults { get; set; } = new();
    
    /// <summary>
    /// Winner of the match.
    /// </summary>
    public Guid WinnerId { get; set; }
    
    /// <summary>
    /// How the match ended.
    /// </summary>
    public MatchEndCondition EndCondition { get; set; } = MatchEndCondition.Normal;
    
    /// <summary>
    /// Replay file path if recorded.
    /// </summary>
    public string? ReplayPath { get; set; }
    
    /// <summary>
    /// Match duration.
    /// </summary>
    public TimeSpan? Duration { get; set; }
}

/// <summary>
/// Individual round result.
/// </summary>
public class RoundResult
{
    public int RoundNumber { get; set; }
    public int Score1 { get; set; }
    public int Score2 { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// How a match ended.
/// </summary>
public enum MatchEndCondition
{
    Normal,
    Forfeit,
    Disqualification,
    Timeout,
    NoShow,
    DoubleKO
}

/// <summary>
/// Tournament round.
/// </summary>
public class TournamentRound
{
    public int RoundNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public BracketPosition BracketPosition { get; set; }
    public int MatchesCount { get; set; }
    public bool IsComplete { get; set; }
    public DateTime? ScheduledStart { get; set; }
    public List<Guid> MatchIds { get; set; } = new();
}

/// <summary>
/// Tournament rules.
/// </summary>
public class TournamentRules
{
    /// <summary>
    /// Best of X games (e.g., 3 = Best of 3).
    /// </summary>
    public int BestOf { get; set; } = 3;
    
    /// <summary>
    /// Number of rounds per game.
    /// </summary>
    public int RoundsPerGame { get; set; } = 2;
    
    /// <summary>
    /// Time limit per match in minutes.
    /// </summary>
    public int? TimeLimitMinutes { get; set; }
    
    /// <summary>
    /// Stage selection rules.
    /// </summary>
    public string StageSelection { get; set; } = "Random";
    
    /// <summary>
    /// Character selection rules.
    /// </summary>
    public string CharacterSelection { get; set; } = "Free";
    
    /// <summary>
    /// Whether character lock is enabled.
    /// </summary>
    public bool CharacterLock { get; set; }
    
    /// <summary>
    /// Allow counterpicking.
    /// </summary>
    public bool AllowCounterpick { get; set; } = true;
    
    /// <summary>
    /// Custom rules text.
    /// </summary>
    public string? CustomRules { get; set; }
    
    /// <summary>
    /// Code of conduct.
    /// </summary>
    public string? CodeOfConduct { get; set; }
}

/// <summary>
/// Tournament settings.
/// </summary>
public class TournamentSettings
{
    /// <summary>
    /// Auto-progress winners.
    /// </summary>
    public bool AutoProgress { get; set; } = true;
    
    /// <summary>
    /// Require check-in.
    /// </summary>
    public bool RequireCheckIn { get; set; } = false;
    
    /// <summary>
    /// Check-in window in minutes.
    /// </summary>
    public int CheckInWindowMinutes { get; set; } = 30;
    
    /// <summary>
    /// Allow self-reporting.
    /// </summary>
    public bool AllowSelfReporting { get; set; } = true;
    
    /// <summary>
    /// Require proof for results.
    /// </summary>
    public bool RequireProof { get; set; } = false;
    
    /// <summary>
    /// Enable third place match.
    /// </summary>
    public bool ThirdPlaceMatch { get; set; } = true;
    
    /// <summary>
    /// Enable Discord notifications.
    /// </summary>
    public bool DiscordNotifications { get; set; } = false;
    
    /// <summary>
    /// Enable OBS overlay.
    /// </summary>
    public bool EnableObsOverlay { get; set; } = false;
    
    /// <summary>
    /// Seeding method.
    /// </summary>
    public SeedingMethod SeedingMethod { get; set; } = SeedingMethod.Random;
}

/// <summary>
/// Seeding methods.
/// </summary>
public enum SeedingMethod
{
    Random,
    SkillBased,
    RegistrationOrder,
    Manual,
    SwissStandings
}

/// <summary>
/// Prize pool.
/// </summary>
public class PrizePool
{
    public decimal Total { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal FirstPlace { get; set; }
    public decimal SecondPlace { get; set; }
    public decimal ThirdPlace { get; set; }
    public List<PrizeDistribution> AdditionalPrizes { get; set; } = new();
}

/// <summary>
/// Prize distribution.
/// </summary>
public class PrizeDistribution
{
    public string Position { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Participant statistics.
/// </summary>
public class ParticipantStatistics
{
    public int MatchesPlayed { get; set; }
    public int MatchesWon { get; set; }
    public int MatchesLost { get; set; }
    public int RoundsWon { get; set; }
    public int RoundsLost { get; set; }
    public decimal WinRate => MatchesPlayed > 0 ? (decimal)MatchesWon / MatchesPlayed * 100 : 0;
    public int UpsetsCaused { get; set; }
    public int UpsetsSuffered { get; set; }
}

/// <summary>
/// Tournament statistics.
/// </summary>
public class TournamentStatistics
{
    public int TotalMatches { get; set; }
    public int CompletedMatches { get; set; }
    public int MatchesInProgress { get; set; }
    public int MatchesPending { get; set; }
    public int Upsets { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan AverageMatchDuration { get; set; }
    public Dictionary<string, int> CharactersUsed { get; set; } = new();
    public Dictionary<string, int> StagesUsed { get; set; } = new();
}

/// <summary>
/// Stream overlay data.
/// </summary>
public class StreamOverlayData
{
    public Guid TournamentId { get; set; }
    public string TournamentName { get; set; } = string.Empty;
    public string CurrentRound { get; set; } = string.Empty;
    public TournamentMatch? CurrentMatch { get; set; }
    public List<TournamentMatch> UpcomingMatches { get; set; } = new();
    public List<TournamentParticipant> Top8 { get; set; } = new();
}

/// <summary>
/// Request to create a tournament.
/// </summary>
public class CreateTournamentRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TournamentFormat Format { get; set; } = TournamentFormat.SingleElimination;
    public int MaxParticipants { get; set; }
    public DateTime? ScheduledStart { get; set; }
    public string Organizer { get; set; } = string.Empty;
    public TournamentRules Rules { get; set; } = new();
    public TournamentSettings Settings { get; set; } = new();
    public bool IsPublic { get; set; } = true;
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Request to register a participant.
/// </summary>
public class RegisterParticipantRequest
{
    public string Name { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? ContactInfo { get; set; }
    public string? Country { get; set; }
    public string? Team { get; set; }
    public string? Character { get; set; }
    public string? StreamUrl { get; set; }
}

/// <summary>
/// Request to report match results.
/// </summary>
public class ReportMatchResultRequest
{
    public int Score1 { get; set; }
    public int Score2 { get; set; }
    public Guid WinnerId { get; set; }
    public List<RoundResult>? RoundResults { get; set; }
    public MatchEndCondition EndCondition { get; set; } = MatchEndCondition.Normal;
    public string? ReplayPath { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Filter for tournament searches.
/// </summary>
public class TournamentFilter
{
    public TournamentFormat? Format { get; set; }
    public TournamentStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Organizer { get; set; }
    public List<string>? Tags { get; set; }
    public bool? IsPublic { get; set; }
    public string? SearchTerm { get; set; }
}

