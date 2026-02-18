using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.BalanceTuning;
using SaveState.Application.Mugen.Services.BalanceTuning.Engines;

namespace SaveState.Application.Mugen.Services.BalanceTuning;

/// <summary>
/// Balance tuning service for competitive balance of advanced mechanics.
/// Analyzes match data and adjusts mechanic parameters for fair gameplay.
/// Refactored to use extracted engines and models.
/// </summary>
public class BalanceTuningService : IBalanceTuningService
{
    private readonly ILogger<BalanceTuningService> _logger;
    private readonly ICacheService _cache;
    private readonly IServiceProvider _serviceProvider;
    private readonly ITimeProvider _timeProvider;

    // Balance state tracking
    private readonly Dictionary<string, BalanceProfile> _balanceProfiles = new();
    private readonly Dictionary<string, MechanicBalance> _mechanicBalances = new();
    private readonly Queue<MatchData> _matchDataQueue = new();
    private readonly List<BalanceAdjustment> _pendingAdjustments = new();

    // Engines
    private readonly EloCalculator _eloCalculator;
    private readonly MatchmakingBalance _matchmakingBalance;
    private readonly StatisticalAnalyzer _statisticalAnalyzer;
    private readonly BalanceAnalysisEngine _analysisEngine;
    private readonly AdjustmentEngine _adjustmentEngine;
    private readonly ReportingEngine _reportingEngine;
    private readonly MonitoringEngine _monitoringEngine;

    public BalanceTuningService(
        ILogger<BalanceTuningService> logger,
        ILoggerFactory loggerFactory,
        ICacheService cache,
        IServiceProvider serviceProvider,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _cache = cache;
        _serviceProvider = serviceProvider;
        _timeProvider = timeProvider;

        _eloCalculator = new EloCalculator(loggerFactory.CreateLogger<EloCalculator>());
        _matchmakingBalance = new MatchmakingBalance(loggerFactory.CreateLogger<MatchmakingBalance>());
        _statisticalAnalyzer = new StatisticalAnalyzer(loggerFactory.CreateLogger<StatisticalAnalyzer>(), _timeProvider);
        _analysisEngine = new BalanceAnalysisEngine(loggerFactory.CreateLogger<BalanceAnalysisEngine>());
        _adjustmentEngine = new AdjustmentEngine(loggerFactory.CreateLogger<AdjustmentEngine>(), _timeProvider);
        _reportingEngine = new ReportingEngine(loggerFactory.CreateLogger<ReportingEngine>(), _timeProvider);
        _monitoringEngine = new MonitoringEngine(loggerFactory.CreateLogger<MonitoringEngine>(), _timeProvider);

        InitializeBalanceSystems();
    }

    public async Task<Result<BalanceAnalysis>> AnalyzeBalanceAsync(string sessionId, IReadOnlyList<MatchData> matchData, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Analyzing balance for session {SessionId} with {MatchCount} matches", sessionId, matchData.Count);

            var analysis = new BalanceAnalysis
            {
                SessionId = sessionId,
                MatchCount = matchData.Count,
                MechanicUsage = _analysisEngine.AnalyzeMechanicUsage(matchData),
                WinRates = _analysisEngine.CalculateWinRates(matchData),
                PlaytimeDistribution = _analysisEngine.AnalyzePlaytimeDistribution(matchData),
                SkillGapAnalysis = _analysisEngine.AnalyzeSkillGaps(matchData),
                BalanceScore = _analysisEngine.CalculateBalanceScore(matchData),
                Recommendations = _analysisEngine.GenerateBalanceRecommendations(matchData),
                AnalysisTimestamp = _timeProvider.UtcNow
            };

            await _cache.SetAsync($"balance_analysis_{sessionId}", analysis, TimeSpan.FromHours(2), ct);

            _logger.LogInformation("Balance analysis completed: Score {Score:F2}, {Recommendations} recommendations",
                analysis.BalanceScore, analysis.Recommendations.Count);

            return Result.Success<BalanceAnalysis>(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing balance");
            return Result.Failure<BalanceAnalysis>($"Balance analysis failed: {ex.Message}");
        }
    }

