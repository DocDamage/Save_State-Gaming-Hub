using SaveState.Core.Common;
using SaveState.Core.Common.Services;
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
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<NaturalLanguageSaveSearch> _logger;

    public NaturalLanguageSaveSearch(
        ISaveStateRepository saveStateRepo,
        ITimeProvider timeProvider,
        ILogger<NaturalLanguageSaveSearch> logger)
    {
        _saveStateRepo = saveStateRepo;
        _timeProvider = timeProvider;
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

            // Get all save states for the user first
            // In a real implementation, this would be filtered by user context
            var allSaveStates = await GetAllSaveStatesAsync(ct);

            // Search based on extracted information using semantic similarity
            var saveStates = await SearchByEmbeddingsAsync(
                naturalLanguageQuery,
                allSaveStates,
                analysis.Value,
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

    /// <summary>
    /// Gets all save states from the repository.
    /// </summary>
    private async Task<IReadOnlyList<SaveStateEntity>> GetAllSaveStatesAsync(CancellationToken ct)
    {
        try
        {
            // In a real implementation, this would use a method like GetAllAsync or GetByUserIdAsync
            // For now, we return an empty list as a placeholder
            // The actual implementation would depend on the ISaveStateRepository interface
            return new List<SaveStateEntity>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve save states");
            return new List<SaveStateEntity>();
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
        var now = _timeProvider.Now;

        // Extract relative dates
        if (lower.Contains("yesterday"))
            return now.AddDays(-1);
        if (lower.Contains("last week"))
            return now.AddDays(-7);
        if (lower.Contains("last month"))
            return now.AddDays(-30);

        // Day of week references
        var daysOfWeek = new[] { "sunday", "monday", "tuesday", "wednesday", "thursday", "friday", "saturday" };
        foreach (var day in daysOfWeek)
        {
            if (lower.Contains(day))
            {
                var targetDay = Enum.Parse<DayOfWeek>(day, true);
                var today = now.DayOfWeek;
                var daysAgo = ((int)today - (int)targetDay + 7) % 7;
                if (daysAgo == 0) daysAgo = 7; // Last occurrence, not today
                return now.AddDays(-daysAgo);
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
        SearchQueryAnalysis analysis,
        CancellationToken ct)
    {
        // Semantic embedding search implementation
        // This simulates embedding-based search using keyword matching and relevance scoring
        // In a production system, this would use an actual embedding model (e.g., OpenAI, local embeddings)

        var scoredResults = new List<(SaveStateEntity SaveState, float Score)>();

        foreach (var saveState in candidates)
        {
            var score = CalculateSemanticRelevance(saveState, query, analysis);
            if (score > 0)
            {
                scoredResults.Add((saveState, score));
            }
        }

        // Sort by relevance score descending and return top results
        var sortedResults = scoredResults
            .OrderByDescending(r => r.Score)
            .Select(r => r.SaveState)
            .ToList();

        await Task.CompletedTask; // Placeholder for async operation
        return sortedResults;
    }

    /// <summary>
    /// Calculates semantic relevance score between a save state and the search query.
    /// This simulates embedding similarity using keyword matching and metadata analysis.
    /// </summary>
    private float CalculateSemanticRelevance(
        SaveStateEntity saveState,
        string query,
        SearchQueryAnalysis analysis)
    {
        var score = CalculateLocationScore(saveState, query, analysis);
        score += CalculateContentScore(saveState, query, analysis);
        score += CalculateTemporalScore(saveState, analysis);
        score += CalculateMetadataScore(saveState, analysis);

        return Math.Min(score, 1.0f); // Cap at 1.0
    }

    private static float CalculateLocationScore(SaveStateEntity saveState, string query, SearchQueryAnalysis analysis)
    {
        var score = 0f;
        var queryLower = query.ToLowerInvariant();

        // Location match (highest weight)
        if (!string.IsNullOrEmpty(saveState.GameLocation))
        {
            if (!string.IsNullOrEmpty(analysis.ReferencedLocation) &&
                saveState.GameLocation.Contains(analysis.ReferencedLocation, StringComparison.OrdinalIgnoreCase))
            {
                score += 0.35f;
            }
            else if (queryLower.Contains(saveState.GameLocation.ToLowerInvariant()))
            {
                score += 0.30f;
            }
        }

        return score;
    }

    private static float CalculateContentScore(SaveStateEntity saveState, string query, SearchQueryAnalysis analysis)
    {
        var score = 0f;
        var queryLower = query.ToLowerInvariant();
        var keywords = analysis.ExtractedKeywords;

        // Description match
        if (!string.IsNullOrEmpty(saveState.Description))
        {
            if (queryLower.Contains(saveState.Description.ToLowerInvariant()))
            {
                score += 0.20f;
            }

            var descLower = saveState.Description.ToLowerInvariant();
            score += keywords.Count(k => descLower.Contains(k)) * 0.05f;

            // Game name match in description
            if (!string.IsNullOrEmpty(analysis.ReferencedGame) &&
                descLower.Contains(analysis.ReferencedGame.ToLowerInvariant()))
            {
                score += 0.15f;
            }
        }

        // Game name match in location
        if (!string.IsNullOrEmpty(analysis.ReferencedGame) &&
            !string.IsNullOrEmpty(saveState.GameLocation) &&
            saveState.GameLocation.Contains(analysis.ReferencedGame, StringComparison.OrdinalIgnoreCase))
        {
            score += 0.25f;
        }

        // FilePath matching
        if (!string.IsNullOrEmpty(saveState.FilePath))
        {
            var fileNameLower = Path.GetFileName(saveState.FilePath).ToLowerInvariant();
            score += keywords.Count(k => fileNameLower.Contains(k)) * 0.05f;
        }

        // Branch name matching
        if (!string.IsNullOrEmpty(saveState.BranchName))
        {
            var branchLower = saveState.BranchName.ToLowerInvariant();
            score += keywords.Count(k => branchLower.Contains(k)) * 0.08f;
        }

        return score;
    }

    private float CalculateTemporalScore(SaveStateEntity saveState, SearchQueryAnalysis analysis)
    {
        var score = 0f;

        // Date proximity bonus
        if (analysis.ReferencedDate.HasValue)
        {
            var daysDiff = Math.Abs((saveState.CreatedAt - analysis.ReferencedDate.Value).TotalDays);
            score += daysDiff switch
            {
                < 1 => 0.15f,
                < 7 => 0.10f,
                < 30 => 0.05f,
                _ => 0f
            };
        }

        // Recency boost
        var daysSinceCreation = (_timeProvider.UtcNow - saveState.CreatedAt).TotalDays;
        score += daysSinceCreation switch
        {
            < 7 => 0.05f,
            < 30 => 0.02f,
            _ => 0f
        };

        return score;
    }

    private static float CalculateMetadataScore(SaveStateEntity saveState, SearchQueryAnalysis analysis)
    {
        var score = 0f;

        // Auto-save vs Manual preference
        if (analysis.Tags.Contains("autosave") && saveState.IsAutoSave)
        {
            score += 0.10f;
        }
        else if (analysis.Tags.Contains("manual") && !saveState.IsAutoSave)
        {
            score += 0.10f;
        }

        // Favorite boost
        if (saveState.IsFavorite)
        {
            score += 0.03f;
        }

        return score;
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
