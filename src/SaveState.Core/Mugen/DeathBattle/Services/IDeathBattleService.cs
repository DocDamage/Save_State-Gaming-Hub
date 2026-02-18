using SaveState.Core.Common;

namespace SaveState.Core.Mugen.DeathBattle.Services;

/// <summary>
/// Service for managing Death Battle simulations.
/// YouTube Death Battle style with research, analysis, and dramatic outcomes.
/// </summary>
public interface IDeathBattleService
{
    /// <summary>
    /// Creates a new Death Battle.
    /// </summary>
    Task<Result<DeathBattleMatch>> CreateBattleAsync(
        CreateDeathBattleRequest request,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets a battle by its code.
    /// </summary>
    Task<Result<DeathBattleMatch>> GetBattleAsync(
        string battleCode,
        CancellationToken ct = default);
    
    /// <summary>
    /// Starts the battle simulation process.
    /// </summary>
    Task<Result> StartBattleAsync(
        string battleCode,
        CancellationToken ct = default);
    
    /// <summary>
    /// Advances to the next phase of the battle.
    /// </summary>
    Task<Result<DeathBattlePhase>> NextPhaseAsync(
        string battleCode,
        CancellationToken ct = default);
    
    /// <summary>
    /// Runs battle simulations to determine likely outcomes.
    /// </summary>
    Task<Result<DeathBattleSimulation>> RunSimulationsAsync(
        string battleCode,
        int simulationCount = 1000,
        CancellationToken ct = default);
    
    /// <summary>
    /// Concludes the battle with a winner.
    /// </summary>
    Task<Result<DeathBattleMatch>> ConcludeBattleAsync(
        string battleCode,
        Guid winnerId,
        DeathBattleOutcome outcome,
        string reasoning,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets all battles with filtering.
    /// </summary>
    Task<Result<List<DeathBattleMatch>>> GetBattlesAsync(
        DeathBattleFilter? filter = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets battles featuring a specific character.
    /// </summary>
    Task<Result<List<DeathBattleMatch>>> GetCharacterBattlesAsync(
        Guid characterId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Submits a vote for who will win.
    /// </summary>
    Task<Result> VoteAsync(
        string battleCode,
        Guid combatantId,
        Guid userId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets current vote counts.
    /// </summary>
    Task<Result<VoteTally>> GetVoteTallyAsync(
        string battleCode,
        CancellationToken ct = default);
    
    /// <summary>
    /// Suggests a new Death Battle matchup.
    /// </summary>
    Task<Result<DeathBattleSuggestion>> SuggestBattleAsync(
        Guid combatant1Id,
        Guid combatant2Id,
        string reasoning,
        Guid userId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets all battle suggestions.
    /// </summary>
    Task<Result<List<DeathBattleSuggestion>>> GetSuggestionsAsync(
        bool includeAccepted = false,
        CancellationToken ct = default);
    
    /// <summary>
    /// Upvotes a battle suggestion.
    /// </summary>
    Task<Result> UpvoteSuggestionAsync(
        Guid suggestionId,
        Guid userId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets the Death Battle leaderboard.
    /// </summary>
    Task<Result<List<DeathBattleLeaderboardEntry>>> GetLeaderboardAsync(
        int top = 100,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets character's Death Battle statistics.
    /// </summary>
    Task<Result<CharacterDeathBattleStats>> GetCharacterStatsAsync(
        Guid characterId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Generates a battle preview/trailer.
    /// </summary>
    Task<Result<string>> GeneratePreviewAsync(
        string battleCode,
        CancellationToken ct = default);
    
    /// <summary>
    /// Exports battle results.
    /// </summary>
    Task<Result<byte[]>> ExportBattleAsync(
        string battleCode,
        ExportFormat format = ExportFormat.Json,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets popular/upcoming battles.
    /// </summary>
    Task<Result<List<DeathBattleMatch>>> GetFeaturedBattlesAsync(
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets random battle suggestion.
    /// </summary>
    Task<Result<(Guid Character1Id, Guid Character2Id)>> GetRandomMatchupAsync(
        CancellationToken ct = default);
}

/// <summary>
/// Filter for retrieving battles.
/// </summary>
public class DeathBattleFilter
{
    public DeathBattleState? State { get; set; }
    public Guid? CharacterId { get; set; }
    public bool? IsPublic { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public List<string>? Tags { get; set; }
    public DeathBattleSortOrder SortBy { get; set; } = DeathBattleSortOrder.Newest;
}

/// <summary>
/// Sort order for battles.
/// </summary>
public enum DeathBattleSortOrder
{
    Newest,
    Oldest,
    MostViewed,
    MostVoted,
    RecentlyUpdated
}

/// <summary>
/// Vote tally results.
/// </summary>
public class VoteTally
{
    public string BattleCode { get; set; } = string.Empty;
    public Guid Combatant1Id { get; set; }
    public string Combatant1Name { get; set; } = string.Empty;
    public int Combatant1Votes { get; set; }
    public Guid Combatant2Id { get; set; }
    public string Combatant2Name { get; set; } = string.Empty;
    public int Combatant2Votes { get; set; }
    public int TotalVotes => Combatant1Votes + Combatant2Votes;
    public string Leading => Combatant1Votes > Combatant2Votes ? Combatant1Name :
                            Combatant2Votes > Combatant1Votes ? Combatant2Name : "Tie";
}

/// <summary>
/// Character Death Battle statistics.
/// </summary>
public class CharacterDeathBattleStats
{
    public Guid CharacterId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public int TotalBattles { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Draws { get; set; }
    public decimal WinRate => TotalBattles > 0 ? (decimal)Wins / TotalBattles * 100 : 0;
    public int KOs { get; set; }
    public int Deaths { get; set; }
    public List<string> NotableVictories { get; set; } = new();
    public List<string> NotableDefeats { get; set; } = new();
    public int CurrentStreak { get; set; }
    public int LongestWinStreak { get; set; }
    public string Tier { get; set; } = string.Empty;
    public int Rank { get; set; }
}

/// <summary>
/// Export format.
/// </summary>
public enum ExportFormat
{
    Json,
    Pdf,
    Markdown,
    Html
}
