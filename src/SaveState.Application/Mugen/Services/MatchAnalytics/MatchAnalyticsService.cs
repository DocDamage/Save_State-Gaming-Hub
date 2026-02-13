using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Services.MatchAnalytics.Engines;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;

namespace SaveState.Application.Mugen.Services.MatchAnalytics;

/// <summary>
/// Refactored match analytics service acting as a coordinator for specialized engines.
/// Provides comprehensive match analysis, statistics, pattern recognition, and recommendations.
/// </summary>
public class MatchAnalyticsService : IMatchAnalyticsService
{
    private readonly ILogger<MatchAnalyticsService> _logger;
    private readonly ICacheService _cache;
    private readonly MatchDataEngine _matchDataEngine;
    private readonly StatisticEngine _statisticEngine;
    private readonly PatternEngine _patternEngine;
    private readonly ReportingEngine _reportingEngine;
    private readonly VisualizationEngine _visualizationEngine;

    public MatchAnalyticsService(
        ILogger<MatchAnalyticsService> logger,
        ILoggerFactory loggerFactory,
        ICacheService cache)
    {
        _logger = logger;
        _cache = cache;

        // Initialize engines
        _matchDataEngine = new MatchDataEngine(loggerFactory.CreateLogger<MatchDataEngine>());
        _statisticEngine = new StatisticEngine(loggerFactory.CreateLogger<StatisticEngine>());
        _patternEngine = new PatternEngine(loggerFactory.CreateLogger<PatternEngine>());
        _visualizationEngine = new VisualizationEngine(loggerFactory.CreateLogger<VisualizationEngine>());
        _reportingEngine = new ReportingEngine(
            loggerFactory.CreateLogger<ReportingEngine>(),
            _statisticEngine,
            _patternEngine);
    }