    public async Task<Result<BalanceAdjustment>> CalculateAdjustmentAsync(MechanicType mechanic, BalanceData balanceData, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Calculating balance adjustment for {Mechanic}", mechanic);

            var currentBalance = GetOrCreateMechanicBalance(mechanic);
            var adjustment = new BalanceAdjustment
            {
                Mechanic = mechanic,
                CurrentParameters = currentBalance.Parameters,
                TargetParameters = _adjustmentEngine.CalculateTargetParameters(mechanic, balanceData, currentBalance.Parameters),
                AdjustmentType = _adjustmentEngine.DetermineAdjustmentType(mechanic, balanceData),
                Magnitude = _adjustmentEngine.CalculateAdjustmentMagnitude(mechanic, balanceData),
                Confidence = _adjustmentEngine.CalculateAdjustmentConfidence(balanceData),
                Rationale = _adjustmentEngine.GenerateAdjustmentRationale(mechanic, balanceData),
                CalculatedAt = _timeProvider.UtcNow
            };

            if (!_adjustmentEngine.ValidateAdjustment(adjustment))
            {
                return Result.Failure<BalanceAdjustment>("Adjustment would break game balance");
            }

            _pendingAdjustments.Add(adjustment);

            _logger.LogInformation("Balance adjustment calculated: {Mechanic} {Type} by {Magnitude:F2}%",
                mechanic, adjustment.AdjustmentType, adjustment.Magnitude * 100);

            return Result.Success<BalanceAdjustment>(adjustment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating balance adjustment");
            return Result.Failure<BalanceAdjustment>($"Balance adjustment calculation failed: {ex.Message}");
        }
    }

