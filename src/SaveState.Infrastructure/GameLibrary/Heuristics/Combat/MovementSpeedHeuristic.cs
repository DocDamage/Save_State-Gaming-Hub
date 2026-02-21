using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting movement speed values in game memory.
/// Movement speed values typically:
/// - Are floats in range 1.0-1000.0 (units per second)
/// - Dynamic - change with stance, buffs, equipment
/// - Decrease while aiming, increase while sprinting
/// - Often normalized to base 100 or 1.0
/// </summary>
public sealed class MovementSpeedHeuristic : IValueHeuristic
{
    public string Name => "Movement Speed Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int sprintIncreases = 0;
        int aimDecreases = 0;

        // Check value range
        if (IsInMovementSpeedRange(value.CurrentValue))
        {
            score += 0.3;
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

            // Movement speed should never be negative
            if (currVal < 0)
            {
                score -= 0.3;
            }

            // Check for sprint correlation (speed increases)
            if (curr.RelatedAction == PlayerAction.Sprinted && delta > 0)
            {
                sprintIncreases++;
                score += 0.15;
            }

            // Check for attack correlation (speed decreases during attacks)
            if (curr.RelatedAction == PlayerAction.Attacked && delta < 0)
            {
                aimDecreases++;
                score += 0.15;
            }

            // Check for buff correlation (via used ability)
            if (curr.RelatedAction == PlayerAction.UsedAbility && delta > 0)
            {
                score += 0.1;
            }

            // Check for debuff correlation (via damage taken)
            if (curr.RelatedAction == PlayerAction.TookDamage && delta < 0)
            {
                score += 0.1;
            }
        }

        // Strong bonus for sprint patterns
        if (sprintIncreases >= 2)
        {
            score += 0.2;
        }

        // Strong bonus for aim patterns
        if (aimDecreases >= 2)
        {
            score += 0.2;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsInMovementSpeedRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            // Movement speed typically 1-1000 (units/sec) or 0-500% modifier
            return (val >= 1.0 && val <= 1000.0) || (val >= 0.0 && val <= 500.0);
        }
        catch
        {
            return false;
        }
    }
}