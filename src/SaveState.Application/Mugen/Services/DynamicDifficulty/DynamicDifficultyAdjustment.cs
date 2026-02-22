using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Advanced dynamic difficulty adjustment system using AI to adapt opponent
/// behavior and challenge level based on real-time player performance analysis.
/// </summary>
public class DynamicDifficultyAdjustment : IDynamicDifficultyAdjustment
{
    private readonly ILogger<DynamicDifficultyAdjustment> _logger;
    private readonly ICacheService _cache;
    private readonly ITimeProvider _timeProvider;
    private readonly DdaPerformanceMonitor _performanceMonitor;
    private readonly DdaDifficultyAdapter _difficultyAdapter;
    private readonly DdaBehaviorModulator _behaviorModulator;
    private readonly DdaLearningSystem _learningSystem;

    public DynamicDifficultyAdjustment(
        ILogger<DynamicDifficultyAdjustment> logger,
        ICacheService cache,
        ITimeProvider timeProvider,
        DdaPerformanceMonitor performanceMonitor,
        DdaDifficultyAdapter difficultyAdapter,
        DdaBehaviorModulator behaviorModulator,
        DdaLearningSystem learningSystem)
    {
        _logger = logger;
        _cache = cache;
        _timeProvider = timeProvider;
        _performanceMonitor = performanceMonitor;
        _difficultyAdapter = difficultyAdapter;
        _behaviorModulator = behaviorModulator;
        _learningSystem = learningSystem;
    }

    public async Task<Result<DdaDifficultyProfile>> CreateDifficultyProfileAsync(DdaDifficultyProfileRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating difficulty profile for player {PlayerId}", request.PlayerId);
            var historical = await AnalyzeHistoricalPerformanceAsync(request.PlayerId, ct);
            var profile = new DdaDifficultyProfile
            {
                ProfileId = Guid.NewGuid().ToString(),
                PlayerId = request.PlayerId,
                BaseDifficulty = request.BaseDifficulty,
                AdaptiveSettings = GenerateAdaptiveSettings(historical),
                BehaviorParameters = GenerateBehaviorParameters(historical),
                PerformanceThresholds = GeneratePerformanceThresholds(historical),
                AdaptationRules = GenerateAdaptationRules(historical),
                LearningEnabled = true,
                CreatedAt = _timeProvider.UtcNow,
                LastUpdated = _timeProvider.UtcNow
            };
            var cacheKey = $"dda_profile_{request.PlayerId}";
            await _cache.SetAsync(cacheKey, profile, TimeSpan.FromHours(24), ct);
            _logger.LogInformation("Difficulty profile created: {ProfileId}", profile.ProfileId);
            return Result.Success(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating difficulty profile for {PlayerId}", request.PlayerId);
            return Result.Failure<DdaDifficultyProfile>($"Failed to create profile: {ex.Message}");
        }
    }

    public async Task<Result<DdaDifficultyAdjustment>> CalculateAdjustmentAsync(string playerId, DdaMatchState matchState, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Calculating difficulty adjustment for player {PlayerId}", playerId);
            var profileResult = await GetDifficultyProfileAsync(playerId, ct);
            if (!profileResult.IsSuccess) return Result.Failure<DdaDifficultyAdjustment>(profileResult.Error);
            var currentPerformance = await _performanceMonitor.AnalyzeCurrentPerformanceAsync(matchState, ct);
            var adjustment = await _difficultyAdapter.CalculateAdjustmentAsync(profileResult.Value, currentPerformance, ct);
            await UpdateProfileWithLearningAsync(profileResult.Value, currentPerformance, adjustment, ct);
            _logger.LogInformation("Difficulty adjustment calculated: {Adjustment} for player {PlayerId}",
                adjustment.AdjustmentType, playerId);
            return Result.Success(adjustment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating adjustment for {PlayerId}", playerId);
            return Result.Failure<DdaDifficultyAdjustment>($"Failed to calculate adjustment: {ex.Message}");
        }
    }

