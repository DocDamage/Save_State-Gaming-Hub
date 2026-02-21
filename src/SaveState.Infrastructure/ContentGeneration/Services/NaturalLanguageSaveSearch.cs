using SaveState.Core.Common;
using SaveState.Core.ContentGeneration.Services;
using SaveState.Core.SaveStates;
using SaveStateEntity = SaveState.Core.SaveStates.Entities.SaveState;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace SaveState.Infrastructure.ContentGeneration.Services;

/// <summary>
/// Implementation of natural language search for save states.
/// </summary>
public class NaturalLanguageSaveSearch : INaturalLanguageSaveSearch
{
    private readonly ISaveStateRepository _saveStateRepo;
    private readonly ILogger<NaturalLanguageSaveSearch> _logger;

    public NaturalLanguageSaveSearch(
        ISaveStateRepository saveStateRepo,
        ILogger<NaturalLanguageSaveSearch> logger)
    {
        _saveStateRepo = saveStateRepo;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<SaveStateEntity>>> SearchAsync(
        string naturalLanguageQuery,
        CancellationToken ct = default)
    {
        try
        {
            var analysis = await AnalyzeQueryAsync(naturalLanguageQuery, ct);
            if (analysis.IsFailure)
            {
                return Result<IReadOnlyList<SaveStateEntity>>.Failure(analysis.Error!, analysis.ErrorType);
            }

            // Get all save states first
            var allSaveStates = new List<SaveStateEntity>();

            // Search based on extracted information
            // Note: This is a simplified implementation - in production,
            // the repository would support more advanced filtering
            var saveStates = await SearchByEmbeddingsAsync(
                naturalLanguageQuery,
                allSaveStates,
                ct);

            return Result<IReadOnlyList<SaveStateEntity>>.Success(saveStates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Natural language search failed for query: {Query}", naturalLanguageQuery);
            return Result<IReadOnlyList<SaveStateEntity>>.Failure("Search failed", ErrorType.Internal);
        }
    }

    public Task<Result<SearchQueryAnalysis>> AnalyzeQueryAsync(
        string query,
        CancellationToken ct = default)
    {
        try
        {
            // Pattern-based extraction
            var keywords = ExtractKeywords(query);
            var date = ExtractDate(query);
            var game = ExtractGameName(query);
            var location = ExtractLocation(query);
            var intent = DetermineIntent(query);
            var tags = ExtractTags(query);

            var analysis = new SearchQueryAnalysis
            {
                OriginalQuery = query,
                Intent = intent,
                ExtractedKeywords = keywords,
                ReferencedDate = date,
                ReferencedGame = game,
                ReferencedLocation = location,
                Tags = tags
            };

            return Task.FromResult(Result<SearchQueryAnalysis>.Success(analysis));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Query analysis failed");
            return Task.FromResult(Result<SearchQueryAnalysis>.Failure("Analysis failed", ErrorType.Internal));
        }
    }

    private List<string> ExtractKeywords(string query)
    {
        var stopWords = new[] { "the", "a", "an", "in", "on", "at", "to", "for", "of", "my", "where", "find", "before", "after" };
        var words = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Where(w => !stopWords.Contains(w)).ToList();
    }

    private DateTime? ExtractDate(string query)
    {
        var lower = query.ToLowerInvariant();

        // Extract relative dates
        if (lower.Contains("yesterday"))
            return DateTime.Now.AddDays(-1);
        if (lower.Contains("last week"))
            return DateTime.Now.AddDays(-7);
        if (lower.Contains("last month"))
            return DateTime.Now.AddDays(-30);

        // Day of week references
        var daysOfWeek = new[] { "sunday", "monday", "tuesday", "wednesday", "thursday", "friday", "saturday" };
        foreach (var day in daysOfWeek)
        {
            if (lower.Contains(day))
            {
                var targetDay = Enum.Parse<DayOfWeek>(day, true);
                var today = DateTime.Now.DayOfWeek;
                var daysAgo = ((int)today - (int)targetDay + 7) % 7;
                if (daysAgo == 0) daysAgo = 7; // Last occurrence, not today
                return DateTime.Now.AddDays(-daysAgo);
            }
        }

        return null;
    }

    private string? ExtractGameName(string query)
    {
        // Pattern: "in [GameName]" or "from [GameName]"
        var match = Regex.Match(query, @"(?:in|from|of|before the)\s+([A-Z][a-zA-Z\s]+?)(?:\s+(?:boss|level|area)|\s*$)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private string? ExtractLocation(string query)
    {
        // Pattern: "at [Location]" or "in [Location]"
        var patterns = new[] {
            @"at\s+([A-Z][a-zA-Z\s]+)",
            @"in\s+(?:the\s+)?([A-Z][a-zA-Z\s]+?)(?:\s+(?:area|zone|region|dungeon|castle|forest))"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(query, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups[1].Value.Trim();
        }

        return null;
    }

    private string DetermineIntent(string query)
    {
        var lower = query.ToLower();
        if (lower.Contains("find") || lower.Contains("where") || lower.Contains("show"))
            return "find";
        if (lower.Contains("restore") || lower.Contains("load") || lower.Contains("use"))
            return "restore";
        if (lower.Contains("compare") || lower.Contains("difference"))
            return "compare";
        if (lower.Contains("delete") || lower.Contains("remove"))
            return "delete";
        return "search";
    }

    private List<string> ExtractTags(string query)
    {
        var tags = new List<string>();
        var lower = query.ToLower();

        if (lower.Contains("boss") || lower.Contains("final"))
            tags.Add("boss");
        if (lower.Contains("checkpoint") || lower.Contains("save point"))
            tags.Add("checkpoint");
        if (lower.Contains("power") || lower.Contains("upgrade"))
            tags.Add("powerup");
        if (lower.Contains("secret") || lower.Contains("hidden"))
            tags.Add("secret");
        if (lower.Contains("auto") || lower.Contains("autosave"))
            tags.Add("autosave");
        if (lower.Contains("manual"))
            tags.Add("manual");

        return tags;
    }

    private async Task<IReadOnlyList<SaveStateEntity>> SearchByEmbeddingsAsync(
        string query,
        IReadOnlyList<SaveStateEntity> candidates,
        CancellationToken ct)
    {
        // TODO: Generate embedding for query and compare with save state embeddings
        // For now, return candidates sorted by relevance
        await Task.CompletedTask; // Placeholder for async operation
        return candidates.OrderByDescending(s => CalculateRelevance(s, query)).ToList();
    }

    private float CalculateRelevance(SaveStateEntity saveState, string query)
    {
        var score = 0f;
        var lowerQuery = query.ToLower();

        if (saveState.GameLocation != null && lowerQuery.Contains(saveState.GameLocation.ToLower()))
            score += 0.3f;
        if (saveState.Description != null && lowerQuery.Contains(saveState.Description.ToLower()))
            score += 0.2f;

        return score;
    }
}
