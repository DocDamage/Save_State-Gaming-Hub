using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting critical resistance values in game memory.
/// Critical resistance values typically:
/// - Are floats in range 0.0-100.0 (percentage)
/// - Static or change with gear/skill upgrades
/// - Reduce chance of receiving critical hits
/// - Often found in RPG and MMO games
/// </summary>
public sealed class CritResistanceHeuristic : IValueHeuristic
{
    public string Name => "Critical Resistance Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int increases = 0;
        int decreases = 0;
        int damageCorrelations = 0;

        // Check value range
        if (IsInResistanceRange(value.CurrentValue))
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
            if (Math.Abs(delta) > 0.001 && Math.Abs(delta) < 20)
            {
                score += 0.05;
            }

            // Check for damage taken correlation
            if (curr.RelatedAction == PlayerAction.TookDamage && delta == 0)
            {
                damageCorrelations++;
            }

            // Check for level up correlation
            if (curr.RelatedAction == PlayerAction.LeveledUp && delta > 0)
            {
                score += 0.1;
            }
        }

        // Critical resistance should be relatively static
        if (history.Count > 1)
        {
            var changeRatio = (double)(increases + decreases) / (history.Count - 1);
            if (changeRatio < 0.1)
            {
                score += 0.25;
            }
        }

        // Usually increases with defensive gear
        if (increases >= decreases)
        {
            score += 0.1;
        }

        // Bonus for damage correlations
        if (damageCorrelations >= 2)
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

    private static bool IsInResistanceRange(object? value)
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