    public async Task<Result<AdjustmentApplication>> ApplyAdjustmentAsync(BalanceAdjustment adjustment, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Applying balance adjustment for {Mechanic}", adjustment.Mechanic);

            var application = await _adjustmentEngine.ApplyMechanicAdjustmentAsync(adjustment, ct);

            var balance = GetOrCreateMechanicBalance(adjustment.Mechanic);
            balance.Parameters = adjustment.TargetParameters;
            balance.LastAdjusted = _timeProvider.UtcNow;
            balance.AdjustmentCount++;

            _pendingAdjustments.Remove(adjustment);

            var result = new AdjustmentApplication
            {
                Adjustment = adjustment,
                AppliedAt = _timeProvider.UtcNow,
                Success = application.Success,
                PerformanceImpact = application.PerformanceImpact,
                RollbackAvailable = true
            };

            _logger.LogInformation("Balance adjustment applied: {Mechanic} - Success: {Success}",
                adjustment.Mechanic, result.Success);

            return Result.Success<AdjustmentApplication>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying balance adjustment");
            return Result.Failure<AdjustmentApplication>($"Balance adjustment application failed: {ex.Message}");
        }
    }

    public async Task<Result<BalancePatch>> CreateBalancePatchAsync(IReadOnlyList<BalanceAdjustment> adjustments, string patchVersion, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating balance patch {Version} with {AdjustmentCount} adjustments", patchVersion, adjustments.Count);

            var patch = new BalancePatch
            {
                Version = patchVersion,
                Adjustments = adjustments.ToList(),
                CreatedAt = _timeProvider.UtcNow,
                TestResults = await _monitoringEngine.RunBalanceTestsAsync(adjustments, ct),
                RiskAssessment = _monitoringEngine.AssessPatchRisk(adjustments),
                RollbackPlan = _monitoringEngine.GenerateRollbackPlan(adjustments)
            };

            if (!_monitoringEngine.ValidateBalancePatch(patch))
            {
                return Result.Failure<BalancePatch>("Balance patch validation failed");
            }

            await _cache.SetAsync($"balance_patch_{patchVersion}", patch, TimeSpan.FromDays(30), ct);

            _logger.LogInformation("Balance patch created: {Version} - Risk Level: {Risk}",
                patchVersion, patch.RiskAssessment.Level);

            return Result.Success<BalancePatch>(patch);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating balance patch");
            return Result.Failure<BalancePatch>($"Balance patch creation failed: {ex.Message}");
        }
    }

    public async Task<Result<BalanceMonitoring>> MonitorBalanceAsync(string sessionId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Monitoring balance for session {SessionId}", sessionId);

            var monitoring = new BalanceMonitoring
            {
                SessionId = sessionId,
                CurrentMetrics = await _monitoringEngine.CollectBalanceMetricsAsync(sessionId, ct),
                TrendAnalysis = _monitoringEngine.AnalyzeBalanceTrends(sessionId),
                Alerts = _monitoringEngine.GenerateBalanceAlerts(sessionId),
                HealthScore = _monitoringEngine.CalculateBalanceHealth(sessionId),
                MonitoringTimestamp = _timeProvider.UtcNow
            };

            var criticalAlerts = monitoring.Alerts.Where(a => a.Severity == AlertSeverity.Critical).ToList();
            if (criticalAlerts.Any())
            {
                _logger.LogWarning("Critical balance alerts detected: {Count}", criticalAlerts.Count);
                await _monitoringEngine.TriggerBalanceInterventionAsync(criticalAlerts, sessionId, ct);
            }

            _logger.LogInformation("Balance monitoring completed: Health score {Score:F2}, {Alerts} alerts",
                monitoring.HealthScore, monitoring.Alerts.Count);

            return Result.Success<BalanceMonitoring>(monitoring);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error monitoring balance");
            return Result.Failure<BalanceMonitoring>($"Balance monitoring failed: {ex.Message}");
        }
    }

    public async Task<Result<CompetitiveRanking>> UpdateCompetitiveRankingAsync(IReadOnlyList<PlayerStats> playerStats, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Updating competitive ranking for {PlayerCount} players", playerStats.Count);

            var ranking = new CompetitiveRanking
            {
                Players = _reportingEngine.CalculatePlayerRankings(playerStats),
                Divisions = _reportingEngine.GenerateRankingDivisions(playerStats),
                SeasonStats = _reportingEngine.CalculateSeasonStatistics(playerStats),
                BalanceFactors = _reportingEngine.CalculateBalanceFactors(playerStats),
                UpdatedAt = _timeProvider.UtcNow
            };

            if (!_reportingEngine.ValidateCompetitiveRanking(ranking))
            {
                return Result.Failure<CompetitiveRanking>("Competitive ranking validation failed");
            }

            await _cache.SetAsync("competitive_ranking", ranking, TimeSpan.FromMinutes(30), ct);

            _logger.LogInformation("Competitive ranking updated: {DivisionCount} divisions, top player rating {TopRating:F0}",
                ranking.Divisions.Count, ranking.Players.FirstOrDefault()?.Rating ?? 0);

            return Result.Success<CompetitiveRanking>(ranking);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating competitive ranking");
            return Result.Failure<CompetitiveRanking>($"Competitive ranking update failed: {ex.Message}");
        }
    }

    public async Task<Result<BalanceReport>> GenerateBalanceReportAsync(string sessionId, DateRange dateRange, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating balance report for session {SessionId}", sessionId);

            var report = new BalanceReport
            {
                SessionId = sessionId,
                DateRange = dateRange,
                ExecutiveSummary = await _reportingEngine.GenerateExecutiveSummaryAsync(sessionId, dateRange, ct),
                MechanicAnalysis = await _reportingEngine.AnalyzeMechanicBalanceAsync(sessionId, dateRange, ct),
                PlayerFeedback = await _reportingEngine.CollectPlayerFeedbackAsync(sessionId, dateRange, ct),
                TournamentResults = await _reportingEngine.AnalyzeTournamentResultsAsync(sessionId, dateRange, ct),
                Recommendations = _reportingEngine.GenerateReportRecommendations(sessionId, dateRange),
                GeneratedAt = _timeProvider.UtcNow
            };

            await _cache.SetAsync($"balance_report_{sessionId}_{dateRange.Start:yyyyMMdd}_{dateRange.End:yyyyMMdd}",
                report, TimeSpan.FromDays(7), ct);

            _logger.LogInformation("Balance report generated for {SessionId}", sessionId);
            return Result.Success<BalanceReport>(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating balance report");
            return Result.Failure<BalanceReport>($"Balance report generation failed: {ex.Message}");
        }
    }

    #region Private Methods

    private void InitializeBalanceSystems()
    {
        _logger.LogInformation("Balance tuning systems initialized");
    }

    private MechanicBalance GetOrCreateMechanicBalance(MechanicType mechanic)
    {
        if (!_mechanicBalances.TryGetValue(mechanic.ToString(), out var balance))
        {
            balance = _adjustmentEngine.CreateMechanicBalance(mechanic);
            _mechanicBalances[mechanic.ToString()] = balance;
        }
        return balance;
    }

    #endregion
}
