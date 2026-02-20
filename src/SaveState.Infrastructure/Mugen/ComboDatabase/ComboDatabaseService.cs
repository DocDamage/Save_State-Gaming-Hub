using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.ComboDatabase;
using SaveState.Core.Mugen.ComboDatabase.Services;
using SaveState.Infrastructure.Mugen.ComboDatabase.Managers;

namespace SaveState.Infrastructure.Mugen.ComboDatabase;

/// <summary>
/// Service for managing the combo database and discovery.
/// </summary>
public class ComboDatabaseService : IComboDatabaseService
{
    private readonly ComboCrudManager _crudManager;
    private readonly ComboSearchManager _searchManager;
    private readonly ComboRatingManager _ratingManager;
    private readonly ComboPracticeManager _practiceManager;
    private readonly ComboSubmissionManager _submissionManager;
    private readonly ComboCollectionManager _collectionManager;
    private readonly ComboImportExportManager _importExportManager;
    private readonly ComboAnalysisManager _analysisManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComboDatabaseService"/> class.
    /// </summary>
    public ComboDatabaseService(
        ComboCrudManager crudManager,
        ComboSearchManager searchManager,
        ComboRatingManager ratingManager,
        ComboPracticeManager practiceManager,
        ComboSubmissionManager submissionManager,
        ComboCollectionManager collectionManager,
        ComboImportExportManager importExportManager,
        ComboAnalysisManager analysisManager)
    {
        _crudManager = crudManager;
        _searchManager = searchManager;
        _ratingManager = ratingManager;
        _practiceManager = practiceManager;
        _submissionManager = submissionManager;
        _collectionManager = collectionManager;
        _importExportManager = importExportManager;
        _analysisManager = analysisManager;
    }

    /// <inheritdoc />
    public Task<Result<ComboEntry>> AddComboAsync(AddComboRequest request, CancellationToken ct = default)
        => _crudManager.AddComboAsync(request, ct);

    /// <inheritdoc />
    public Task<Result<ComboEntry>> UpdateComboAsync(Guid comboId, UpdateComboRequest request, CancellationToken ct = default)
        => _crudManager.UpdateComboAsync(comboId, request, ct);

    /// <inheritdoc />
    public Task<Result<ComboEntry>> GetComboAsync(Guid comboId, CancellationToken ct = default)
        => _crudManager.GetComboAsync(comboId, ct);

    /// <inheritdoc />
    public Task<Result> DeleteComboAsync(Guid comboId, CancellationToken ct = default)
        => _crudManager.DeleteComboAsync(comboId, ct);

    /// <inheritdoc />
    public Task<Result<List<ComboEntry>>> SearchCombosAsync(ComboFilter filter, int page = 1, int pageSize = 20, CancellationToken ct = default)
        => _searchManager.SearchCombosAsync(filter, page, pageSize, ct);

    /// <inheritdoc />
    public Task<Result<CharacterComboDatabase>> GetCharacterCombosAsync(string characterName, CancellationToken ct = default)
        => _searchManager.GetCharacterCombosAsync(characterName, ct);

    /// <inheritdoc />
    public Task<Result<List<ComboEntry>>> GetCombosByDifficultyAsync(string characterName, ComboDifficulty difficulty, CancellationToken ct = default)
        => _searchManager.GetCombosByDifficultyAsync(characterName, difficulty, ct);

    /// <inheritdoc />
    public Task<Result<List<ComboEntry>>> GetOptimalCombosAsync(string characterName, string? startingPosition = null, CancellationToken ct = default)
        => _searchManager.GetOptimalCombosAsync(characterName, startingPosition, ct);

    /// <inheritdoc />
    public Task<Result<List<ComboEntry>>> GetTouchOfDeathCombosAsync(string? characterName = null, CancellationToken ct = default)
        => _searchManager.GetTouchOfDeathCombosAsync(characterName, ct);

    /// <inheritdoc />
    public Task<Result<List<ComboEntry>>> GetCombosByTagAsync(string tag, string? characterName = null, CancellationToken ct = default)
        => _searchManager.GetCombosByTagAsync(tag, characterName, ct);

    /// <inheritdoc />
    public Task<Result<ComboMatchupInfo>> GetMatchupCombosAsync(string characterName, string opponentName, CancellationToken ct = default)
        => _searchManager.GetMatchupCombosAsync(characterName, opponentName, ct);

    /// <inheritdoc />
    public Task<Result> RecordComboUsageAsync(Guid comboId, bool successful, CancellationToken ct = default)
        => _ratingManager.RecordComboUsageAsync(comboId, successful, ct);

    /// <inheritdoc />
    public Task<Result> RateComboAsync(Guid comboId, int rating, string? userId = null, CancellationToken ct = default)
        => _ratingManager.RateComboAsync(comboId, rating, userId, ct);

    /// <inheritdoc />
    public Task<Result> UpvoteComboAsync(Guid comboId, CancellationToken ct = default)
        => _ratingManager.UpvoteComboAsync(comboId, ct);

