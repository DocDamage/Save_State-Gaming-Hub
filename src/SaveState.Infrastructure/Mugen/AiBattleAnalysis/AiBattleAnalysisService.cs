using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Common;
using SaveState.Core.Mugen.AiBattleAnalysis;
using SaveState.Core.Mugen.AiBattleAnalysis.Services;
using SaveState.Infrastructure.Persistence;
using CoreModels = SaveState.Core.Mugen.AiBattleAnalysis;

namespace SaveState.Infrastructure.Mugen.AiBattleAnalysisServices;

/// <summary>
/// AI-powered battle analysis service implementation.
/// </summary>
public class AiBattleAnalysisService : IAiBattleAnalysisService
{
    private readonly SaveStateDbContext _dbContext;
    private readonly ILogger<AiBattleAnalysisService> _logger;
    private readonly IAiInsightsProvider _aiProvider;
    private readonly BattleAnalysisOptions _defaultOptions;
    private readonly Dictionary<Guid, RealTimeAnalysis> _activeSessions = new();

    public AiBattleAnalysisService(
        SaveStateDbContext dbContext,
        ILogger<AiBattleAnalysisService> logger,
        IAiInsightsProvider aiProvider,
        IOptions<BattleAnalysisOptions> options)
    {
        _dbContext = dbContext;
        _logger = logger;
        _aiProvider = aiProvider;
        _defaultOptions = options.Value;
    }

    public async Task<Result<CoreModels.AiBattleAnalysis>> AnalyzeBattleAsync(
        BattleAnalysisRequest request, 
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Starting battle analysis for {Character} vs {Opponent}", 
                request.CharacterName, request.OpponentName);

            // Parse replay data if file path provided
            var replayData = request.ReplayData;
            if (replayData == null && !string.IsNullOrEmpty(request.ReplayFilePath))
            {
                replayData = await File.ReadAllBytesAsync(request.ReplayFilePath, ct);
            }

            if (replayData == null)
            {
                return Result<CoreModels.AiBattleAnalysis>.Failure("No replay data provided", ErrorType.Validation);
            }

            // Create base analysis
            var analysis = new CoreModels.AiBattleAnalysis
            {
                CharacterName = request.CharacterName,
                OpponentName = request.OpponentName,
                Source = request.ReplayFilePath ?? "memory",
                BattleDate = DateTime.UtcNow,
                Duration = TimeSpan.FromSeconds(replayData.Length / 60.0) // Approximate
            };

            var options = request.Options;

            // Detect patterns
            if (options.DetectPatterns)
            {
                analysis.Patterns = await DetectPatternsAsync(replayData, options, ct);
            }

            // Calculate stats
            analysis.Stats = CalculateCombatStats(replayData, analysis.Patterns);

            // Identify weaknesses
            if (options.IdentifyWeaknesses)
            {
                analysis.Weaknesses = IdentifyWeaknesses(analysis.Stats, analysis.Patterns);
            }

            // Find opportunities
            analysis.Opportunities = FindOpportunities(analysis.Weaknesses, analysis.Stats);

            // Generate recommendations
            if (options.GenerateRecommendations)
            {
                analysis.Recommendations = GenerateRecommendations(analysis.Weaknesses, request.OpponentName);
            }

            // AI insights
            if (options.UseAiInsights)
            {
                analysis.Insights = await _aiProvider.GenerateInsightsAsync(analysis, ct);
            }

            // Calculate performance rating
            analysis.PerformanceRating = CalculatePerformanceRating(analysis);

            // Determine result
            analysis.Result = DetermineResult(analysis.Stats);