    /// <inheritdoc />
    public async Task<Result> RecordMatchDataAsync(Core.Mugen.Services.MatchRecording matchData, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Recording match data for match {MatchId}", matchData.MatchId);

            // Convert to internal model
            var internalMatchData = ConvertToInternalModel(matchData);

            // Validate match data
            var validation = _matchDataEngine.ValidateMatchData(internalMatchData);
            if (!validation.IsValid)
            {
                return Result.Failure($"Validation failed: {string.Join(", ", validation.Errors)}");
            }

            // Store match data
            _matchDataEngine.RecordMatch(internalMatchData);

            // Analyze patterns asynchronously
            await _patternEngine.AnalyzeMatchAsync(internalMatchData, ct);

            // Update player caches
            await UpdatePlayerStatisticsCacheAsync(internalMatchData.Player1Id, ct);
            await UpdatePlayerStatisticsCacheAsync(internalMatchData.Player2Id, ct);

            _logger.LogInformation("Match data recorded successfully for {MatchId}", matchData.MatchId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording match data for {MatchId}", matchData.MatchId);
            return Result.Failure($"Failed to record match data: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<Core.Mugen.Services.MatchAnalysis>> AnalyzeMatchAsync(Guid matchId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Analyzing match {MatchId}", matchId);

            // Try cache first
            var cacheKey = $"match_analysis_{matchId}";
            if (_cache.TryGetValue(cacheKey, out Core.Mugen.Services.MatchAnalysis? cached) && cached != null)
            {
                return Result.Success(cached);
            }

            // Find match data
            var matchData = _matchDataEngine.FindMatch(matchId);
            if (matchData == null)
            {
                return Result.Failure<Core.Mugen.Services.MatchAnalysis>("Match not found");
            }

            // Perform analysis using engines
            var player1Performance = AnalyzePlayerPerformance(matchData.Player1Id, matchData);
            var player2Performance = AnalyzePlayerPerformance(matchData.Player2Id, matchData);

            var analysis = new Core.Mugen.Services.MatchAnalysis(
                MatchId: matchId,
                Player1Performance: ConvertToLegacyPerformance(player1Performance),
                Player2Performance: ConvertToLegacyPerformance(player2Performance),
                KeyMoments: IdentifyKeyMoments(matchData),
                TurningPoints: IdentifyTurningPoints(matchData),
                OverallAnalysis: GenerateOverallAnalysis(matchData, player1Performance, player2Performance)
            );

            // Cache result
            _cache.Set(cacheKey, analysis, TimeSpan.FromHours(24));

            return Result.Success(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing match {MatchId}", matchId);
            return Result.Failure<Core.Mugen.Services.MatchAnalysis>($"Analysis failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<Core.Mugen.Services.PlayerStatistics>> GetPlayerStatisticsAsync(Guid playerId, CancellationToken ct = default)
    {
        try
        {
            // Try cache first
            var cacheKey = $"player_stats_{playerId}";
            if (_cache.TryGetValue(cacheKey, out Core.Mugen.Services.PlayerStatistics? cached) && cached != null)
            {
                return Result.Success(cached);
            }

            _logger.LogInformation("Calculating player statistics for {PlayerId}", playerId);

            // Get matches for player
            var matches = _matchDataEngine.GetPlayerMatches(playerId);
            if (!matches.Any())
            {
                return Result.Failure<Core.Mugen.Services.PlayerStatistics>("No matches found for player");
            }

            // Calculate statistics using engine
            var stats = await _statisticEngine.CalculatePlayerStatisticsAsync(playerId, matches, ct);

            // Convert to legacy format
            var legacyStats = ConvertToLegacyStatistics(stats);

            // Cache results
            _cache.Set(cacheKey, legacyStats, TimeSpan.FromHours(1));

            return Result.Success(legacyStats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting player statistics for {PlayerId}", playerId);
            return Result.Failure<Core.Mugen.Services.PlayerStatistics>($"Statistics calculation failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<Core.Mugen.Services.PerformanceTrends>> GetPerformanceTrendsAsync(
        Guid playerId, DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Analyzing performance trends for {PlayerId}", playerId);

            // Get matches in range
            var matches = _matchDataEngine.GetMatchesInRange(playerId, startDate, endDate);
            if (!matches.Any())
            {
                return Result.Failure<Core.Mugen.Services.PerformanceTrends>("No matches found in date range");
            }

            // Use visualization engine to prepare trends
            var trends = _visualizationEngine.PrepareTrendVisualization(playerId, matches, startDate, endDate);

            return Result.Success(ConvertToLegacyTrends(trends));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing performance trends for {PlayerId}", playerId);
            return Result.Failure<Core.Mugen.Services.PerformanceTrends>($"Trend analysis failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<Core.Mugen.Services.PlayerPattern>>> IdentifyPatternsAsync(Guid playerId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Identifying patterns for player {PlayerId}", playerId);

            // Get recent matches
            var matches = _matchDataEngine.GetRecentPlayerMatches(playerId, 50);
            if (!matches.Any())
            {
                return Result.Failure<IReadOnlyList<Core.Mugen.Services.PlayerPattern>>("Insufficient match data");
            }

            // Identify patterns using engine
            var patterns = await _patternEngine.IdentifyPatternsAsync(playerId, matches, ct);

            // Convert to legacy format
            var legacyPatterns = patterns.Select(ConvertToLegacyPattern).ToList();

            return Result.Success<IReadOnlyList<Core.Mugen.Services.PlayerPattern>>(legacyPatterns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error identifying patterns for {PlayerId}", playerId);
            return Result.Failure<IReadOnlyList<Core.Mugen.Services.PlayerPattern>>($"Pattern identification failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<Core.Mugen.Services.ImprovementRecommendation>>> GetImprovementRecommendationsAsync(
        Guid playerId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating improvement recommendations for {PlayerId}", playerId);

            // Gather data
            var statisticsResult = await GetPlayerStatisticsAsync(playerId, ct);
            if (!statisticsResult.IsSuccess)
            {
                return Result.Failure<IReadOnlyList<Core.Mugen.Services.ImprovementRecommendation>>(statisticsResult.Error);
            }

            var patternsResult = await IdentifyPatternsAsync(playerId, ct);
            var trendsResult = await GetPerformanceTrendsAsync(playerId, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, ct);

            // Convert data for reporting engine
            var matches = _matchDataEngine.GetPlayerMatches(playerId);
            var stats = await _statisticEngine.CalculatePlayerStatisticsAsync(playerId, matches, ct);
            var patterns = await _patternEngine.IdentifyPatternsAsync(playerId, matches, ct);
            var trends = trendsResult.IsSuccess
                ? ConvertFromLegacyTrends(trendsResult.Value)
                : new PerformanceTrends(playerId, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow,
                    Array.Empty<TrendPoint>(), Array.Empty<TrendPoint>(), Array.Empty<TrendPoint>(), Array.Empty<string>());

            // Generate recommendations
            var recommendations = await _reportingEngine.GenerateRecommendationsAsync(stats, patterns, trends, ct);

            // Convert to legacy format
            var legacyRecommendations = recommendations.Select(ConvertToLegacyRecommendation).ToList();

            return Result.Success<IReadOnlyList<Core.Mugen.Services.ImprovementRecommendation>>(legacyRecommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating recommendations for {PlayerId}", playerId);
            return Result.Failure<IReadOnlyList<Core.Mugen.Services.ImprovementRecommendation>>($"Recommendation generation failed: {ex.Message}");
        }
    }

    #region Private Helper Methods

    private PlayerMatchStats AnalyzePlayerPerformance(Guid playerId, MatchData matchData)
    {
        var isPlayer1 = matchData.Player1Id == playerId;

        var totalDamageDealt = matchData.Rounds.Sum(r =>
            r.Hits.Where(h => h.AttackerId == playerId).Sum(h => h.Damage));

        var totalDamageReceived = matchData.Rounds.Sum(r =>
            r.Hits.Where(h => h.DefenderId == playerId).Sum(h => h.Damage));

        var longestCombo = matchData.Rounds.Max(r =>
            r.Combos.Where(c => c.PlayerId == playerId).Max(c => (int?)c.Length) ?? 0);

        var specialMovesUsed = matchData.Rounds.Sum(r =>
            r.SpecialMoves.Count(sm => sm.PlayerId == playerId));

        var playerInputs = matchData.InputEvents.Where(ie => ie.PlayerId == playerId).ToList();
        var accuracy = playerInputs.Any()
            ? (decimal)playerInputs.Count(ie => ie.Type == AnalyticsInputType.ButtonPress) / playerInputs.Count()
            : 0;

        var strengths = IdentifyPlayerStrengths(playerId, matchData);
        var weaknesses = IdentifyPlayerWeaknesses(playerId, matchData);

        return new PlayerMatchStats(
            PlayerId: playerId,
            TotalDamageDealt: totalDamageDealt,
            TotalDamageReceived: totalDamageReceived,
            LongestCombo: longestCombo,
            SpecialMovesUsed: specialMovesUsed,
            Accuracy: accuracy,
            Strengths: strengths,
            Weaknesses: weaknesses
        );
    }

    private IReadOnlyList<string> IdentifyPlayerStrengths(Guid playerId, MatchData matchData)
    {
        var strengths = new List<string>();

        var playerCombos = matchData.Rounds.SelectMany(r => r.Combos.Where(c => c.PlayerId == playerId));
        if (playerCombos.Any(c => c.Length >= 5))
        {
            strengths.Add("Strong combo execution");
        }

        var specialMoves = matchData.Rounds.SelectMany(r => r.SpecialMoves.Where(sm => sm.PlayerId == playerId));
        if (specialMoves.Any(sm => sm.Damage >= 100))
        {
            strengths.Add("Effective special move usage");
        }

        var playerInputs = matchData.InputEvents.Where(ie => ie.PlayerId == playerId);
        var accuracy = playerInputs.Any()
            ? (decimal)playerInputs.Count(ie => ie.Type == AnalyticsInputType.ButtonPress) / playerInputs.Count()
            : 0;
        if (accuracy >= 0.8m)
        {
            strengths.Add("High input accuracy");
        }

        return strengths;
    }

    private IReadOnlyList<string> IdentifyPlayerWeaknesses(Guid playerId, MatchData matchData)
    {
        var weaknesses = new List<string>();

        var totalDamageReceived = matchData.Rounds.Sum(r =>
            r.Hits.Where(h => h.DefenderId == playerId).Sum(h => h.Damage));

        if (totalDamageReceived > 500)
        {
            weaknesses.Add("High damage received - defensive improvements needed");
        }

        var interruptedCombos = matchData.Rounds.SelectMany(r =>
            r.Combos.Where(c => c.PlayerId != playerId && c.Length >= 3));
        if (interruptedCombos.Any())
        {
            weaknesses.Add("Combos frequently interrupted - punish game needs work");
        }

        return weaknesses;
    }

    private IReadOnlyList<string> IdentifyKeyMoments(MatchData matchData)
    {
        var keyMoments = new List<string>();

        foreach (var round in matchData.Rounds)
        {
            var highDamageCombos = round.Combos.Where(c => c.TotalDamage >= 200);
            foreach (var combo in highDamageCombos)
            {
                keyMoments.Add($"High damage combo ({combo.TotalDamage} damage)");
            }

            var specialMoves = round.SpecialMoves.GroupBy(sm => sm.PlayerId);
            foreach (var group in specialMoves)
            {
                if (group.Count() >= 3)
                {
                    keyMoments.Add($"Special move chain by Player {group.Key} ({group.Count()} moves)");
                }
            }
        }

        return keyMoments;
    }

    private IReadOnlyList<string> IdentifyTurningPoints(MatchData matchData)
    {
        var turningPoints = new List<string>();

        for (int i = 0; i < matchData.Rounds.Count; i++)
        {
            var round = matchData.Rounds[i];
            var roundHits = round.Hits.ToList();

            for (int j = 0; j < roundHits.Count - 1; j++)
            {
                if (Math.Abs(roundHits[j].Damage - roundHits[j + 1].Damage) >= 50)
                {
                    turningPoints.Add($"Major damage swing in Round {i + 1}");
                }
            }

            var lastHits = round.Hits.TakeLast(3);
            if (lastHits.Any())
            {
                turningPoints.Add($"Round {i + 1} finished with {lastHits.Last().MoveName}");
            }
        }

        return turningPoints;
    }

    private string GenerateOverallAnalysis(MatchData matchData, PlayerMatchStats p1Perf, PlayerMatchStats p2Perf)
    {
        var analysis = new List<string>();

        var damageDiff = p1Perf.TotalDamageDealt - p2Perf.TotalDamageDealt;
        if (Math.Abs(damageDiff) >= 100)
        {
            var leader = damageDiff > 0 ? "Player 1" : "Player 2";
            analysis.Add($"{leader} dominated damage output by {Math.Abs(damageDiff)} points");
        }

        var comboDiff = p1Perf.LongestCombo - p2Perf.LongestCombo;
        if (Math.Abs(comboDiff) >= 3)
        {
            var comboLeader = comboDiff > 0 ? "Player 1" : "Player 2";
            analysis.Add($"{comboLeader} showed superior combo execution");
        }

        var matchDuration = matchData.EndTime - matchData.StartTime;
        if (matchDuration.TotalMinutes < 2)
        {
            analysis.Add("Fast-paced match with quick rounds");
        }
        else if (matchDuration.TotalMinutes > 10)
        {
            analysis.Add("Extended match with strategic gameplay");
        }

        return string.Join(". ", analysis);
    }

    private async Task UpdatePlayerStatisticsCacheAsync(Guid playerId, CancellationToken ct)
    {
        try
        {
            var cacheKey = $"player_stats_{playerId}";
            _cache.Remove(cacheKey);

            // Force recalculation
            var matches = _matchDataEngine.GetPlayerMatches(playerId);
            if (matches.Any())
            {
                var stats = await _statisticEngine.CalculatePlayerStatisticsAsync(playerId, matches, ct);
                var legacyStats = ConvertToLegacyStatistics(stats);
                _cache.Set(cacheKey, legacyStats, TimeSpan.FromHours(1));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error updating cache for {PlayerId}", playerId);
        }
    }

    #endregion

    #region Model Conversion Methods

    private MatchData ConvertToInternalModel(Core.Mugen.Services.MatchRecording recording)
    {
        return new MatchData(
            MatchId: recording.MatchId,
            Player1Id: recording.Player1Id,
            Player2Id: recording.Player2Id,
            Player1Character: recording.Player1Character,
            Player2Character: recording.Player2Character,
            StartTime: recording.StartTime,
            EndTime: recording.EndTime,
            Rounds: recording.Rounds.Select(r => new RoundData(
                RoundNumber: r.RoundNumber,
                WinnerId: r.WinnerId,
                Duration: r.Duration,
                Hits: r.Hits.Select(h => new HitData(
                    AttackerId: h.AttackerId,
                    DefenderId: h.DefenderId,
                    MoveName: h.MoveName,
                    Damage: h.Damage,
                    CounterHit: h.CounterHit,
                    Timestamp: h.Timestamp
                )).ToList(),
                SpecialMoves: r.SpecialMoves.Select(sm => new SpecialMoveData(
                    PlayerId: sm.PlayerId,
                    MoveName: sm.MoveName,
                    Damage: sm.Damage,
                    Timestamp: sm.Timestamp
                )).ToList(),
                Combos: r.Combos.Select(c => new ComboData(
                    PlayerId: c.PlayerId,
                    Length: c.Length,
                    TotalDamage: c.TotalDamage,
                    Duration: c.Duration,
                    Moves: c.Moves
                )).ToList()
            )).ToList(),
            InputEvents: recording.InputEvents.Select(ie => new InputEventData(
                PlayerId: ie.PlayerId,
                Input: ie.Input,
                Timestamp: ie.Timestamp,
                Type: (AnalyticsInputType)(int)ie.Type
            )).ToList(),
            Metadata: new MatchMetadata(
                GameMode: recording.Metadata.GameMode,
                Stage: recording.Metadata.Stage,
                OnlineMatch: recording.Metadata.OnlineMatch,
                CustomData: recording.Metadata.CustomData
            )
        );
    }

    private Core.Mugen.Services.MatchPerformance ConvertToLegacyPerformance(PlayerMatchStats performance)
    {
        return new Core.Mugen.Services.MatchPerformance(
            PlayerId: performance.PlayerId,
            TotalDamageDealt: performance.TotalDamageDealt,
            TotalDamageReceived: performance.TotalDamageReceived,
            LongestCombo: performance.LongestCombo,
            SpecialMovesUsed: performance.SpecialMovesUsed,
            Accuracy: performance.Accuracy,
            Strengths: performance.Strengths,
            Weaknesses: performance.Weaknesses
        );
    }

    private Core.Mugen.Services.PlayerStatistics ConvertToLegacyStatistics(PlayerStatistics stats)
    {
        return new Core.Mugen.Services.PlayerStatistics(
            PlayerId: stats.PlayerId,
            TotalMatches: stats.TotalMatches,
            Wins: stats.Wins,
            Losses: stats.Losses,
            WinRate: stats.WinRate,
            CharacterStats: stats.CharacterStats.ToDictionary(
                kvp => kvp.Key,
                kvp => new Core.Mugen.Services.CharacterAnalyticsStats(
                    CharacterName: kvp.Value.CharacterName,
                    MatchesPlayed: kvp.Value.MatchesPlayed,
                    Wins: kvp.Value.Wins,
                    Losses: kvp.Value.Losses,
                    WinRate: kvp.Value.WinRate,
                    TotalDamageDealt: kvp.Value.TotalDamageDealt,
                    AverageComboLength: (int)kvp.Value.AverageComboLength,
                    MostUsedMoves: kvp.Value.MostUsedMoves
                )),
            Achievements: stats.Achievements.Select(a => new Core.Mugen.Services.Achievement(
                Name: a.Name,
                Description: a.Description,
                UnlockedAt: a.UnlockedAt,
                Rarity: (Core.Mugen.Services.AchievementRarity)(int)a.Rarity
            )).ToList(),
            Ranking: new Core.Mugen.Services.PlayerRanking(
                GlobalRank: stats.Ranking.GlobalRank,
                RegionalRank: stats.Ranking.RegionalRank,
                Rating: stats.Ranking.Rating,
                Tier: stats.Ranking.Tier,
                RankedStats: stats.Ranking.RankedStats.Select(rs => new Core.Mugen.Services.RankedStats(
                    GameMode: rs.GameMode,
                    Rank: rs.Rank,
                    Rating: rs.Rating,
                    Wins: rs.Wins,
                    Losses: rs.Losses,
                    WinRate: rs.WinRate
                )).ToList()
            )
        );
    }

    private Core.Mugen.Services.PlayerPattern ConvertToLegacyPattern(DetectedPattern pattern)
    {
        return new Core.Mugen.Services.PlayerPattern(
            PatternType: pattern.Name,
            Description: pattern.Description,
            Frequency: pattern.Frequency,
            AssociatedMoves: pattern.AssociatedMoves,
            Impact: pattern.Impact
        );
    }

    private Core.Mugen.Services.PerformanceTrends ConvertToLegacyTrends(PerformanceTrends trends)
    {
        return new Core.Mugen.Services.PerformanceTrends(
            PlayerId: trends.PlayerId,
            StartDate: trends.StartDate,
            EndDate: trends.EndDate,
            WinRateTrend: trends.WinRateTrend.Select(t => new Core.Mugen.Services.TrendDataPoint(
                Date: t.Date,
                Value: t.Value,
                Context: t.Context
            )).ToList(),
            DamageTrend: trends.DamageTrend.Select(t => new Core.Mugen.Services.TrendDataPoint(
                Date: t.Date,
                Value: t.Value,
                Context: t.Context
            )).ToList(),
            ComboTrend: trends.ComboTrend.Select(t => new Core.Mugen.Services.TrendDataPoint(
                Date: t.Date,
                Value: t.Value,
                Context: t.Context
            )).ToList(),
            NotableChanges: trends.NotableChanges
        );
    }

    private PerformanceTrends ConvertFromLegacyTrends(Core.Mugen.Services.PerformanceTrends trends)
    {
        return new PerformanceTrends(
            PlayerId: trends.PlayerId,
            StartDate: trends.StartDate,
            EndDate: trends.EndDate,
            WinRateTrend: trends.WinRateTrend.Select(t => new TrendPoint(
                Date: t.Date,
                Value: t.Value,
                Context: t.Context
            )).ToList(),
            DamageTrend: trends.DamageTrend.Select(t => new TrendPoint(
                Date: t.Date,
                Value: t.Value,
                Context: t.Context
            )).ToList(),
            ComboTrend: trends.ComboTrend.Select(t => new TrendPoint(
                Date: t.Date,
                Value: t.Value,
                Context: t.Context
            )).ToList(),
            NotableChanges: trends.NotableChanges
        );
    }

    private Core.Mugen.Services.ImprovementRecommendation ConvertToLegacyRecommendation(ImprovementRecommendation rec)
    {
        return new Core.Mugen.Services.ImprovementRecommendation(
            Category: rec.Category,
            Recommendation: rec.Recommendation,
            Rationale: rec.Rationale,
            Priority: (Core.Mugen.Services.RecommendationPriority)(int)rec.Priority,
            ActionSteps: rec.ActionSteps
        );
    }

    #endregion
}
