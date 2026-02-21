using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting attack speed values in game memory.
/// Attack speed values typically:
/// - Are floats in range 0.1-10.0 (attacks per second) or percentage
/// - Static or change with gear/skill upgrades
/// - Affects melee and ranged attack rate
/// - Often displayed as percentage bonus
/// </summary>
public sealed class AttackSpeedHeuristic : IValueHeuristic
{
    public string Name => "Attack Speed Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int increases = 0;
        int decreases = 0;
        int attackCorrelations = 0;

        // Check value range
        if (IsInAttackSpeedRange(value.CurrentValue))
        {
            score += 0.35;
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

            // Changes typically from gear/buffs
            if (Math.Abs(delta) > 0.001 && Math.Abs(delta) < 50)
            {
                score += 0.05;
            }

            // Check for attack correlation
            if (curr.RelatedAction == PlayerAction.Attacked && delta == 0)
            {
                attackCorrelations++;
            }

            // Check for buff correlation (via used ability)
            if (curr.RelatedAction == PlayerAction.UsedAbility && delta > 0)
            {
                score += 0.1;
            }
        }

        // Attack speed should be relatively static
        if (history.Count > 1)
        {
            var changeRatio = (double)(increases + decreases) / (history.Count - 1);
            if (changeRatio < 0.15)
            {
                score += 0.25;
            }
        }

        // Usually increases with better gear
        if (increases >= decreases)
        {
            score += 0.1;
        }

        // Bonus for attack correlations
        if (attackCorrelations >= 2)
        {
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsInAttackSpeedRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            // Support APS (0.1-10.0) or percentage (0-1000%)
            return (val >= 0.1 && val <= 10.0) || (val >= 0.0 && val <= 1000.0);
        }
        catch
        {
            return false;
        }
    }
}