    public async Task<Result<DdaOpponentBehavior>> GenerateOpponentBehaviorAsync(string playerId, DdaDifficultyAdjustment adjustment, DdaMatchState matchState, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating opponent behavior for player {PlayerId}", playerId);
            var profileResult = await GetDifficultyProfileAsync(playerId, ct);
            if (!profileResult.IsSuccess) return Result.Failure<DdaOpponentBehavior>(profileResult.Error);
            var behavior = await _behaviorModulator.GenerateBehaviorAsync(profileResult.Value, adjustment, matchState, ct);
            _logger.LogInformation("Opponent behavior generated with aggression {Aggression:F2} for player {PlayerId}",
                behavior.AggressionLevel, playerId);
            return Result.Success(behavior);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating opponent behavior for {PlayerId}", playerId);
            return Result.Failure<DdaOpponentBehavior>($"Failed to generate behavior: {ex.Message}");
        }
    }

    public async Task<Result<DdaAdaptationMetrics>> GetAdaptationMetricsAsync(string playerId, TimeSpan period, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting adaptation metrics for player {PlayerId}", playerId);
            var metrics = await _performanceMonitor.GetAdaptationMetricsAsync(playerId, period, ct);
            var result = new DdaAdaptationMetrics
            {
                PlayerId = playerId,
                Period = period,
                DifficultyAdjustments = metrics.DifficultyAdjustments,
                PerformanceTrend = metrics.PerformanceTrend,
                AdaptationEffectiveness = metrics.AdaptationEffectiveness,
                LearningProgress = metrics.LearningProgress,
                OptimalDifficulty = metrics.OptimalDifficulty,
                GeneratedAt = _timeProvider.UtcNow
            };
            _logger.LogInformation("Adaptation metrics retrieved for {PlayerId}: Effectiveness {Effectiveness:F2}",
                playerId, result.AdaptationEffectiveness);
            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting adaptation metrics for {PlayerId}", playerId);
            return Result.Failure<DdaAdaptationMetrics>($"Failed to get metrics: {ex.Message}");
        }
    }

    public async Task<Result> TrainDifficultyModelAsync(IReadOnlyList<DdaTrainingMatch> trainingMatches, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Training difficulty model with {Count} matches", trainingMatches.Count);
            await _learningSystem.TrainModelAsync(trainingMatches, ct);
            _logger.LogInformation("Difficulty model training completed");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error training difficulty model");
            return Result.Failure($"Training failed: {ex.Message}");
        }
    }

    public async Task<Result<DdaChallengeCalibration>> CalibrateChallengeAsync(string playerId, DdaCalibrationRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Calibrating challenge for player {PlayerId}", playerId);
            var calibrationData = await AnalyzeCalibrationDataAsync(playerId, request, ct);
            var calibration = new DdaChallengeCalibration
            {
                PlayerId = playerId,
                OptimalDifficulty = calibrationData.OptimalDifficulty,
                RecommendedSettings = calibrationData.RecommendedSettings,
                PerformanceZones = calibrationData.PerformanceZones,
                AdaptationSensitivity = calibrationData.AdaptationSensitivity,
                ChallengeCurve = calibrationData.ChallengeCurve,
                ConfidenceLevel = calibrationData.ConfidenceLevel,
                LastCalibrated = _timeProvider.UtcNow
            };
            _logger.LogInformation("Challenge calibrated for {PlayerId}: Optimal difficulty {Difficulty}",
                playerId, calibration.OptimalDifficulty);
            return Result.Success(calibration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calibrating challenge for {PlayerId}", playerId);
            return Result.Failure<DdaChallengeCalibration>($"Calibration failed: {ex.Message}");
        }
    }

    public async Task<Result<DdaDifficultyReport>> GenerateDifficultyReportAsync(string playerId, TimeSpan period, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating difficulty report for player {PlayerId}", playerId);
            var profileResult = await GetDifficultyProfileAsync(playerId, ct);
            var metricsResult = await GetAdaptationMetricsAsync(playerId, period, ct);
            if (!profileResult.IsSuccess || !metricsResult.IsSuccess)
                return Result.Failure<DdaDifficultyReport>("Unable to retrieve profile or metrics data");
            var report = new DdaDifficultyReport
            {
                PlayerId = playerId,
                ReportPeriod = period,
                CurrentProfile = profileResult.Value,
                AdaptationMetrics = metricsResult.Value,
                PerformanceAnalysis = await GeneratePerformanceAnalysisAsync(playerId, period, ct),
                Recommendations = await GenerateRecommendationsAsync(profileResult.Value, metricsResult.Value, ct),
                TrendAnalysis = await AnalyzeTrendsAsync(playerId, period, ct),
                GeneratedAt = _timeProvider.UtcNow
            };
            _logger.LogInformation("Difficulty report generated for {PlayerId}", playerId);
            return Result.Success(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating difficulty report for {PlayerId}", playerId);
            return Result.Failure<DdaDifficultyReport>($"Report generation failed: {ex.Message}");
        }
    }

    #region Private Methods

    private async Task<DdaHistoricalPerformanceData> AnalyzeHistoricalPerformanceAsync(string playerId, CancellationToken ct)
    {
        return new DdaHistoricalPerformanceData
        {
            AverageWinRate = 0.55,
            SkillProgression = DdaSkillTrend.Improving,
            PreferredDifficulty = DdaDifficultyLevel.Medium,
            ConsistencyRating = 0.75,
            Strengths = new[] { "Good defense", "Strong combos" },
            Weaknesses = new[] { "Projectile defense", "Anti-air timing" },
            LearningRate = 0.8
        };
    }

    private DdaAdaptiveSettings GenerateAdaptiveSettings(DdaHistoricalPerformanceData historical)
    {
        return new DdaAdaptiveSettings
        {
            BaseAdjustmentRate = 0.1 * historical.LearningRate,
            MaximumAdjustment = 2.0,
            MinimumAdjustment = 0.5,
            AdaptationCooldown = TimeSpan.FromSeconds(30),
            PerformanceWindow = TimeSpan.FromMinutes(5),
            ResetThreshold = 0.3
        };
    }

    private DdaBehaviorParameters GenerateBehaviorParameters(DdaHistoricalPerformanceData historical)
    {
        return new DdaBehaviorParameters
        {
            AggressionBase = historical.AverageWinRate > 0.6 ? 0.7 : 0.5,
            DefensePriority = historical.Strengths.Contains("Good defense") ? 0.8 : 0.4,
            RiskTolerance = historical.ConsistencyRating,
            AdaptationSpeed = historical.LearningRate,
            PatternRecognition = 0.85
        };
    }

    private DdaPerformanceThresholds GeneratePerformanceThresholds(DdaHistoricalPerformanceData historical)
    {
        return new DdaPerformanceThresholds
        {
            WinRateIncreaseThreshold = 0.1,
            WinRateDecreaseThreshold = -0.1,
            ComboSuccessThreshold = 0.6,
            DamageEfficiencyThreshold = 0.7,
            ResourceManagementThreshold = 0.5,
            TimingAccuracyThreshold = 0.65
        };
    }

    private IReadOnlyList<DdaAdaptationRule> GenerateAdaptationRules(DdaHistoricalPerformanceData historical)
    {
        return new List<DdaAdaptationRule>
        {
            new() { Condition = "Player struggling with projectiles", Action = "Increase projectile speed and reduce frequency", Priority = 8, Cooldown = TimeSpan.FromMinutes(2) },
            new() { Condition = "Player performing well in neutral", Action = "Introduce more aggressive pressure patterns", Priority = 7, Cooldown = TimeSpan.FromMinutes(3) },
            new() { Condition = "Player showing improved defense", Action = "Gradually increase attack complexity", Priority = 6, Cooldown = TimeSpan.FromMinutes(5) }
        };
    }

    private async Task<Result<DdaDifficultyProfile>> GetDifficultyProfileAsync(string playerId, CancellationToken ct)
    {
        var cacheKey = $"dda_profile_{playerId}";
        var cached = await _cache.GetAsync<DdaDifficultyProfile>(cacheKey);
        if (cached != null) return Result.Success(cached);
        var request = new DdaDifficultyProfileRequest(playerId, DdaDifficultyLevel.Medium);
        return await CreateDifficultyProfileAsync(request, ct);
    }

    private async Task UpdateProfileWithLearningAsync(DdaDifficultyProfile profile, DdaCurrentPerformanceData performance, DdaDifficultyAdjustment adjustment, CancellationToken ct)
    {
        profile.LastUpdated = _timeProvider.UtcNow;
        if (performance.WinRate > 0.7 && adjustment.AdjustmentType == DdaDifficultyAdjustmentType.Increase)
            profile.BaseDifficulty = (DdaDifficultyLevel)Math.Min((int)DdaDifficultyLevel.VeryHard, (int)profile.BaseDifficulty + 1);
        else if (performance.WinRate < 0.3 && adjustment.AdjustmentType == DdaDifficultyAdjustmentType.Decrease)
            profile.BaseDifficulty = (DdaDifficultyLevel)Math.Max((int)DdaDifficultyLevel.VeryEasy, (int)profile.BaseDifficulty - 1);
        var cacheKey = $"dda_profile_{profile.PlayerId}";
        await _cache.SetAsync(cacheKey, profile, TimeSpan.FromHours(24), ct);
    }

    private async Task<DdaCalibrationData> AnalyzeCalibrationDataAsync(string playerId, DdaCalibrationRequest request, CancellationToken ct)
    {
        return new DdaCalibrationData
        {
            OptimalDifficulty = DdaDifficultyLevel.Medium,
            RecommendedSettings = new DdaAdaptiveSettings { BaseAdjustmentRate = 0.15 },
            PerformanceZones = new Dictionary<DdaDifficultyLevel, DdaPerformanceZone>
            {
                [DdaDifficultyLevel.Easy] = new() { WinRate = 0.85, Engagement = 0.7 },
                [DdaDifficultyLevel.Medium] = new() { WinRate = 0.65, Engagement = 0.9 },
                [DdaDifficultyLevel.Hard] = new() { WinRate = 0.45, Engagement = 0.95 }
            },
            AdaptationSensitivity = 0.8,
            ChallengeCurve = new[] { 0.3, 0.6, 0.8, 0.9, 0.95 },
            ConfidenceLevel = 0.85
        };
    }

    private async Task<DdaPerformanceAnalysis> GeneratePerformanceAnalysisAsync(string playerId, TimeSpan period, CancellationToken ct)
    {
        return new DdaPerformanceAnalysis
        {
            OverallWinRate = 0.58,
            PeakPerformance = 0.75,
            AverageMatchLength = TimeSpan.FromMinutes(4.2),
            MostUsedTechniques = new[] { "Fireball", "Uppercut", "Combo" },
            LearningVelocity = 0.12,
            AdaptationResistance = 0.3
        };
    }

    private async Task<IReadOnlyList<string>> GenerateRecommendationsAsync(DdaDifficultyProfile profile, DdaAdaptationMetrics metrics, CancellationToken ct)
    {
        var recommendations = new List<string>();
        if (metrics.AdaptationEffectiveness < 0.6)
            recommendations.Add("Consider reducing adaptation sensitivity to prevent over-correction");
        if (metrics.PerformanceTrend == DdaSkillTrend.Improving && profile.BaseDifficulty == DdaDifficultyLevel.Easy)
            recommendations.Add("Player is improving rapidly - consider gradual difficulty increase");
        if (metrics.LearningProgress > 0.8)
            recommendations.Add("Player has adapted well to current difficulty - ready for challenge increase");
        return recommendations;
    }

    private async Task<DdaTrendAnalysis> AnalyzeTrendsAsync(string playerId, TimeSpan period, CancellationToken ct)
    {
        return new DdaTrendAnalysis
        {
            DifficultyProgression = new[] { DdaDifficultyLevel.Easy, DdaDifficultyLevel.Medium, DdaDifficultyLevel.Medium },
            PerformanceCorrelation = 0.75,
            AdaptationPatterns = new[] { "Gradual increase", "Plateau periods", "Sudden improvements" },
            OptimalChallengePoints = new[] { TimeSpan.FromDays(7), TimeSpan.FromDays(14) },
            BurnoutIndicators = new[] { "Declining win rate", "Increased match length" }
        };
    }

    #endregion
}