    /// <inheritdoc />
    public Task<Result> DownvoteComboAsync(Guid comboId, CancellationToken ct = default)
        => _ratingManager.DownvoteComboAsync(comboId, ct);

    /// <inheritdoc />
    public Task<Result<ComboPracticeSession>> StartPracticeSessionAsync(Guid comboId, CancellationToken ct = default)
        => _practiceManager.StartPracticeSessionAsync(comboId, ct);

    /// <inheritdoc />
    public Task<Result> RecordPracticeAttemptAsync(Guid sessionId, PracticeAttempt attempt, CancellationToken ct = default)
        => _practiceManager.RecordPracticeAttemptAsync(sessionId, attempt, ct);

    /// <inheritdoc />
    public Task<Result<ComboPracticeSession>> CompletePracticeSessionAsync(Guid sessionId, CancellationToken ct = default)
        => _practiceManager.CompletePracticeSessionAsync(sessionId, ct);

    /// <inheritdoc />
    public Task<Result<ComboSubmission>> SubmitComboAsync(Guid comboId, string submitterName, string? submitterId = null, CancellationToken ct = default)
        => _submissionManager.SubmitComboAsync(comboId, submitterName, submitterId, ct);

    /// <inheritdoc />
    public Task<Result> ReviewSubmissionAsync(Guid submissionId, SubmissionStatus status, string? reviewerNotes = null, string? reviewedBy = null, CancellationToken ct = default)
        => _submissionManager.ReviewSubmissionAsync(submissionId, status, reviewerNotes, reviewedBy, ct);

    /// <inheritdoc />
    public Task<Result<List<ComboSubmission>>> GetPendingSubmissionsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
        => _submissionManager.GetPendingSubmissionsAsync(page, pageSize, ct);

    /// <inheritdoc />
    public Task<Result<ComboCollection>> CreateCollectionAsync(string name, string? description, string? characterName, string creator, bool isPublic = true, CancellationToken ct = default)
        => _collectionManager.CreateCollectionAsync(name, description, characterName, creator, isPublic, ct);

    /// <inheritdoc />
    public Task<Result> AddToCollectionAsync(Guid collectionId, Guid comboId, CancellationToken ct = default)
        => _collectionManager.AddToCollectionAsync(collectionId, comboId, ct);

    /// <inheritdoc />
    public Task<Result<ComboCollection>> GetCollectionAsync(Guid collectionId, CancellationToken ct = default)
        => _collectionManager.GetCollectionAsync(collectionId, ct);

    /// <inheritdoc />
    public Task<Result<List<ComboCollection>>> GetCharacterCollectionsAsync(string characterName, bool includePrivate = false, CancellationToken ct = default)
        => _collectionManager.GetCharacterCollectionsAsync(characterName, includePrivate, ct);

    /// <inheritdoc />
    public Task<Result<List<ComboEntry>>> DiscoverCombosFromReplayAsync(Guid replayAnalysisId, CancellationToken ct = default)
        => _importExportManager.DiscoverCombosFromReplayAsync(replayAnalysisId, ct);

    /// <inheritdoc />
    public Task<Result<int>> ImportCombosAsync(string source, string data, CancellationToken ct = default)
        => _importExportManager.ImportCombosAsync(source, data, ct);

    /// <inheritdoc />
    public Task<Result<string>> ExportCombosAsync(string characterName, ExportFormat format = ExportFormat.Json, CancellationToken ct = default)
        => _importExportManager.ExportCombosAsync(characterName, format, ct);

    /// <inheritdoc />
    public Task<Result<List<DamageOptimizationSuggestion>>> GetOptimizationSuggestionsAsync(Guid comboId, CancellationToken ct = default)
        => _analysisManager.GetOptimizationSuggestionsAsync(comboId, ct);

    /// <inheritdoc />
    public Task<Result<DamageOptimizationSuggestion>> SuggestImprovementAsync(Guid comboId, string suggestion, int potentialDamage, string method, CancellationToken ct = default)
        => _analysisManager.SuggestImprovementAsync(comboId, suggestion, potentialDamage, method, ct);

    /// <inheritdoc />
    public Task<Result<List<ComboEntry>>> FindSimilarCombosAsync(Guid comboId, int maxResults = 10, CancellationToken ct = default)
        => _analysisManager.FindSimilarCombosAsync(comboId, maxResults, ct);

    /// <inheritdoc />
    public Task<Result<ComboRoutesAnalysis>> GetComboRoutesAsync(string characterName, CancellationToken ct = default)
        => _analysisManager.GetComboRoutesAsync(characterName, ct);

    /// <inheritdoc />
    public Task<Result> LinkRelatedCombosAsync(Guid comboId1, Guid comboId2, CancellationToken ct = default)
        => _analysisManager.LinkRelatedCombosAsync(comboId1, comboId2, ct);
}
