namespace SaveState.Infrastructure.Ai.Services;

using Microsoft.Extensions.Logging;
using SaveState.Core.Ai.Services;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Enums;
using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

public class NaturalLanguageGameSearch : INaturalLanguageGameSearch
{
    private readonly IAiOrchestrator _aiOrchestrator;
    private readonly ILogger<NaturalLanguageGameSearch> _logger;

    public NaturalLanguageGameSearch(IAiOrchestrator aiOrchestrator, ILogger<NaturalLanguageGameSearch> logger)
    {
        _aiOrchestrator = aiOrchestrator;
        _logger = logger;
    }

    public async Task<CollectionFilter> ParseQueryAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new CollectionFilter();
        }

        var heuristicFilter = ParseHeuristicFilter(query);
        var aiFilter = await TryParseWithAiAsync(query, ct);

        return MergeFilters(aiFilter, heuristicFilter);
    }

    private async Task<CollectionFilter?> TryParseWithAiAsync(string query, CancellationToken ct)
    {
        var prompt = $@"
You are a gaming library assistant. Identify search criteria from the user's natural language query.
Return a valid JSON object matching this structure (all fields optional):
{{
    ""PlatformName"": ""string"",
    ""Genre"": ""string"",
    ""MinReleaseYear"": int,
    ""MaxReleaseYear"": int,
    ""Status"": ""Installed"" | ""NotInstalled"" | ""Running"" | ""Updating"" | null,
    ""HasAchievements"": bool,
    ""MinRating"": int,
    ""MinPlaytimeHours"": int,
    ""MaxPlaytimeHours"": int,
    ""MaxDaysSinceLastPlayed"": int,
    ""IsCompleted"": bool
}}

User Query: ""{query}""

Rules:
- For decadal terms like ""90s"", set MinReleaseYear=1990 and MaxReleaseYear=1999.
- For specific years ""from 2004"", set MinReleaseYear=2004.
- For ""best"" games, imply MinRating=80.
- Return ONLY the JSON. No Markdown formatting.
";

        try
        {
            var result = await _aiOrchestrator.GenerateTextAsync(prompt, ct);
            if (!result.IsSuccess)
            {
                _logger.LogWarning("AI generation failed for query: {Query}", query);
                return null;
            }

            var json = ExtractJsonObject(result.Value);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return TryParseFilterFromJson(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse natural language query: {Query}", query);
            // Return empty filter so we don't crash, maybe results in no filtering or empty results depending on caller
            return null;
        }
    }

    private static string ExtractJsonObject(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var trimmed = text.Trim();
        if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed.Substring(7);
        }
        if (trimmed.StartsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed.Substring(3);
        }
        if (trimmed.EndsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed.Substring(0, trimmed.Length - 3);
        }

        var startIndex = trimmed.IndexOf('{');
        var endIndex = trimmed.LastIndexOf('}');

        if (startIndex >= 0 && endIndex > startIndex)
        {
            return trimmed.Substring(startIndex, endIndex - startIndex + 1).Trim();
        }

        return string.Empty;
    }

    private static CollectionFilter? TryParseFilterFromJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var root = document.RootElement;

        var platformName = GetString(root, "PlatformName");
        var genre = GetString(root, "Genre");
        var status = ParseStatus(GetString(root, "Status"));
        var hasAchievements = GetBool(root, "HasAchievements");
        var minRating = GetInt(root, "MinRating");
        var minReleaseYear = GetInt(root, "MinReleaseYear");
        var maxReleaseYear = GetInt(root, "MaxReleaseYear");
        var maxDaysSinceLastPlayed = GetInt(root, "MaxDaysSinceLastPlayed");
        var tag = GetString(root, "Tag");
        var isCompleted = GetBool(root, "IsCompleted");
        var isInBacklog = GetBool(root, "IsInBacklog");

        var minPlaytime = ParseDuration(root, "MinPlaytimeHours", "MinPlaytimeMinutes", "MinPlaytime");
        var maxPlaytime = ParseDuration(root, "MaxPlaytimeHours", "MaxPlaytimeMinutes", "MaxPlaytime");

        return new CollectionFilter
        {
            PlatformName = platformName,
            Genre = genre,
            Status = status,
            HasAchievements = hasAchievements,
            MinRating = minRating,
            MinReleaseYear = minReleaseYear,
            MaxReleaseYear = maxReleaseYear,
            MaxDaysSinceLastPlayed = maxDaysSinceLastPlayed,
            MinPlaytime = minPlaytime,
            MaxPlaytime = maxPlaytime,
            Tag = tag,
            IsCompleted = isCompleted,
            IsInBacklog = isInBacklog
        };
    }

    private static CollectionFilter MergeFilters(CollectionFilter? primary, CollectionFilter fallback)
    {
        var baseFilter = primary ?? new CollectionFilter();

        return baseFilter with
        {
            MaxPlaytime = baseFilter.MaxPlaytime ?? fallback.MaxPlaytime,
            MinPlaytime = baseFilter.MinPlaytime ?? fallback.MinPlaytime,
            MaxDaysSinceLastPlayed = baseFilter.MaxDaysSinceLastPlayed ?? fallback.MaxDaysSinceLastPlayed,
            PlatformName = baseFilter.PlatformName ?? fallback.PlatformName,
            Genre = baseFilter.Genre ?? fallback.Genre,
            Status = baseFilter.Status ?? fallback.Status,
            Tag = baseFilter.Tag ?? fallback.Tag,
            HasAchievements = baseFilter.HasAchievements ?? fallback.HasAchievements,
            MinRating = baseFilter.MinRating ?? fallback.MinRating,
            MinReleaseYear = baseFilter.MinReleaseYear ?? fallback.MinReleaseYear,
            MaxReleaseYear = baseFilter.MaxReleaseYear ?? fallback.MaxReleaseYear,
            IsCompleted = baseFilter.IsCompleted ?? fallback.IsCompleted,
            IsInBacklog = baseFilter.IsInBacklog ?? fallback.IsInBacklog
        };
    }

    private static CollectionFilter ParseHeuristicFilter(string query)
    {
        var normalized = query.Trim().ToLowerInvariant();
        var filter = new CollectionFilter();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return filter;
        }

        if (ContainsAny(normalized, "not installed", "uninstalled"))
        {
            filter = filter with { Status = GameStatus.NotInstalled };
        }
        else if (ContainsAny(normalized, "installed"))
        {
            filter = filter with { Status = GameStatus.Installed };
        }

        if (ContainsAny(normalized, "running", "playing now"))
        {
            filter = filter with { Status = GameStatus.Running };
        }

        if (ContainsAny(normalized, "haven't finished", "havent finished", "not finished", "unfinished", "not completed", "incomplete"))
        {
            filter = filter with { IsCompleted = false };
        }
        else if (ContainsAny(normalized, "completed", "finished", "beaten"))
        {
            filter = filter with { IsCompleted = true };
        }

        if (ContainsAny(normalized, "last month", "past month", "in the last month", "this month"))
        {
            filter = filter with { MaxDaysSinceLastPlayed = 30 };
        }
        else if (ContainsAny(normalized, "last week", "past week", "in the last week", "this week"))
        {
            filter = filter with { MaxDaysSinceLastPlayed = 7 };
        }
        else if (ContainsAny(normalized, "last year", "past year", "in the last year", "this year"))
        {
            filter = filter with { MaxDaysSinceLastPlayed = 365 };
        }
        else if (ContainsAny(normalized, "yesterday"))
        {
            filter = filter with { MaxDaysSinceLastPlayed = 1 };
        }

        if (ContainsAny(normalized, "started", "played", "playing", "picked up"))
        {
            filter = filter with { MinPlaytime = filter.MinPlaytime ?? TimeSpan.FromMinutes(1) };
        }

        if (ContainsAny(normalized, "best", "top rated", "highest rated"))
        {
            filter = filter with { MinRating = filter.MinRating ?? 80 };
        }

        var genre = ExtractGenre(normalized);
        if (!string.IsNullOrWhiteSpace(genre))
        {
            filter = filter with { Genre = genre };
        }

        var platform = ExtractPlatform(normalized);
        if (!string.IsNullOrWhiteSpace(platform))
        {
            filter = filter with { PlatformName = platform };
        }

        var playtimeRange = ExtractPlaytimeRange(normalized);
        if (playtimeRange.Min.HasValue)
        {
            filter = filter with { MinPlaytime = playtimeRange.Min };
        }
        if (playtimeRange.Max.HasValue)
        {
            filter = filter with { MaxPlaytime = playtimeRange.Max };
        }

        var releaseYears = ExtractReleaseYearRange(normalized);
        if (releaseYears.Min.HasValue)
        {
            filter = filter with { MinReleaseYear = releaseYears.Min };
        }
        if (releaseYears.Max.HasValue)
        {
            filter = filter with { MaxReleaseYear = releaseYears.Max };
        }

        return filter;
    }

    private static bool ContainsAny(string text, params string[] terms)
    {
        foreach (var term in terms)
        {
            if (text.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string? ExtractGenre(string normalized)
    {
        if (ContainsAny(normalized, "jrpg"))
        {
            return "JRPG";
        }

        if (ContainsAny(normalized, "rpg", "role-playing"))
        {
            return "RPG";
        }

        if (ContainsAny(normalized, "strategy"))
        {
            return "Strategy";
        }

        if (ContainsAny(normalized, "adventure"))
        {
            return "Adventure";
        }

        if (ContainsAny(normalized, "action"))
        {
            return "Action";
        }

        if (ContainsAny(normalized, "simulation", "sim"))
        {
            return "Simulation";
        }

        if (ContainsAny(normalized, "shooter"))
        {
            return "Shooter";
        }

        if (ContainsAny(normalized, "platformer"))
        {
            return "Platformer";
        }

        return null;
    }

    private static string? ExtractPlatform(string normalized)
    {
        if (ContainsAny(normalized, "steam deck"))
        {
            return "Steam Deck";
        }

        if (ContainsAny(normalized, "switch", "nintendo switch"))
        {
            return "Switch";
        }

        if (ContainsAny(normalized, "ps5", "playstation 5"))
        {
            return "PS5";
        }

        if (ContainsAny(normalized, "ps4", "playstation 4"))
        {
            return "PS4";
        }

        if (ContainsAny(normalized, "xbox"))
        {
            return "Xbox";
        }

        if (ContainsAny(normalized, "pc", "windows"))
        {
            return "PC";
        }

        return null;
    }

    private static (TimeSpan? Min, TimeSpan? Max) ExtractPlaytimeRange(string normalized)
    {
        var betweenMatch = Regex.Match(normalized, @"between\s+(\d+)\s*(hours|hrs|hr|h)\s+and\s+(\d+)\s*(hours|hrs|hr|h)");
        if (betweenMatch.Success)
        {
            var minHours = int.Parse(betweenMatch.Groups[1].Value);
            var maxHours = int.Parse(betweenMatch.Groups[3].Value);
            return (TimeSpan.FromHours(minHours), TimeSpan.FromHours(maxHours));
        }

        var underHours = Regex.Match(normalized, @"(under|less than|at most|max)\s+(\d+)\s*(hours|hrs|hr|h)");
        if (underHours.Success)
        {
            var hours = int.Parse(underHours.Groups[2].Value);
            return (null, TimeSpan.FromHours(hours));
        }

        var overHours = Regex.Match(normalized, @"(over|more than|at least|min)\s+(\d+)\s*(hours|hrs|hr|h)");
        if (overHours.Success)
        {
            var hours = int.Parse(overHours.Groups[2].Value);
            return (TimeSpan.FromHours(hours), null);
        }

        var underMinutes = Regex.Match(normalized, @"(under|less than|at most|max)\s+(\d+)\s*(minutes|mins|min|m)");
        if (underMinutes.Success)
        {
            var minutes = int.Parse(underMinutes.Groups[2].Value);
            return (null, TimeSpan.FromMinutes(minutes));
        }

        var overMinutes = Regex.Match(normalized, @"(over|more than|at least|min)\s+(\d+)\s*(minutes|mins|min|m)");
        if (overMinutes.Success)
        {
            var minutes = int.Parse(overMinutes.Groups[2].Value);
            return (TimeSpan.FromMinutes(minutes), null);
        }

        return (null, null);
    }

    private static (int? Min, int? Max) ExtractReleaseYearRange(string normalized)
    {
        var decadeMatch = Regex.Match(normalized, @"\b(19|20)?(\d{2})s\b");
        if (decadeMatch.Success)
        {
            var decadeValue = int.Parse(decadeMatch.Groups[2].Value);
            var century = decadeMatch.Groups[1].Success
                ? int.Parse(decadeMatch.Groups[1].Value)
                : decadeValue >= 30 ? 19 : 20;

            var min = (century * 100) + decadeValue;
            return (min, min + 9);
        }

        var fromMatch = Regex.Match(normalized, @"\b(from|since|after)\s+((19|20)\d{2})\b");
        if (fromMatch.Success)
        {
            var year = int.Parse(fromMatch.Groups[2].Value);
            return (year, null);
        }

        var beforeMatch = Regex.Match(normalized, @"\b(before|prior to|earlier than)\s+((19|20)\d{2})\b");
        if (beforeMatch.Success)
        {
            var year = int.Parse(beforeMatch.Groups[2].Value);
            return (null, year);
        }

        return (null, null);
    }

    private static string? GetString(JsonElement root, string name)
    {
        if (!TryGetPropertyIgnoreCase(root, name, out var element))
        {
            return null;
        }

        return element.ValueKind == JsonValueKind.String ? element.GetString() : null;
    }

    private static bool? GetBool(JsonElement root, string name)
    {
        if (!TryGetPropertyIgnoreCase(root, name, out var element))
        {
            return null;
        }

        return element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False
            ? element.GetBoolean()
            : null;
    }

    private static int? GetInt(JsonElement root, string name)
    {
        if (!TryGetPropertyIgnoreCase(root, name, out var element))
        {
            return null;
        }

        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var value))
        {
            return value;
        }

        if (element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out value))
        {
            return value;
        }

        return null;
    }

    private static TimeSpan? ParseDuration(JsonElement root, string hoursProperty, string minutesProperty, string fallbackProperty)
    {
        var hours = GetInt(root, hoursProperty);
        if (hours.HasValue)
        {
            return TimeSpan.FromHours(hours.Value);
        }

        var minutes = GetInt(root, minutesProperty);
        if (minutes.HasValue)
        {
            return TimeSpan.FromMinutes(minutes.Value);
        }

        if (TryGetPropertyIgnoreCase(root, fallbackProperty, out var element))
        {
            if (element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out var raw))
            {
                return TimeSpan.FromHours(raw);
            }

            if (element.ValueKind == JsonValueKind.String)
            {
                var text = element.GetString();
                if (string.IsNullOrWhiteSpace(text))
                {
                    return null;
                }

                if (TimeSpan.TryParse(text, out var parsed))
                {
                    return parsed;
                }

                var match = Regex.Match(text, @"(\d+)\s*(hours|hrs|hr|h)");
                if (match.Success)
                {
                    return TimeSpan.FromHours(int.Parse(match.Groups[1].Value));
                }

                match = Regex.Match(text, @"(\d+)\s*(minutes|mins|min|m)");
                if (match.Success)
                {
                    return TimeSpan.FromMinutes(int.Parse(match.Groups[1].Value));
                }
            }
        }

        return null;
    }

    private static GameStatus? ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        var normalized = status.Trim().ToLowerInvariant();
        return normalized switch
        {
            "installed" => GameStatus.Installed,
            "notinstalled" => GameStatus.NotInstalled,
            "not installed" => GameStatus.NotInstalled,
            "uninstalled" => GameStatus.NotInstalled,
            "running" => GameStatus.Running,
            "playing" => GameStatus.Running,
            "updating" => GameStatus.Updating,
            _ => Enum.TryParse<GameStatus>(status, true, out var parsed) ? parsed : null
        };
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
