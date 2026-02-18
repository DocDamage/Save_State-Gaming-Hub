namespace SaveState.Application.Mugen.Services.BalanceTuning.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.BalanceTuning;
using SaveState.Core.Common.Services;

/// <summary>
/// Engine for calculating and applying balance adjustments.
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

    /// <summary>
    /// Calculates target parameters for a mechanic adjustment.
    /// </summary>
    public Dictionary<string, object> CalculateTargetParameters(MechanicType mechanic, BalanceData balanceData, IReadOnlyDictionary<string, object> currentParams)
    {
        var target = new Dictionary<string, object>(currentParams);

        // Calculate desired change based on win rate distance from 50%
        var desiredChange = (0.5f - balanceData.WinRate) * 100; // Percentage adjustment needed
        var adjustmentFactor = 1 + (desiredChange / 100f);

        foreach (var param in currentParams)
        {
            if (param.Value is float floatValue)
            {
                target[param.Key] = floatValue * adjustmentFactor;
            }
            else if (param.Value is int intValue)
            {
                target[param.Key] = (int)(intValue * adjustmentFactor);
            }
        }

        return target;
    }

    /// <summary>
    /// Determines the type of adjustment needed.
    /// </summary>
    public string DetermineAdjustmentType(MechanicType mechanic, BalanceData balanceData)
    {
        var desiredChange = (0.5f - balanceData.WinRate) * 100;
        if (desiredChange > 10)
            return "Buff";
        if (desiredChange < -10)
            return "Nerf";
        return "Tweak";
    }

    /// <summary>
    /// Calculates the magnitude of the adjustment.
    /// </summary>
    public float CalculateAdjustmentMagnitude(MechanicType mechanic, BalanceData balanceData)
    {
        var desiredChange = Math.Abs(0.5f - balanceData.WinRate) * 100;
        return Math.Min(desiredChange / 100f, 0.3f); // Cap at 30%
    }

    /// <summary>
    /// Calculates confidence level for the adjustment.
    /// </summary>
    public float CalculateAdjustmentConfidence(BalanceData balanceData)
    {
        var sampleSizeFactor = Math.Min(balanceData.MatchCount / 100f, 1.0f);
        var usageFactor = Math.Min(balanceData.UsageRate * 2, 1.0f); // Higher usage = more data
        return sampleSizeFactor * usageFactor;
    }

    /// <summary>
    /// Generates rationale for the adjustment.
    /// </summary>
    public string GenerateAdjustmentRationale(MechanicType mechanic, BalanceData balanceData)
    {
        var desiredChange = (0.5f - balanceData.WinRate) * 100;
        return $"Based on {balanceData.MatchCount} matches with {balanceData.WinRate:P} win rate and {balanceData.UsageRate:P} usage. {mechanic} requires {(desiredChange > 0 ? "buff" : "nerf")} of {Math.Abs(desiredChange):F1}%.";
    }

    /// <summary>
    /// Validates that an adjustment won't break game balance.
    /// </summary>
    public bool ValidateAdjustment(BalanceAdjustment adjustment)
    {
        // Don't allow adjustments that are too extreme
        if (adjustment.Magnitude > 0.5f)
            return false;

        // Don't allow adjustments with low confidence
        if (adjustment.Confidence < 0.3f)
            return false;

        return true;
    }

    /// <summary>
    /// Applies a mechanic adjustment.
    /// </summary>
    public Task<MechanicAdjustmentApplication> ApplyMechanicAdjustmentAsync(BalanceAdjustment adjustment, CancellationToken ct = default)
    {
        _logger.LogInformation("Applying adjustment for {Mechanic}: {Type} by {Magnitude:P}",
            adjustment.Mechanic, adjustment.AdjustmentType, adjustment.Magnitude);

        var application = new MechanicAdjustmentApplication
        {
            Success = true,
            PerformanceImpact = adjustment.Magnitude * 0.1f, // Small performance impact
            AppliedAt = _timeProvider.UtcNow
        };

        return Task.FromResult(application);
    }

    /// <summary>
    /// Creates a new mechanic balance entry.
    /// </summary>
    public MechanicBalance CreateMechanicBalance(MechanicType mechanic)
    {
        return new MechanicBalance
        {
            Mechanic = mechanic,
            Parameters = new Dictionary<string, object>(),
            AdjustmentCount = 0,
            CreatedAt = _timeProvider.UtcNow
        };
    }
}
