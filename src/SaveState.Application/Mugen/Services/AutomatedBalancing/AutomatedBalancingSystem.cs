using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;

namespace SaveState.Application.Mugen.Services.AutomatedBalancing;

/// <summary>
/// Automated balancing system using machine learning to analyze gameplay data
/// and maintain game balance through intelligent character and mechanic adjustments.
/// </summary>
public class AutomatedBalancingSystem : IAutomatedBalancingSystem
{
    private readonly ILogger<AutomatedBalancingSystem> _logger;
    private readonly ICacheService _cache;
    private readonly ITimeProvider _timeProvider;
    private readonly Engines.BalanceAnalyzer _balanceAnalyzer;
    private readonly Engines.AdjustmentEngine _adjustmentEngine;
    private readonly Engines.GameStateMonitor _gameStateMonitor;
    private readonly Engines.BalancePredictor _balancePredictor;

    public AutomatedBalancingSystem(
        ILogger<AutomatedBalancingSystem> logger,
        ILoggerFactory loggerFactory,
        ICacheService cache,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _cache = cache;
        _timeProvider = timeProvider;
        _balanceAnalyzer = new Engines.BalanceAnalyzer(loggerFactory.CreateLogger<Engines.BalanceAnalyzer>(), timeProvider);
        _adjustmentEngine = new Engines.AdjustmentEngine(loggerFactory.CreateLogger<Engines.AdjustmentEngine>(), timeProvider);
        _gameStateMonitor = new Engines.GameStateMonitor(loggerFactory.CreateLogger<Engines.GameStateMonitor>(), timeProvider);
        _balancePredictor = new Engines.BalancePredictor(loggerFactory.CreateLogger<Engines.BalancePredictor>(), timeProvider);
    }

    public async Task<BalanceAnalysis> AnalyzeBalanceAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Analyzing game balance");
        return await _balanceAnalyzer.AnalyzeAsync("game_001", ct);
    }

    public async Task<IReadOnlyList<BalanceAdjustment>> GenerateAdjustmentsAsync(BalanceAnalysis analysis, CancellationToken ct = default)
    {
        _logger.LogInformation("Generating balance adjustments based on analysis");
        return await _adjustmentEngine.GenerateAdjustmentsAsync(analysis, ct);
    }

    public async Task<BalancePatch> ApplyPatchAsync(BalanceAdjustment adjustment, CancellationToken ct = default)
    {
        _logger.LogInformation("Applying balance patch for {TargetElement}", adjustment.TargetElement);
        return await _adjustmentEngine.ApplyPatchAsync(adjustment, ct);
    }

    public async Task<GameBalanceReport> GetBalanceReportAsync(string gameId, CancellationToken ct = default)
    {
        _logger.LogInformation("Generating balance report for game {GameId}", gameId);

        var analysis = await _balanceAnalyzer.AnalyzeAsync(gameId, ct);
        var adjustments = await GenerateAdjustmentsAsync(analysis, ct);

        var characterOverviews = analysis.CharacterMetrics.Select(m => new CharacterBalanceOverview
        {
            CharacterId = m.CharacterId,
            CharacterName = m.CharacterId,
            OverallWinRate = m.WinRate,
            TierPlacement = m.Tier,
            Strengths = new List<string>(),
            Weaknesses = new List<string>()
        }).ToList();

        var suggestions = adjustments.Select(a => new BalanceSuggestion
        {
            TargetCharacter = a.TargetElement,
            Suggestion = $"{a.Type} {a.TargetElement}",
            Priority = a.Confidence,
            Rationale = a.Reason
        }).ToList();

        var report = new GameBalanceReport
        {
            GameId = gameId,
            GeneratedAt = _timeProvider.UtcNow,
            CharacterOverviews = characterOverviews,
            Suggestions = suggestions,
            RiskAssessment = new BalancingRiskAssessment()
        };

        _logger.LogInformation("Balance report generated for game {GameId}", gameId);
        return report;
    }
}
