using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting healing efficiency values in game memory.
/// Healing efficiency values typically:
/// - Are floats in range 0.0-200.0 (percentage)
/// - Static or change with gear/skill upgrades
/// - Increase amount of healing received or given
/// - Often found on support builds and healers
/// </summary>
public sealed class HealingEfficiencyHeuristic : IValueHeuristic
{
    public string Name => "Healing Efficiency Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int increases = 0;
        int decreases = 0;
        int healCorrelations = 0;

        // Check value range
        if (IsInEfficiencyRange(value.CurrentValue))
        {
            score += 0.4;
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

            if (delta > 0)
                increases++;
            else if (delta < 0)
                decreases++;

            // Changes typically from gear upgrades
            if (Math.Abs(delta) > 0.001 && Math.Abs(delta) < 30)
            {
                score += 0.05;
            }

            // Check for heal correlation
            if (curr.RelatedAction == PlayerAction.Healed && delta == 0)
            {
                healCorrelations++;
            }

            // Check for level up correlation
            if (curr.RelatedAction == PlayerAction.LeveledUp && delta > 0)
            {
                score += 0.1;
            }
        }

        // Healing efficiency should be relatively static
        if (history.Count > 1)
        {
            var changeRatio = (double)(increases + decreases) / (history.Count - 1);
            if (changeRatio < 0.1)
            {
                score += 0.25;
            }
        }

        // Usually increases with support gear
        if (increases >= decreases)
        {
            score += 0.1;
        }

        // Bonus for heal correlations
        if (healCorrelations >= 2)
        {
            score += 0.1;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsInEfficiencyRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            // Support both percentage (0-200) and decimal (0-2) formats
            return (val >= 0.0 && val <= 200.0) || (val >= 0.0 && val <= 2.0);
        }
        catch
        {
            return false;
        }
    }
}