/// <summary>
/// Dynamic Difficulty Adjustment interface.
/// </summary>
public interface IDynamicDifficultyAdjustment
{
    Task<Result<DdaDifficultyProfile>> CreateDifficultyProfileAsync(DdaDifficultyProfileRequest request, CancellationToken ct = default);
    Task<Result<DdaDifficultyAdjustment>> CalculateAdjustmentAsync(string playerId, DdaMatchState matchState, CancellationToken ct = default);
    Task<Result<DdaOpponentBehavior>> GenerateOpponentBehaviorAsync(string playerId, DdaDifficultyAdjustment adjustment, DdaMatchState matchState, CancellationToken ct = default);
    Task<Result<DdaAdaptationMetrics>> GetAdaptationMetricsAsync(string playerId, TimeSpan period, CancellationToken ct = default);
    Task<Result> TrainDifficultyModelAsync(IReadOnlyList<DdaTrainingMatch> trainingMatches, CancellationToken ct = default);
    Task<Result<DdaChallengeCalibration>> CalibrateChallengeAsync(string playerId, DdaCalibrationRequest request, CancellationToken ct = default);
    Task<Result<DdaDifficultyReport>> GenerateDifficultyReportAsync(string playerId, TimeSpan period, CancellationToken ct = default);
}

// Types

