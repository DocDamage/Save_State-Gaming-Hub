using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ComboDatabase;
using SaveState.Core.Mugen.ComboDatabase.Services;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Mugen.ComboDatabaseServices;

/// <summary>
/// Thin facade for combo database operations.
/// </summary>
public class ComboDatabaseService : IComboDatabaseService
{
    private readonly SaveStateDbContext _dbContext;
    private readonly ILogger<ComboDatabaseService> _logger;

    public ComboDatabaseService(
        SaveStateDbContext dbContext,
        ILogger<ComboDatabaseService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public Task<Result<ComboEntry>> AddComboAsync(AddComboRequest request,  CancellationToken ct = default) =>
        ComboDatabaseServiceOperations.AddComboAsync(_dbContext, _logger, request, ct);

    public Task<Result<ComboEntry>> UpdateComboAsync(Guid comboId,  UpdateComboRequest request, CancellationToken ct = default) =>
        ComboDatabaseServiceOperations.UpdateComboAsync(_dbContext, _logger, comboId, request, ct);

    public Task<Result<ComboEntry>> GetComboAsync(Guid comboId,  CancellationToken ct = default) =>
        ComboDatabaseServiceOperations.GetComboAsync(_dbContext, _logger, comboId, ct);

    public Task<Result> DeleteComboAsync(Guid comboId,  CancellationToken ct = default) =>
        ComboDatabaseServiceOperations.DeleteComboAsync(_dbContext, _logger, comboId, ct);

    public Task<Result<List<ComboEntry>>> SearchCombosAsync(ComboFilter filter,  int page = 1,  int pageSize = 20, CancellationToken ct = default) =>
        ComboDatabaseServiceOperations.SearchCombosAsync(_dbContext, _logger, filter, page, pageSize, ct);

    public Task<Result<CharacterComboDatabase>> GetCharacterCombosAsync(string characterName,  CancellationToken ct = default) =>
        ComboDatabaseServiceOperations.GetCharacterCombosAsync(_dbContext, _logger, characterName, ct);

    public Task<Result<List<ComboEntry>>> GetCombosByDifficultyAsync(string characterName,  ComboDifficulty difficulty, CancellationToken ct = default) =>
        ComboDatabaseServiceOperations.GetCombosByDifficultyAsync(_dbContext, _logger, characterName, difficulty, ct);

    public Task<Result<List<ComboEntry>>> GetOptimalCombosAsync(string characterName,  string? startingPosition = null, CancellationToken ct = default) =>
        ComboDatabaseServiceOperations.GetOptimalCombosAsync(_dbContext, _logger, characterName, startingPosition, ct);

    public Task<Result<List<ComboEntry>>> GetTouchOfDeathCombosAsync(string? characterName = null, CancellationToken ct = default) =>
        ComboDatabaseServiceOperations.GetTouchOfDeathCombosAsync(_dbContext, _logger, characterName, ct);

    public Task<Result<List<ComboEntry>>> GetCombosByTagAsync(string tag,  string? characterName = null, CancellationToken ct = default) =>
        ComboDatabaseServiceOperations.GetCombosByTagAsync(_dbContext, _logger, tag, characterName, ct);

    public Task<Result<ComboMatchupInfo>> GetMatchupCombosAsync(string characterName,  string opponentName, CancellationToken ct = default) =>
        ComboDatabaseServiceOperations.GetMatchupCombosAsync(_dbContext, _logger, characterName, opponentName, ct);

    public Task<Result> RecordComboUsageAsync(Guid comboId,  bool successful, CancellationToken ct = default) =>
        ComboDatabaseServiceOperations.RecordComboUsageAsync(_dbContext, _logger, comboId, successful, ct);

    public Task<Result> RateComboAsync(Guid comboId,  int rating,  string? userId = null, CancellationToken ct = default) =>
        ComboDatabaseServiceOperations.RateComboAsync(_dbContext, _logger, comboId, rating, userId, ct);

    public Task<Result> UpvoteComboAsync(Guid comboId,  CancellationToken ct = default) =>
        ComboDatabaseServiceOperations.UpvoteComboAsync(_dbContext, _logger, comboId, ct);

    public Task<Result> DownvoteComboAsync(Guid comboId,  CancellationToken ct = default) =>
        ComboDatabaseServiceOperations.DownvoteComboAsync(_dbContext, _logger, comboId, ct);

    public Task<Result<ComboPracticeSession>> StartPracticeSessionAsync(Guid comboId,  CancellationToken ct = default) =>
        ComboDatabaseServiceOperations.StartPracticeSessionAsync(_dbContext, _logger, comboId, ct);

    public Task<Result> RecordPracticeAttemptAsync(Guid sessionId,  PracticeAttempt attempt, CancellationToken ct = default) =>
        ComboDatabaseServiceOperations.RecordPracticeAttemptAsync(_dbContext, _logger, sessionId, attempt, ct);

    public Task<Result<ComboPracticeSession>> CompletePracticeSessionAsync(Guid sessionId,  CancellationToken ct = default) =>
        ComboDatabaseServiceOperations.CompletePracticeSessionAsync(_dbContext, _logger, sessionId, ct);

    public Task<Result<ComboSubmission>> SubmitComboAsync(Guid comboId,  string submitterName, string? submitterId = null, CancellationToken ct = default) =>
        ComboDatabaseServiceOperations.SubmitComboAsync(_dbContext, _logger, comboId, submitterName, submitterId, ct);

    public Task<Result> ReviewSubmissionAsync(Guid submissionId,  SubmissionStatus status,  string? reviewerNotes = null, string? reviewedBy = null, CancellationToken ct = default) =>
        ComboDatabaseServiceOperations.ReviewSubmissionAsync(_dbContext, _logger, submissionId, status, reviewerNotes, reviewedBy, ct);

    public Task<Result<List<ComboSubmission>>> GetPendingSubmissionsAsync(int page = 1,  int pageSize = 20, CancellationToken ct = default) =>
        ComboDatabaseServiceOperations.GetPendingSubmissionsAsync(_dbContext, _logger, page, pageSize, ct);

    public Task<Result<ComboCollection>> CreateCollectionAsync(string name,  string? description,  string? characterName, string creator, bool isPublic = true, CancellationToken ct = default) =>
        ComboDatabaseServiceOperations.CreateCollectionAsync(_dbContext, _logger, name, description, characterName, creator, isPublic, ct);

    public Task<Result> AddToCollectionAsync(Guid collectionId,  Guid comboId, CancellationToken ct = default) =>
        ComboDatabaseServiceOperations.AddToCollectionAsync(_dbContext, _logger, collectionId, comboId, ct);

    public Task<Result<ComboCollection>> GetCollectionAsync(Guid collectionId,  CancellationToken ct = default) =>
        ComboDatabaseServiceOperations.GetCollectionAsync(_dbContext, _logger, collectionId, ct);

    public Task<Result<List<ComboCollection>>> GetCharacterCollectionsAsync(string characterName,  bool includePrivate = false, CancellationToken ct = default) =>
        ComboDatabaseServiceOperations.GetCharacterCollectionsAsync(_dbContext, _logger, characterName, includePrivate, ct);

    public Task<Result<List<ComboEntry>>> DiscoverCombosFromReplayAsync(Guid replayAnalysisId, CancellationToken ct = default) =>
        ComboDatabaseServiceOperations.DiscoverCombosFromReplayAsync(_dbContext, _logger, replayAnalysisId, ct);

    public Task<Result<int>> ImportCombosAsync(string source,  string data, CancellationToken ct = default) =>
        ComboDatabaseServiceOperations.ImportCombosAsync(_dbContext, _logger, source, data, ct);

    public Task<Result<string>> ExportCombosAsync(string characterName,  ExportFormat format = ExportFormat.Json, CancellationToken ct = default) =>
        ComboDatabaseServiceOperations.ExportCombosAsync(_dbContext, _logger, characterName, format, ct);

    public Task<Result<List<DamageOptimizationSuggestion>>> GetOptimizationSuggestionsAsync(Guid comboId, CancellationToken ct = default) =>
        ComboDatabaseServiceOperations.GetOptimizationSuggestionsAsync(_dbContext, _logger, comboId, ct);

    public Task<Result<DamageOptimizationSuggestion>> SuggestImprovementAsync(Guid comboId,  string suggestion,  int potentialDamage, string method, CancellationToken ct = default) =>
        ComboDatabaseServiceOperations.SuggestImprovementAsync(_dbContext, _logger, comboId, suggestion, potentialDamage, method, ct);

    public Task<Result<List<ComboEntry>>> FindSimilarCombosAsync(Guid comboId,  int maxResults = 10, CancellationToken ct = default) =>
        ComboDatabaseServiceOperations.FindSimilarCombosAsync(_dbContext, _logger, comboId, maxResults, ct);

    public Task<Result<ComboRoutesAnalysis>> GetComboRoutesAsync(string characterName, CancellationToken ct = default) =>
        ComboDatabaseServiceOperations.GetComboRoutesAsync(_dbContext, _logger, characterName, ct);

    public Task<Result> LinkRelatedCombosAsync(Guid comboId1,  Guid comboId2, CancellationToken ct = default) =>
        ComboDatabaseServiceOperations.LinkRelatedCombosAsync(_dbContext, _logger, comboId1, comboId2, ct);
}
