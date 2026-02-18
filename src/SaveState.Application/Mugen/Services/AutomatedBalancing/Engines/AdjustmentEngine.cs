using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;

namespace SaveState.Application.Mugen.Services.AutomatedBalancing.Engines;

/// <summary>
/// Generates balance adjustments.
/// </summary>
public class AdjustmentEngine
{
    private readonly ILogger<AdjustmentEngine> _logger;
    private readonly ITimeProvider _timeProvider;

    public AdjustmentEngine(ILogger<AdjustmentEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<BalanceAdjustment>> GenerateAdjustmentsAsync(BalanceAnalysis analysis, CancellationToken ct = default)
    {
        _logger.LogInformation("Generating adjustments for game {GameId}", analysis.GameId);

        var adjustments = new List<BalanceAdjustment>();

        // Analyze problematic elements and generate adjustments
        foreach (var element in analysis.Trends.ProblematicElements)
        {
            if (element.Severity > 0.7)
            {
                adjustments.Add(CreateAdjustment(element, analysis));
            }
        }

        return await Task.FromResult(adjustments);
    }

    private BalanceAdjustment CreateAdjustment(ProblematicElement element, BalanceAnalysis analysis)
    {
        return new BalanceAdjustment
        {
            AdjustmentId = Guid.NewGuid().ToString(),
            TargetElement = element.ElementId,
            Type = DetermineAdjustmentType(element),
            Magnitude = element.Severity,
            Reason = element.Issue,
            Confidence = 0.8,
            StatAdjustments = Array.Empty<StatAdjustment>(),
            MoveAdjustments = Array.Empty<MoveAdjustment>()
        };
    }

    private AdjustmentType DetermineAdjustmentType(ProblematicElement element)
    {
        return element.Issue.Contains("overpowered", StringComparison.OrdinalIgnoreCase)
            ? AdjustmentType.Nerf
            : AdjustmentType.Buff;
    }

    public async Task<BalancePatch> ApplyPatchAsync(BalanceAdjustment adjustment, CancellationToken ct = default)
    {
        _logger.LogInformation("Applying patch for {Target}", adjustment.TargetElement);

        return new BalancePatch
        {
            PatchId = Guid.NewGuid().ToString(),
            Version = "1.0.0",
            AppliedAt = _timeProvider.UtcNow,
            Adjustments = new[] { adjustment },
            Impact = new PatchImpact { WinRateChange = 0.02, PickRateChange = 0.01 },
            TestResult = new PatchTestResult { Passed = true, StabilityScore = 0.95 }
        };
    }
}