public class DdaDifficultyProfile
{
    public string ProfileId { get; set; } = default!;
    public string PlayerId { get; set; } = default!;
    public DdaDifficultyLevel BaseDifficulty { get; set; }
    public DdaAdaptiveSettings AdaptiveSettings { get; set; } = default!;
    public DdaBehaviorParameters BehaviorParameters { get; set; } = default!;
    public DdaPerformanceThresholds PerformanceThresholds { get; set; } = default!;
    public IReadOnlyList<DdaAdaptationRule> AdaptationRules { get; set; } = default!;
    public bool LearningEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class DdaDifficultyProfileRequest
{
    public string PlayerId { get; set; } = default!;
    public DdaDifficultyLevel BaseDifficulty { get; set; }
    public DdaDifficultyProfileRequest() { }
    public DdaDifficultyProfileRequest(string playerId, DdaDifficultyLevel baseDifficulty)
    {
        PlayerId = playerId;
        BaseDifficulty = baseDifficulty;
    }
}

public class DdaDifficultyAdjustment
{
    public DdaDifficultyAdjustmentType AdjustmentType { get; set; }
    public double Magnitude { get; set; }
    public string Reasoning { get; set; } = default!;
    public double Confidence { get; set; }
    public TimeSpan SuggestedDuration { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class DdaOpponentBehavior
{
    public double AggressionLevel { get; set; }
    public double DefensePriority { get; set; }
    public double RiskTolerance { get; set; }
    public double PatternComplexity { get; set; }
    public TimeSpan ReactionTime { get; set; }
    public double ResourceUsage { get; set; }
    public double ComboFrequency { get; set; }
    public double ProjectileUsage { get; set; }
    public double AntiAirFrequency { get; set; }
    public double ThrowAttempts { get; set; }
    public double MeterManagement { get; set; }
    public DateTime ActiveUntil { get; set; }
}

public class DdaAdaptationMetrics
{
    public string PlayerId { get; set; } = default!;
    public TimeSpan Period { get; set; }
    public int DifficultyAdjustments { get; set; }
    public DdaSkillTrend PerformanceTrend { get; set; }
    public double AdaptationEffectiveness { get; set; }
    public double LearningProgress { get; set; }
    public DdaDifficultyLevel OptimalDifficulty { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class DdaChallengeCalibration
{
    public string PlayerId { get; set; } = default!;
    public DdaDifficultyLevel OptimalDifficulty { get; set; }
    public DdaAdaptiveSettings RecommendedSettings { get; set; } = default!;
    public IReadOnlyDictionary<DdaDifficultyLevel, DdaPerformanceZone> PerformanceZones { get; set; } = default!;
    public double AdaptationSensitivity { get; set; }
    public IReadOnlyList<double> ChallengeCurve { get; set; } = default!;
    public double ConfidenceLevel { get; set; }
    public DateTime LastCalibrated { get; set; }
}

public class DdaDifficultyReport
{
    public string PlayerId { get; set; } = default!;
    public TimeSpan ReportPeriod { get; set; }
    public DdaDifficultyProfile CurrentProfile { get; set; } = default!;
    public DdaAdaptationMetrics AdaptationMetrics { get; set; } = default!;
    public DdaPerformanceAnalysis PerformanceAnalysis { get; set; } = default!;
    public IReadOnlyList<string> Recommendations { get; set; } = default!;
    public DdaTrendAnalysis TrendAnalysis { get; set; } = default!;
    public DateTime GeneratedAt { get; set; }
}

public class DdaAdaptiveSettings
{
    public double BaseAdjustmentRate { get; set; }
    public double MaximumAdjustment { get; set; }
    public double MinimumAdjustment { get; set; }
    public TimeSpan AdaptationCooldown { get; set; }
    public TimeSpan PerformanceWindow { get; set; }
    public double ResetThreshold { get; set; }
}

public class DdaBehaviorParameters
{
    public double AggressionBase { get; set; }
    public double DefensePriority { get; set; }
    public double RiskTolerance { get; set; }
    public double AdaptationSpeed { get; set; }
    public double PatternRecognition { get; set; }
}

public class DdaPerformanceThresholds
{
    public double WinRateIncreaseThreshold { get; set; }
    public double WinRateDecreaseThreshold { get; set; }
    public double ComboSuccessThreshold { get; set; }
    public double DamageEfficiencyThreshold { get; set; }
    public double ResourceManagementThreshold { get; set; }
    public double TimingAccuracyThreshold { get; set; }
}

public class DdaAdaptationRule
{
    public string Condition { get; set; } = default!;
    public string Action { get; set; } = default!;
    public int Priority { get; set; }
    public TimeSpan Cooldown { get; set; }
}

public class DdaHistoricalPerformanceData
{
    public double AverageWinRate { get; set; }
    public DdaSkillTrend SkillProgression { get; set; }
    public DdaDifficultyLevel PreferredDifficulty { get; set; }
    public double ConsistencyRating { get; set; }
    public IReadOnlyList<string> Strengths { get; set; } = default!;
    public IReadOnlyList<string> Weaknesses { get; set; } = default!;
    public double LearningRate { get; set; }
}

public class DdaCurrentPerformanceData
{
    public double WinRate { get; set; }
    public double ComboSuccess { get; set; }
    public double DamageEfficiency { get; set; }
    public double ResourceManagement { get; set; }
    public double TimingAccuracy { get; set; }
    public double DecisionMaking { get; set; }
    public double AdaptationSpeed { get; set; }
}

public class DdaMatchState
{
    public string MatchId { get; set; } = default!;
    public string PlayerId { get; set; } = default!;
    public string OpponentId { get; set; } = default!;
    public int RoundNumber { get; set; }
    public TimeSpan MatchDuration { get; set; }
    public int PlayerHealth { get; set; }
    public int OpponentHealth { get; set; }
    public int PlayerMeter { get; set; }
    public int OpponentMeter { get; set; }
    public IReadOnlyList<string> RecentActions { get; set; } = default!;
    public IReadOnlyDictionary<string, double> PerformanceMetrics { get; set; } = default!;
}

public class DdaTrainingMatch
{
    public string MatchId { get; set; } = default!;
    public string PlayerId { get; set; } = default!;
    public DdaDifficultyLevel Difficulty { get; set; }
    public double PlayerPerformance { get; set; }
    public DdaDifficultyAdjustment Adjustment { get; set; } = default!;
    public DdaOpponentBehavior Behavior { get; set; } = default!;
}

public class DdaCalibrationRequest
{
    public IReadOnlyList<DdaDifficultyTest> DifficultyTests { get; set; } = default!;
    public TimeSpan CalibrationPeriod { get; set; }
    public IReadOnlyList<string> FocusAreas { get; set; } = default!;
}

public class DdaDifficultyTest
{
    public DdaDifficultyLevel Difficulty { get; set; }
    public TimeSpan Duration { get; set; }
    public double WinRate { get; set; }
    public double Engagement { get; set; }
    public IReadOnlyList<string> Feedback { get; set; } = default!;
}

public class DdaCalibrationData
{
    public DdaDifficultyLevel OptimalDifficulty { get; set; }
    public DdaAdaptiveSettings RecommendedSettings { get; set; } = default!;
    public IReadOnlyDictionary<DdaDifficultyLevel, DdaPerformanceZone> PerformanceZones { get; set; } = default!;
    public double AdaptationSensitivity { get; set; }
    public IReadOnlyList<double> ChallengeCurve { get; set; } = default!;
    public double ConfidenceLevel { get; set; }
}

public class DdaPerformanceZone
{
    public double WinRate { get; set; }
    public double Engagement { get; set; }
}

public class DdaPerformanceAnalysis
{
    public double OverallWinRate { get; set; }
    public double PeakPerformance { get; set; }
    public TimeSpan AverageMatchLength { get; set; }
    public IReadOnlyList<string> MostUsedTechniques { get; set; } = default!;
    public double LearningVelocity { get; set; }
    public double AdaptationResistance { get; set; }
}

public class DdaTrendAnalysis
{
    public IReadOnlyList<DdaDifficultyLevel> DifficultyProgression { get; set; } = default!;
    public double PerformanceCorrelation { get; set; }
    public IReadOnlyList<string> AdaptationPatterns { get; set; } = default!;
    public IReadOnlyList<TimeSpan> OptimalChallengePoints { get; set; } = default!;
    public IReadOnlyList<string> BurnoutIndicators { get; set; } = default!;
}

public class DdaAdaptationMetricsData
{
    public int DifficultyAdjustments { get; set; }
    public DdaSkillTrend PerformanceTrend { get; set; }
    public double AdaptationEffectiveness { get; set; }
    public double LearningProgress { get; set; }
    public DdaDifficultyLevel OptimalDifficulty { get; set; }
}

public enum DdaDifficultyAdjustmentType { Increase, Decrease, Maintain }
public enum DdaDifficultyLevel { VeryEasy, Easy, Medium, Hard, VeryHard }
public enum DdaSkillTrend { Improving, Stable, Declining }