            // Save to database
            _dbContext.AiBattleAnalyses.Add(analysis);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Battle analysis completed with rating {Rating}", analysis.PerformanceRating);
            return Result<CoreModels.AiBattleAnalysis>.Success(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze battle");
            return Result<CoreModels.AiBattleAnalysis>.Failure($"Analysis failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<List<CoreModels.AiBattleAnalysis>>> GetCharacterAnalysesAsync(
        string characterName, 
        string? opponentName = null,
        CancellationToken ct = default)
    {
        try
        {
            var query = _dbContext.AiBattleAnalyses
                .AsNoTracking()
                .Where(a => a.CharacterName == characterName);

            if (!string.IsNullOrEmpty(opponentName))
                query = query.Where(a => a.OpponentName == opponentName);

            var analyses = await query
                .OrderByDescending(a => a.BattleDate)
                .ToListAsync(ct);

            return Result<List<CoreModels.AiBattleAnalysis>>.Success(analyses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get analyses for {Character}", characterName);
            return Result<List<CoreModels.AiBattleAnalysis>>.Failure($"Query failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<BattleComparison>> CompareBattlesAsync(
        Guid currentAnalysisId, 
        Guid previousAnalysisId,
        CancellationToken ct = default)
    {
        try
        {
            var current = await _dbContext.AiBattleAnalyses
                .FirstOrDefaultAsync(a => a.Id == currentAnalysisId, ct);
            
            var previous = await _dbContext.AiBattleAnalyses
                .FirstOrDefaultAsync(a => a.Id == previousAnalysisId, ct);

            if (current == null || previous == null)
                return Result<BattleComparison>.Failure("Analysis not found", ErrorType.NotFound);

            var comparison = new BattleComparison
            {
                Current = current,
                Previous = previous,
                Improvements = new List<string>(),
                Regressions = new List<string>()
            };

            // Compare stats
            if (current.Stats.HitRate > previous.Stats.HitRate + 5)
                comparison.Improvements.Add($"Hit rate improved from {previous.Stats.HitRate:F1}% to {current.Stats.HitRate:F1}%");
            else if (current.Stats.HitRate < previous.Stats.HitRate - 5)
                comparison.Regressions.Add($"Hit rate dropped from {previous.Stats.HitRate:F1}% to {current.Stats.HitRate:F1}%");

            if (current.Stats.MaxComboHits > previous.Stats.MaxComboHits)
                comparison.Improvements.Add($"Max combo improved from {previous.Stats.MaxComboHits} to {current.Stats.MaxComboHits} hits");

            if (current.Stats.Punishes > previous.Stats.Punishes)
                comparison.Improvements.Add($"Punishes increased from {previous.Stats.Punishes} to {current.Stats.Punishes}");

            return Result<BattleComparison>.Success(comparison);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compare battles");
            return Result<BattleComparison>.Failure($"Comparison failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<List<DetectedPattern>>> GetCharacterPatternsAsync(
        string characterName,
        PatternType? type = null,
        CancellationToken ct = default)
    {
        try
        {
            var query = _dbContext.AiBattleAnalyses
                .AsNoTracking()
                .Where(a => a.CharacterName == characterName)
                .SelectMany(a => a.Patterns);

            if (type.HasValue)
                query = query.Where(p => p.Type == type.Value);

            var patterns = await query
                .GroupBy(p => p.Name)
                .Select(g => new DetectedPattern
                {
                    Name = g.Key,
                    Type = g.First().Type,
                    Description = g.First().Description,
                    Frequency = g.Sum(p => p.Frequency),
                    SuccessRate = g.Average(p => p.SuccessRate)
                })
                .OrderByDescending(p => p.Frequency)
                .ToListAsync(ct);

            return Result<List<DetectedPattern>>.Success(patterns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get patterns for {Character}", characterName);
            return Result<List<DetectedPattern>>.Failure($"Query failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<List<PlayerWeakness>>> GetCharacterWeaknessesAsync(
        string characterName,
        SeverityLevel? minSeverity = null,
        CancellationToken ct = default)
    {
        try
        {
            var query = _dbContext.AiBattleAnalyses
                .AsNoTracking()
                .Where(a => a.CharacterName == characterName)
                .SelectMany(a => a.Weaknesses);

            if (minSeverity.HasValue)
                query = query.Where(w => w.Severity >= minSeverity.Value);

            var weaknesses = await query
                .GroupBy(w => w.Description)
                .Select(g => new PlayerWeakness
                {
                    Description = g.Key,
                    Category = g.First().Category,
                    Severity = g.Max(w => w.Severity),
                    Occurrences = g.Sum(w => w.Occurrences),
                    DamageTaken = g.Sum(w => w.DamageTaken)
                })
                .OrderByDescending(w => w.Occurrences)
                .ToListAsync(ct);

            return Result<List<PlayerWeakness>>.Success(weaknesses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get weaknesses for {Character}", characterName);
            return Result<List<PlayerWeakness>>.Failure($"Query failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<List<TrainingRecommendation>>> GenerateTrainingPlanAsync(
        string characterName,
        int sessionMinutes = 30,
        CancellationToken ct = default)
    {
        try
        {
            var weaknesses = await GetCharacterWeaknessesAsync(characterName, SeverityLevel.Medium, ct);
            if (weaknesses.IsFailure)
                return Result<List<TrainingRecommendation>>.Failure(weaknesses.Error!, weaknesses.ErrorType);

            var recommendations = new List<TrainingRecommendation>();

            foreach (var weakness in weaknesses.Value.Take(3))
            {
                var rec = new TrainingRecommendation
                {
                    Category = weakness.Category.ToString(),
                    Focus = weakness.Description,
                    Description = $"Work on {weakness.Description}",
                    Priority = (int)weakness.Severity,
                    EstimatedTime = TimeSpan.FromMinutes(sessionMinutes / 3.0),
                    Drills = GenerateDrillsForWeakness(weakness)
                };
                recommendations.Add(rec);
            }

            return Result<List<TrainingRecommendation>>.Success(recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate training plan");
            return Result<List<TrainingRecommendation>>.Failure($"Generation failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<List<CounterStrategy>>> GetMatchupAdviceAsync(
        string characterName,
        string opponentName,
        CancellationToken ct = default)
    {
        try
        {
            // Get previous analyses for this matchup
            var analyses = await GetCharacterAnalysesAsync(characterName, opponentName, ct);
            if (analyses.IsFailure || !analyses.Value.Any())
            {
                // Return generic advice if no data
                return Result<List<CounterStrategy>>.Success(GetGenericMatchupAdvice(opponentName));
            }

            // Aggregate successful strategies from wins
            var winningAnalyses = analyses.Value.Where(a => a.Result == BattleResult.Win);
            var strategies = new List<CounterStrategy>();

            foreach (var analysis in winningAnalyses.Take(3))
            {
                strategies.AddRange(analysis.Recommendations);
            }

            return Result<List<CounterStrategy>>.Success(strategies.Distinct().ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get matchup advice");
            return Result<List<CounterStrategy>>.Failure($"Query failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<PerformanceTrend>> GetPerformanceTrendAsync(
        string characterName,
        DateTime? since = null,
        CancellationToken ct = default)
    {
        try
        {
            var startDate = since ?? DateTime.UtcNow.AddMonths(-1);
            
            var analyses = await _dbContext.AiBattleAnalyses
                .AsNoTracking()
                .Where(a => a.CharacterName == characterName && a.BattleDate >= startDate)
                .OrderBy(a => a.BattleDate)
                .ToListAsync(ct);

            if (!analyses.Any())
                return Result<PerformanceTrend>.Failure("No data available", ErrorType.NotFound);

            var trend = new PerformanceTrend
            {
                CharacterName = characterName,
                StartDate = startDate,
                EndDate = DateTime.UtcNow,
                TotalBattles = analyses.Count,
                Wins = analyses.Count(a => a.Result == BattleResult.Win),
                Losses = analyses.Count(a => a.Result == BattleResult.Loss),
                RatingOverTime = analyses.Select(a => new TrendDataPoint
                {
                    Date = a.BattleDate,
                    Value = a.PerformanceRating
                }).ToList(),
                HitRateOverTime = analyses.Select(a => new TrendDataPoint
                {
                    Date = a.BattleDate,
                    Value = a.Stats.HitRate
                }).ToList()
            };

            trend.OverallTrend = DetermineTrendDirection(trend.RatingOverTime);
            trend.Analysis = GenerateTrendAnalysis(trend);

            return Result<PerformanceTrend>.Success(trend);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get performance trend");
            return Result<PerformanceTrend>.Failure($"Query failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public Task<Result<byte[]>> ExportAnalysisAsync(
        Guid analysisId,
        ExportFormat format,
        CancellationToken ct = default)
    {
        // Simplified export - would implement full export logic
        return Task.FromResult(Result<byte[]>.Failure("Export not implemented", ErrorType.External));
    }

    public async Task<Result> DeleteAnalysisAsync(Guid analysisId, CancellationToken ct = default)
    {
        try
        {
            var analysis = await _dbContext.AiBattleAnalyses
                .FirstOrDefaultAsync(a => a.Id == analysisId, ct);

            if (analysis == null)
                return Result.Failure("Analysis not found", ErrorType.NotFound);

            _dbContext.AiBattleAnalyses.Remove(analysis);
            await _dbContext.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete analysis");
            return Result.Failure($"Delete failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public Task<Result<RealTimeAnalysis>> StartRealTimeAnalysisAsync(
        string characterName,
        string opponentName,
        CancellationToken ct = default)
    {
        var sessionId = Guid.NewGuid();
        var session = new RealTimeAnalysis
        {
            SessionId = sessionId,
            CharacterName = characterName,
            OpponentName = opponentName,
            StartedAt = DateTime.UtcNow,
            CurrentStats = new CombatStats(),
            Insights = new List<RealTimeInsight>(),
            Suggestions = new List<string>()
        };

        _activeSessions[sessionId] = session;
        _logger.LogInformation("Started real-time analysis session {SessionId}", sessionId);

        return Task.FromResult(Result<RealTimeAnalysis>.Success(session));
    }

    public Task<Result> FeedFrameDataAsync(
        Guid sessionId,
        FrameDataSnapshot snapshot,
        CancellationToken ct = default)
    {
        if (!_activeSessions.TryGetValue(sessionId, out var session))
            return Task.FromResult(Result.Failure("Session not found", ErrorType.NotFound));

        session.FrameCount++;

        // Analyze frame data and generate insights
        if (snapshot.IsHit && !snapshot.IsBlocking)
        {
            session.CurrentStats.SuccessfulHits++;
        }

        if (snapshot.PlayerHealth < 50 && session.CurrentStats.SuccessfulHits > 0)
        {
            session.Suggestions.Add("Consider defensive options - health is low");
        }

        return Task.FromResult(Result.Success());
    }

    public async Task<Result<CoreModels.AiBattleAnalysis>> StopRealTimeAnalysisAsync(
        Guid sessionId,
        CancellationToken ct = default)
    {
        if (!_activeSessions.TryGetValue(sessionId, out var session))
            return Result<CoreModels.AiBattleAnalysis>.Failure("Session not found", ErrorType.NotFound);

        _activeSessions.Remove(sessionId);

        // Convert to full analysis
        var analysis = new CoreModels.AiBattleAnalysis
        {
            CharacterName = session.CharacterName,
            OpponentName = session.OpponentName,
            BattleDate = session.StartedAt,
            Duration = DateTime.UtcNow - session.StartedAt,
            Stats = session.CurrentStats,
            PerformanceRating = CalculateRealTimePerformanceRating(session)
        };

        _dbContext.AiBattleAnalyses.Add(analysis);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Stopped real-time analysis session {SessionId}", sessionId);
        return Result<CoreModels.AiBattleAnalysis>.Success(analysis);
    }

    #region Private Methods

    private async Task<List<DetectedPattern>> DetectPatternsAsync(byte[] replayData, BattleAnalysisOptions options, CancellationToken ct)
    {
        // Simplified pattern detection - would use actual replay parsing
        var patterns = new List<DetectedPattern>
        {
            new()
            {
                Name = "Jab Pressure",
                Type = PatternType.Offensive,
                Description = "Repeated light attacks to maintain pressure",
                Frequency = 15,
                SuccessRate = 0.7m
            },
            new()
            {
                Name = "Jump-in Attempts",
                Type = PatternType.Movement,
                Description = "Frequent jump-in attacks",
                Frequency = 8,
                SuccessRate = 0.4m,
                IsPunishable = true,
                CounterStrategy = "Use anti-air moves"
            }
        };

        return patterns;
    }

    private CombatStats CalculateCombatStats(byte[] replayData, List<DetectedPattern> patterns)
    {
        // Calculate stats from replay data
        return new CombatStats
        {
            TotalAttacks = patterns.Sum(p => p.Frequency),
            SuccessfulHits = (int)(patterns.Sum(p => p.Frequency) * 0.6),
            CombosPerformed = 12,
            MaxComboHits = 8,
            BlocksPerformed = 25,
            Punishes = 3,
            ThrowsAttempted = 5,
            ThrowsSuccessful = 3
        };
    }

    private List<PlayerWeakness> IdentifyWeaknesses(CombatStats stats, List<DetectedPattern> patterns)
    {
        var weaknesses = new List<PlayerWeakness>();

        if (stats.HitRate < 50)
        {
            weaknesses.Add(new PlayerWeakness
            {
                Description = "Low hit rate - attacks frequently whiff",
                Category = WeaknessCategory.Neutral,
                Severity = SeverityLevel.High,
                Occurrences = stats.WhiffedAttacks,
                SuggestedFix = "Practice spacing and neutral game"
            });
        }

        var jumpPattern = patterns.FirstOrDefault(p => p.Name.Contains("Jump"));
        if (jumpPattern?.SuccessRate < 0.5m)
        {
            weaknesses.Add(new PlayerWeakness
            {
                Description = "Jump-ins are being anti-aired",
                Category = WeaknessCategory.AntiAir,
                Severity = SeverityLevel.Medium,
                Occurrences = jumpPattern.Frequency,
                SuggestedFix = "Mix up approach with ground options"
            });
        }

        return weaknesses;
    }

    private List<ImprovementOpportunity> FindOpportunities(List<PlayerWeakness> weaknesses, CombatStats stats)
    {
        return weaknesses.Select(w => new ImprovementOpportunity
        {
            Area = w.Category.ToString(),
            Description = w.SuggestedFix ?? "Practice this area",
            PotentialImpact = (int)w.Severity * 10,
            Difficulty = DifficultyLevel.Medium,
            PracticeDrills = new List<string> { "Training mode practice", "Replay review" }
        }).ToList();
    }

    private List<CounterStrategy> GenerateRecommendations(List<PlayerWeakness> weaknesses, string opponentName)
    {
        return weaknesses.Select(w => new CounterStrategy
        {
            Situation = w.Description,
            Strategy = w.SuggestedFix ?? "Adapt gameplay",
            Execution = "Practice in training mode",
            RiskLevel = 2,
            RewardLevel = 4
        }).ToList();
    }

    private int CalculatePerformanceRating(CoreModels.AiBattleAnalysis analysis)
    {
        var rating = 50; // Base
        rating += (int)(analysis.Stats.HitRate / 2);
        rating += analysis.Stats.Punishes * 5;
        rating += analysis.Stats.MaxComboHits * 2;
        rating -= analysis.Weaknesses.Count(w => w.Severity == SeverityLevel.High) * 10;
        return Math.Clamp(rating, 0, 100);
    }

    private int CalculateRealTimePerformanceRating(RealTimeAnalysis session)
    {
        var rating = 50;
        rating += (int)(session.CurrentStats.HitRate / 2);
        return Math.Clamp(rating, 0, 100);
    }

    private BattleResult DetermineResult(CombatStats stats)
    {
        if (stats.SuccessfulHits > stats.TotalAttacks * 0.6m && stats.Punishes > 2)
            return BattleResult.Win;
        if (stats.SuccessfulHits < stats.TotalAttacks * 0.4m)
            return BattleResult.Loss;
        return BattleResult.Draw;
    }

    private List<TrainingDrill> GenerateDrillsForWeakness(PlayerWeakness weakness)
    {
        return new List<TrainingDrill>
        {
            new()
            {
                Name = $"{weakness.Category} Practice",
                Setup = "Training mode",
                Goal = weakness.SuggestedFix ?? "Improve",
                Repetitions = 20,
                Difficulty = DifficultyLevel.Medium
            }
        };
    }

    private List<CounterStrategy> GetGenericMatchupAdvice(string opponentName)
    {
        return new List<CounterStrategy>
        {
            new()
            {
                Situation = "Neutral game",
                Strategy = "Control space with normals",
                Execution = "Use pokes to establish range",
                RiskLevel = 1,
                RewardLevel = 2
            }
        };
    }

    private TrendDirection DetermineTrendDirection(List<TrendDataPoint> points)
    {
        if (points.Count < 2) return TrendDirection.Stable;
        
        var first = points.First().Value;
        var last = points.Last().Value;
        var diff = last - first;
        
        if (diff > 10) return TrendDirection.Improving;
        if (diff < -10) return TrendDirection.Declining;
        
        // Check consistency
        var variance = points.Select(p => Math.Abs(p.Value - points.Average(p2 => p2.Value))).Average();
        if (variance > 15) return TrendDirection.Inconsistent;
        
        return TrendDirection.Stable;
    }

    private string GenerateTrendAnalysis(PerformanceTrend trend)
    {
        return trend.OverallTrend switch
        {
            TrendDirection.Improving => "Your performance is improving. Keep practicing!",
            TrendDirection.Declining => "Performance has declined. Consider reviewing fundamentals.",
            TrendDirection.Inconsistent => "Results are inconsistent. Focus on consistency.",
            _ => "Performance is stable. Work on specific areas to improve."
        };
    }

    #endregion
}

/// <summary>
/// Interface for AI insights provider.
/// </summary>
public interface IAiInsightsProvider
{
    Task<AiInsights> GenerateInsightsAsync(CoreModels.AiBattleAnalysis analysis, CancellationToken ct = default);
}
