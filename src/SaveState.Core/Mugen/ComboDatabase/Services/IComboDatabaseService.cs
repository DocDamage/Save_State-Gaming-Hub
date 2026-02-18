using SaveState.Core.Common;

namespace SaveState.Core.Mugen.ComboDatabase.Services;

/// <summary>
/// Service for managing the combo database and discovery.
/// </summary>
public interface IComboDatabaseService
{
    /// <summary>
    /// Adds a new combo to the database.
    /// </summary>
    Task<Result<ComboEntry>> AddComboAsync(
        AddComboRequest request, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Updates an existing combo.
    /// </summary>
    Task<Result<ComboEntry>> UpdateComboAsync(
        Guid comboId, 
        UpdateComboRequest request,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets a combo by ID.
    /// </summary>
    Task<Result<ComboEntry>> GetComboAsync(
        Guid comboId, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Deletes a combo.
    /// </summary>
    Task<Result> DeleteComboAsync(
        Guid comboId, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Searches combos with filtering and sorting.
    /// </summary>
    Task<Result<List<ComboEntry>>> SearchCombosAsync(
        ComboFilter filter, 
        int page = 1, 
        int pageSize = 20,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets all combos for a character.
    /// </summary>
    Task<Result<CharacterComboDatabase>> GetCharacterCombosAsync(
        string characterName, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets combos by difficulty for a character.
    /// </summary>
    Task<Result<List<ComboEntry>>> GetCombosByDifficultyAsync(
        string characterName, 
        ComboDifficulty difficulty,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets optimal combos for a character.
    /// </summary>
    Task<Result<List<ComboEntry>>> GetOptimalCombosAsync(
        string characterName, 
        string? startingPosition = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets Touch of Death combos.
    /// </summary>
    Task<Result<List<ComboEntry>>> GetTouchOfDeathCombosAsync(
        string? characterName = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets combos by tag.
    /// </summary>
    Task<Result<List<ComboEntry>>> GetCombosByTagAsync(
        string tag, 
        string? characterName = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets matchup-specific combo recommendations.
    /// </summary>
    Task<Result<ComboMatchupInfo>> GetMatchupCombosAsync(
        string characterName, 
        string opponentName,
        CancellationToken ct = default);
    
    /// <summary>
    /// Records combo usage in a match.
    /// </summary>
    Task<Result> RecordComboUsageAsync(
        Guid comboId, 
        bool successful,
        CancellationToken ct = default);
    
    /// <summary>
    /// Rates a combo.
    /// </summary>
    Task<Result> RateComboAsync(
        Guid comboId, 
        int rating, 
        string? userId = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Upvotes a combo.
    /// </summary>
    Task<Result> UpvoteComboAsync(
        Guid comboId, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Downvotes a combo.
    /// </summary>
    Task<Result> DownvoteComboAsync(
        Guid comboId, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Starts a practice session for a combo.
    /// </summary>
    Task<Result<ComboPracticeSession>> StartPracticeSessionAsync(
        Guid comboId, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Records a practice attempt.
    /// </summary>
    Task<Result> RecordPracticeAttemptAsync(
        Guid sessionId, 
        PracticeAttempt attempt,
        CancellationToken ct = default);
    
    /// <summary>
    /// Completes a practice session.
    /// </summary>
    Task<Result<ComboPracticeSession>> CompletePracticeSessionAsync(
        Guid sessionId, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Submits a combo for community approval.
    /// </summary>
    Task<Result<ComboSubmission>> SubmitComboAsync(
        Guid comboId, 
        string submitterName,
        string? submitterId = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Reviews a submitted combo.
    /// </summary>
    Task<Result> ReviewSubmissionAsync(
        Guid submissionId, 
        SubmissionStatus status, 
        string? reviewerNotes = null,
        string? reviewedBy = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets pending submissions.
    /// </summary>
    Task<Result<List<ComboSubmission>>> GetPendingSubmissionsAsync(
        int page = 1, 
        int pageSize = 20,
        CancellationToken ct = default);
    
    /// <summary>
    /// Creates a combo collection.
    /// </summary>
    Task<Result<ComboCollection>> CreateCollectionAsync(
        string name, 
        string? description, 
        string? characterName,
        string creator,
        bool isPublic = true,
        CancellationToken ct = default);
    
    /// <summary>
    /// Adds a combo to a collection.
    /// </summary>
    Task<Result> AddToCollectionAsync(
        Guid collectionId, 
        Guid comboId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets a combo collection.
    /// </summary>
    Task<Result<ComboCollection>> GetCollectionAsync(
        Guid collectionId, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets collections for a character.
    /// </summary>
    Task<Result<List<ComboCollection>>> GetCharacterCollectionsAsync(
        string characterName, 
        bool includePrivate = false,
        CancellationToken ct = default);
    
    /// <summary>
    /// Discovers combos from replay analysis.
    /// </summary>
    Task<Result<List<ComboEntry>>> DiscoverCombosFromReplayAsync(
        Guid replayAnalysisId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Imports combos from external source.
    /// </summary>
    Task<Result<int>> ImportCombosAsync(
        string source, 
        string data,
        CancellationToken ct = default);
    
    /// <summary>
    /// Exports combos to JSON.
    /// </summary>
    Task<Result<string>> ExportCombosAsync(
        string characterName, 
        ExportFormat format = ExportFormat.Json,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets damage optimization suggestions for a combo.
    /// </summary>
    Task<Result<List<DamageOptimizationSuggestion>>> GetOptimizationSuggestionsAsync(
        Guid comboId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Suggests an improvement to a combo.
    /// </summary>
    Task<Result<DamageOptimizationSuggestion>> SuggestImprovementAsync(
        Guid comboId, 
        string suggestion, 
        int potentialDamage,
        string method,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets similar combos based on move sequence.
    /// </summary>
    Task<Result<List<ComboEntry>>> FindSimilarCombosAsync(
        Guid comboId, 
        int maxResults = 10,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets combo routes (common starters and enders).
    /// </summary>
    Task<Result<ComboRoutesAnalysis>> GetComboRoutesAsync(
        string characterName,
        CancellationToken ct = default);
    
    /// <summary>
    /// Marks a combo as related to another.
    /// </summary>
    Task<Result> LinkRelatedCombosAsync(
        Guid comboId1, 
        Guid comboId2,
        CancellationToken ct = default);
}

/// <summary>
/// Export formats for combo data.
/// </summary>
public enum ExportFormat
{
    Json,
    Csv,
    Markdown,
    Xml
}

/// <summary>
/// Combo routes analysis.
/// </summary>
public class ComboRoutesAnalysis
{
    public string CharacterName { get; set; } = string.Empty;
    public List<RouteInfo> CommonStarters { get; set; } = new();
    public List<RouteInfo> CommonEnders { get; set; } = new();
    public List<RouteInfo> CommonExtensions { get; set; } = new();
    public List<RouteInfo> MeterDumpOptions { get; set; } = new();
    public Dictionary<string, List<string>> RouteMap { get; set; } = new();
}

/// <summary>
/// Route information.
/// </summary>
public class RouteInfo
{
    public string Move { get; set; } = string.Empty;
    public int OccurrenceCount { get; set; }
    public decimal SuccessRate { get; set; }
    public int AverageDamage { get; set; }
    public List<string> CommonFollowups { get; set; } = new();
}
