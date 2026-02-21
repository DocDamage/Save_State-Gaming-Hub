using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting freeze chance values in game memory.
/// Freeze chance values typically:
/// - Are floats in range 0.0-100.0 (percentage)
/// - Static or change with gear/skill upgrades
/// - Often associated with ice/cold elemental effects
/// - May trigger on successful attacks with cold weapons
/// </summary>
public sealed class FreezeChanceHeuristic : IValueHeuristic
{
    public string Name => "Freeze Chance Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int increases = 0;
        int decreases = 0;

        // Check value range
        if (IsInChanceRange(value.CurrentValue))
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

            // Small changes indicate gradual improvement
            if (Math.Abs(delta) > 0.001 && Math.Abs(delta) < 10)
            {
                score += 0.05;
            }

            // Check for skill upgrade correlation
            if (curr.RelatedAction == PlayerAction.LeveledUp && delta > 0)
            {
                score += 0.1;
            }
        }

        // Freeze chance should be relatively static
        if (history.Count > 1)
        {
            var changeRatio = (double)(increases + decreases) / (history.Count - 1);
            if (changeRatio < 0.15)
            {
                score += 0.25;
            }
        }

        // Usually increases with gear/levels
        if (increases >= decreases)
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

    private static bool IsInChanceRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            // Support both percentage (0-100) and decimal (0-1) formats
            return (val >= 0.0 && val <= 100.0) || (val >= 0.0 && val <= 1.0);
        }
        catch
        {
            return false;
        }
    }
}