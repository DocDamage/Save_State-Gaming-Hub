using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting weapon charge time values in game memory.
/// Charge time values typically:
/// - Are floats in range 0.1-5.0 (seconds)
/// - Static or change with weapon/attachment upgrades
/// - Time required to fully charge a shot
/// - Common in bows, railguns, and charge weapons
/// </summary>
public sealed class ChargeTimeHeuristic : IValueHeuristic
{
    public string Name => "Charge Time Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int changes = 0;
        int weaponSwitchCorrelations = 0;

        // Check value range
        if (IsInChargeTimeRange(value.CurrentValue))
        {
            score += 0.45;
        }

        // Analyze observation history
        for (int i = 1; i < history.Count; i++)
        {
            var prev = history[i - 1];
            var curr = history[i];

            if (prev.Value == null || curr.Value == null)
                continue;

            double? prevVal = HeuristicUtilities.ConvertToDouble(prev.Value);
            double? currVal = HeuristicUtilities.ConvertToDouble(curr.Value);

            if (!prevVal.HasValue || !currVal.HasValue)
                continue;

            var delta = currVal.Value - prevVal.Value;

            // Track changes
            if (Math.Abs(delta) > 0.001)
            {
                changes++;
            }

            // Charge time changes typically from attachments
            if (Math.Abs(delta) > 0.1 && Math.Abs(delta) < 2.0)
            {
                score += 0.1;
            }

            // Charge time changes on equipment change (tracked via attack)
            if (curr.RelatedAction == PlayerAction.Attacked && delta != 0)
            {
                weaponSwitchCorrelations++;
                score += 0.15;
            }
        }

        // Charge time should be relatively static per weapon
        if (history.Count > 1)
        {
            var changeRatio = (double)changes / (history.Count - 1);
            if (changeRatio < 0.1)
            {
                score += 0.3;
            }
        }

        // Bonus for weapon switch correlations
        if (weaponSwitchCorrelations >= 1)
        {
            score += 0.1;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double";
    }

    private static bool IsInChargeTimeRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            // Charge time typically 0.1-5.0 seconds
            return val >= 0.1 && val <= 5.0;
        }
        catch
        {
            return false;
        }
    }
}