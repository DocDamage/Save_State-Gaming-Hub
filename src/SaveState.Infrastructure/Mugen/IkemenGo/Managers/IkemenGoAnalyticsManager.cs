using System.Text.Json;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;

namespace SaveState.Infrastructure.Mugen.IkemenGo.Managers;

/// <summary>
/// Manages IKEMEN GO player statistics, match history, and library analysis.
/// </summary>
public sealed class IkemenGoAnalyticsManager
{
    private readonly ILogger<IkemenGoAnalyticsManager> _logger;
    private readonly ITimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="IkemenGoAnalyticsManager"/> class.
    /// </summary>
    public IkemenGoAnalyticsManager(
        ILogger<IkemenGoAnalyticsManager> logger,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Gets player statistics from IKEMEN GO save data.
    /// </summary>
    public async Task<Result<IkemenGoPlayerStats>> GetPlayerStatsAsync(
        string playerName,
        string dataPath,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting player stats for {Player}", playerName);

            var statsPath = Path.Combine(dataPath, "stats.json");
            if (File.Exists(statsPath))
            {
                var json = await File.ReadAllTextAsync(statsPath, ct);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty(playerName, out var playerStats))
                {
                    var characterUsage = new List<IkemenGoCharacterUsage>();
                    if (playerStats.TryGetProperty("characterUsage", out var charUsage))
                    {
                        foreach (var charStat in charUsage.EnumerateObject())
                        {
                            characterUsage.Add(new IkemenGoCharacterUsage(
                                charStat.Name,
                                charStat.Value.GetProperty("matches").GetInt32(),
                                charStat.Value.GetProperty("wins").GetInt32(),
                                charStat.Value.GetProperty("winRate").GetSingle()));
                        }
                    }

                    var stats = new IkemenGoPlayerStats(
                        playerName,
                        playerStats.GetProperty("totalMatches").GetInt32(),
                        playerStats.GetProperty("wins").GetInt32(),
                        playerStats.GetProperty("losses").GetInt32(),
                        playerStats.GetProperty("draws").GetInt32(),
                        TimeSpan.FromSeconds(playerStats.GetProperty("totalPlayTime").GetDouble()),
                        playerStats.GetProperty("favoriteCharacter").GetString() ?? "Unknown",
                        characterUsage);

                    return Result<IkemenGoPlayerStats>.Success(stats);
                }
            }

            // Return default stats if no data found
            return Result<IkemenGoPlayerStats>.Success(new IkemenGoPlayerStats(
                playerName,
                0, 0, 0, 0,
                TimeSpan.Zero,
                "Unknown",
                new List<IkemenGoCharacterUsage>()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get player stats");
            return Result<IkemenGoPlayerStats>.Failure($"Failed to get stats: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets match history.
    /// </summary>
    public async Task<Result<IReadOnlyList<IkemenGoMatchRecord>>> GetMatchHistoryAsync(
        string playerName,
        string dataPath,
        int limit = 100,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting match history for {Player} (limit: {Limit})", playerName, limit);

            var historyPath = Path.Combine(dataPath, "matches.json");
            if (!File.Exists(historyPath))
            {
                return Result<IReadOnlyList<IkemenGoMatchRecord>>.Success(new List<IkemenGoMatchRecord>());
            }

            var json = await File.ReadAllTextAsync(historyPath, ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var matches = new List<IkemenGoMatchRecord>();
            if (root.TryGetProperty("matches", out var matchesArray))
            {
                foreach (var match in matchesArray.EnumerateArray().Take(limit))
                {
                    ct.ThrowIfCancellationRequested();

                    var record = new IkemenGoMatchRecord(
                        match.GetProperty("timestamp").GetDateTime(),
                        match.GetProperty("mode").GetString() ?? "Unknown",
                        match.GetProperty("player1").GetString() ?? "Player1",
                        match.GetProperty("player2").GetString() ?? "Player2",
                        match.GetProperty("character1").GetString() ?? "Unknown",
                        match.GetProperty("character2").GetString() ?? "Unknown",
                        match.GetProperty("result").GetString() ?? "Unknown",
                        TimeSpan.FromSeconds(match.GetProperty("duration").GetDouble()));

                    matches.Add(record);
                }
            }

            return Result<IReadOnlyList<IkemenGoMatchRecord>>.Success(matches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get match history");
            return Result<IReadOnlyList<IkemenGoMatchRecord>>.Failure($"Failed to get history: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Generates compatibility report for entire content library.
    /// </summary>
    public async Task<Result<IkemenGoLibraryCompatibilityReport>> AnalyzeLibraryCompatibilityAsync(
        string charsPath,
        string stagesPath,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Analyzing library compatibility");

            var reports = new List<IkemenGoCompatibilityReport>();
            int total = 0, full = 0, partial = 0, migration = 0, incompatible = 0;

            // Analyze characters
            if (Directory.Exists(charsPath))
            {
                foreach (var charDir in Directory.GetDirectories(charsPath))
                {
                    ct.ThrowIfCancellationRequested();
                    total++;

                    var report = await AnalyzeCharacterCompatibilityAsync(charDir, ct);
                    reports.Add(report);

                    switch (report.CompatibilityLevel)
                    {
                        case IkemenGoCompatibilityLevel.Full: full++; break;
                        case IkemenGoCompatibilityLevel.Partial: partial++; break;
                        case IkemenGoCompatibilityLevel.RequiresMigration: migration++; break;
                        case IkemenGoCompatibilityLevel.Incompatible: incompatible++; break;
                    }
                }
            }

            // Analyze stages
            if (Directory.Exists(stagesPath))
            {
                foreach (var stageDir in Directory.GetDirectories(stagesPath))
                {
                    ct.ThrowIfCancellationRequested();
                    total++;

                    var report = await AnalyzeStageCompatibilityAsync(stageDir, ct);
                    reports.Add(report);

                    switch (report.CompatibilityLevel)
                    {
                        case IkemenGoCompatibilityLevel.Full: full++; break;
                        case IkemenGoCompatibilityLevel.Partial: partial++; break;
                        case IkemenGoCompatibilityLevel.RequiresMigration: migration++; break;
                        case IkemenGoCompatibilityLevel.Incompatible: incompatible++; break;
                    }
                }
            }

            var result = new IkemenGoLibraryCompatibilityReport(
                total,
                full,
                partial,
                migration,
                incompatible,
                reports);

            return Result<IkemenGoLibraryCompatibilityReport>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze library compatibility");
            return Result<IkemenGoLibraryCompatibilityReport>.Failure($"Analysis failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Records a match result.
    /// </summary>
    public async Task<Result> RecordMatchAsync(
        string dataPath,
        IkemenGoMatchRecord match,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Recording match: {Player1} vs {Player2}", match.Player1Name, match.Player2Name);

            Directory.CreateDirectory(dataPath);
            var historyPath = Path.Combine(dataPath, "matches.json");

            var matches = new List<IkemenGoMatchRecord>();
            if (File.Exists(historyPath))
            {
                var existingResult = await LoadMatchesAsync(historyPath, ct);
                if (existingResult.IsSuccess)
                {
                    matches = existingResult.Value.ToList();
                }
            }

            matches.Insert(0, match);

            // Keep only last 1000 matches
            if (matches.Count > 1000)
            {
                matches = matches.Take(1000).ToList();
            }

            var data = new { matches };
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(data, options);
            await File.WriteAllTextAsync(historyPath, json, ct);

            // Update player stats
            await UpdatePlayerStatsFromMatchAsync(dataPath, match, ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record match");
            return Result.Failure($"Failed to record match: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Updates player statistics based on match outcome.
    /// </summary>
    public async Task<Result> UpdatePlayerStatsAsync(
        string dataPath,
        string playerName,
        MatchOutcome outcome,
        string characterUsed,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Updating stats for {Player}", playerName);

            Directory.CreateDirectory(dataPath);
            var statsPath = Path.Combine(dataPath, "stats.json");

            Dictionary<string, PlayerStatsData> allStats = new();
            if (File.Exists(statsPath))
            {
                var json = await File.ReadAllTextAsync(statsPath, ct);
                allStats = JsonSerializer.Deserialize<Dictionary<string, PlayerStatsData>>(json) 
                    ?? new Dictionary<string, PlayerStatsData>();
            }

            if (!allStats.TryGetValue(playerName, out var playerStats))
            {
                playerStats = new PlayerStatsData
                {
                    TotalMatches = 0,
                    Wins = 0,
                    Losses = 0,
                    Draws = 0,
                    TotalPlayTime = 0,
                    FavoriteCharacter = characterUsed,
                    CharacterUsage = new Dictionary<string, CharacterUsageData>()
                };
            }

            // Update totals
            playerStats.TotalMatches++;
            switch (outcome)
            {
                case MatchOutcome.Win: playerStats.Wins++; break;
                case MatchOutcome.Loss: playerStats.Losses++; break;
                case MatchOutcome.Draw: playerStats.Draws++; break;
            }

            // Update character usage
            if (!playerStats.CharacterUsage.TryGetValue(characterUsed, out var charUsage))
            {
                charUsage = new CharacterUsageData { Matches = 0, Wins = 0 };
            }
            charUsage.Matches++;
            if (outcome == MatchOutcome.Win) charUsage.Wins++;
            playerStats.CharacterUsage[characterUsed] = charUsage;

            // Update favorite character
            playerStats.FavoriteCharacter = playerStats.CharacterUsage
                .OrderByDescending(c => c.Value.Matches)
                .First().Key;

            allStats[playerName] = playerStats;

            var options = new JsonSerializerOptions { WriteIndented = true };
            var updatedJson = JsonSerializer.Serialize(allStats, options);
            await File.WriteAllTextAsync(statsPath, updatedJson, ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update player stats");
            return Result.Failure($"Failed to update stats: {ex.Message}", ErrorType.Internal);
        }
    }

    private async Task<IkemenGoCompatibilityReport> AnalyzeCharacterCompatibilityAsync(
        string characterPath,
        CancellationToken ct)
    {
        var issues = new List<IkemenGoCompatibilityIssue>();
        var suggestions = new List<IkemenGoMigrationSuggestion>();

        // Check for .def file
        var defFiles = Directory.GetFiles(characterPath, "*.def");
        if (defFiles.Length == 0)
        {
            issues.Add(new IkemenGoCompatibilityIssue(
                IkemenGoIssueSeverity.Critical,
                "NO_DEF_FILE",
                "No .def file found",
                null, null));
        }

        // Check for animation files
        var airFiles = Directory.GetFiles(characterPath, "*.air");
        if (airFiles.Length == 0)
        {
            issues.Add(new IkemenGoCompatibilityIssue(
                IkemenGoIssueSeverity.Error,
                "NO_AIR_FILE",
                "No animation (.air) file found",
                null, null));
        }

        // Check for command files
        var cmdFiles = Directory.GetFiles(characterPath, "*.cmd");
        if (cmdFiles.Length == 0)
        {
            issues.Add(new IkemenGoCompatibilityIssue(
                IkemenGoIssueSeverity.Warning,
                "NO_CMD_FILE",
                "No command (.cmd) file found",
                null, null));
        }

        await Task.CompletedTask;

        var level = DetermineCompatibilityLevel(issues);
        return new IkemenGoCompatibilityReport(
            characterPath,
            Path.GetFileName(characterPath),
            level,
            issues,
            suggestions);
    }

    private async Task<IkemenGoCompatibilityReport> AnalyzeStageCompatibilityAsync(
        string stagePath,
        CancellationToken ct)
    {
        var issues = new List<IkemenGoCompatibilityIssue>();
        var suggestions = new List<IkemenGoMigrationSuggestion>();

        var defFiles = Directory.GetFiles(stagePath, "*.def");
        if (defFiles.Length == 0)
        {
            issues.Add(new IkemenGoCompatibilityIssue(
                IkemenGoIssueSeverity.Critical,
                "NO_STAGE_DEF",
                "No stage definition file found",
                null, null));
        }

        await Task.CompletedTask;

        var level = DetermineCompatibilityLevel(issues);
        return new IkemenGoCompatibilityReport(
            stagePath,
            Path.GetFileName(stagePath),
            level,
            issues,
            suggestions);
    }

    private IkemenGoCompatibilityLevel DetermineCompatibilityLevel(List<IkemenGoCompatibilityIssue> issues)
    {
        if (issues.Any(i => i.Severity == IkemenGoIssueSeverity.Critical))
            return IkemenGoCompatibilityLevel.Incompatible;

        if (issues.Any(i => i.Severity == IkemenGoIssueSeverity.Error))
            return IkemenGoCompatibilityLevel.RequiresMigration;

        if (issues.Any(i => i.Severity == IkemenGoIssueSeverity.Warning))
            return IkemenGoCompatibilityLevel.Partial;

        return IkemenGoCompatibilityLevel.Full;
    }

    private async Task<Result<List<IkemenGoMatchRecord>>> LoadMatchesAsync(string path, CancellationToken ct)
    {
        try
        {
            var json = await File.ReadAllTextAsync(path, ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var matches = new List<IkemenGoMatchRecord>();
            if (root.TryGetProperty("matches", out var matchesArray))
            {
                foreach (var match in matchesArray.EnumerateArray())
                {
                    matches.Add(new IkemenGoMatchRecord(
                        match.GetProperty("timestamp").GetDateTime(),
                        match.GetProperty("mode").GetString() ?? "Unknown",
                        match.GetProperty("player1").GetString() ?? "Player1",
                        match.GetProperty("player2").GetString() ?? "Player2",
                        match.GetProperty("character1").GetString() ?? "Unknown",
                        match.GetProperty("character2").GetString() ?? "Unknown",
                        match.GetProperty("result").GetString() ?? "Unknown",
                        TimeSpan.FromSeconds(match.GetProperty("duration").GetDouble())));
                }
            }

            return Result<List<IkemenGoMatchRecord>>.Success(matches);
        }
        catch (Exception ex)
        {
            return Result<List<IkemenGoMatchRecord>>.Failure(ex.Message, ErrorType.Internal);
        }
    }

    private async Task UpdatePlayerStatsFromMatchAsync(string dataPath, IkemenGoMatchRecord match, CancellationToken ct)
    {
        // Update player 1 stats
        var p1Outcome = match.Result.ToLowerInvariant() switch
        {
            "win" or "player1" => MatchOutcome.Win,
            "loss" or "player2" => MatchOutcome.Loss,
            _ => MatchOutcome.Draw
        };
        await UpdatePlayerStatsAsync(dataPath, match.Player1Name, p1Outcome, match.Player1Character, ct);

        // Update player 2 stats
        var p2Outcome = match.Result.ToLowerInvariant() switch
        {
            "win" or "player2" => MatchOutcome.Win,
            "loss" or "player1" => MatchOutcome.Loss,
            _ => MatchOutcome.Draw
        };
        await UpdatePlayerStatsAsync(dataPath, match.Player2Name, p2Outcome, match.Player2Character, ct);
    }
}

/// <summary>
/// Match outcome.
/// </summary>
public enum MatchOutcome
{
    Win,
    Loss,
    Draw
}

/// <summary>
/// Internal player stats data structure.
/// </summary>
internal class PlayerStatsData
{
    public int TotalMatches { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Draws { get; set; }
    public double TotalPlayTime { get; set; }
    public string FavoriteCharacter { get; set; } = "Unknown";
    public Dictionary<string, CharacterUsageData> CharacterUsage { get; set; } = new();
}

/// <summary>
/// Internal character usage data structure.
/// </summary>
internal class CharacterUsageData
{
    public int Matches { get; set; }
    public int Wins { get; set; }
    public float WinRate => Matches > 0 ? (float)Wins / Matches : 0;
}
