using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Intelligence.AiContent.Services;

namespace SaveState.Infrastructure.Intelligence.AiContent;

/// <summary>
/// Natural language search for save states using semantic understanding.
/// </summary>
public sealed class NaturalLanguageSaveSearch : INaturalLanguageSaveSearch
{
    private readonly ILogger<NaturalLanguageSaveSearch> _logger;
    private readonly Dictionary<Guid, SaveStateContext> _indexedSaves = new();

    public NaturalLanguageSaveSearch(ILogger<NaturalLanguageSaveSearch> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<SemanticSaveResult>>> SearchSavesAsync(
        string query,
        Guid? gameId = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Searching saves with query: '{Query}' for game {GameId}",
            query, gameId);

        // Simple keyword matching for now
        // In production, this would use semantic embeddings
        var results = new List<SemanticSaveResult>();
        var queryLower = query.ToLowerInvariant();
        var queryTerms = queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var (saveId, context) in _indexedSaves)
        {
            if (gameId.HasValue && context.GameId != gameId.Value)
                continue;

            var relevanceScore = CalculateRelevance(queryTerms, context);

            if (relevanceScore > 0.3f)
            {
                results.Add(new SemanticSaveResult(
                    SaveStateId: saveId,
                    GameId: context.GameId,
                    GameTitle: context.GameTitle ?? "Unknown",
                    Description: context.PlayerNotes,
                    RelevanceScore: relevanceScore,
                    CreatedAt: DateTime.UtcNow.AddDays(-new Random(saveId.GetHashCode()).Next(1, 30)),
                    PreviewImageUrl: null));
            }
        }

        // Sort by relevance
        results = results.OrderByDescending(r => r.RelevanceScore).ToList();

        return Task.FromResult(Result.Success<IReadOnlyList<SemanticSaveResult>>(results));
    }

    /// <inheritdoc />
    public Task<Result> IndexSaveStateAsync(
        Guid saveStateId,
        SaveStateContext context,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Indexing save state {SaveStateId} for game {GameTitle}",
            saveStateId, context.GameTitle);

        _indexedSaves[saveStateId] = context;

        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result<string>> GenerateDescriptionAsync(
        Guid saveStateId,
        CancellationToken ct = default)
    {
        if (!_indexedSaves.TryGetValue(saveStateId, out var context))
        {
            return Task.FromResult(Result.Failure<string>(
                "Save state not found", ErrorType.NotFound));
        }

        // Generate natural language description
        var description = GenerateNaturalDescription(context);

        return Task.FromResult(Result.Success(description));
    }

    // Private helper methods

    private float CalculateRelevance(string[] queryTerms, SaveStateContext context)
    {
        var score = 0f;
        var textToSearch = $"{context.PlayerNotes} {context.GameLocation} {context.CharacterLevel} {string.Join(" ", context.Tags ?? new List<string>())}";
        var textLower = textToSearch.ToLowerInvariant();

        foreach (var term in queryTerms)
        {
            // Exact match
            if (textLower.Contains(term))
            {
                score += 0.3f;
            }

            // Keyword matching for common save state terms
            score += term switch
            {
                "boss" when textLower.Contains("boss") => 0.4f,
                "final" when textLower.Contains("final") => 0.4f,
                "beginning" or "start" when textLower.Contains("start") || textLower.Contains("beginning") => 0.4f,
                "level" or "lvl" when context.CharacterLevel != null => 0.3f,
                "achievement" when (context.UnlockedAchievements?.Any() ?? false) => 0.3f,
                _ => 0f
            };
        }

        return Math.Min(score, 1.0f);
    }

    private string GenerateNaturalDescription(SaveStateContext context)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(context.GameLocation))
        {
            parts.Add($"Located at {context.GameLocation}");
        }

        if (!string.IsNullOrEmpty(context.CharacterLevel))
        {
            parts.Add($"Character at {context.CharacterLevel}");
        }

        if (context.PlayTimeAtSave.HasValue)
        {
            parts.Add($"Total play time: {context.PlayTimeAtSave.Value.TotalHours:F1} hours");
        }

        if (context.UnlockedAchievements?.Any() ?? false)
        {
            parts.Add($"Achievements unlocked: {context.UnlockedAchievements.Count}");
        }

        if (!string.IsNullOrEmpty(context.PlayerNotes))
        {
            parts.Add($"Notes: {context.PlayerNotes}");
        }

        return parts.Any()
            ? string.Join(". ", parts)
            : "Save state with no additional context";
    }
}
