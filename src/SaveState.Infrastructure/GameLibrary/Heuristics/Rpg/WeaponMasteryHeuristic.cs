using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting weapon mastery levels in RPG games.
/// Weapon mastery values typically:
/// - Are integers in range 0-100
/// - Increase through weapon usage
/// - Affect weapon damage and abilities
/// </summary>
public sealed class WeaponMasteryHeuristic : IValueHeuristic
{
    public string Name => "Weapon Mastery Detection";
    public string Category => "RPG";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool onlyIncreases = true;
        int incrementEvents = 0;

        // Check value range (mastery typically 0-100)
        if (IsInMasteryRange(value.CurrentValue))
        {
            score += 0.4;
        }

        // Must be integer
        if (!HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score -= 0.2;
        }
        else
        {
            score += 0.15;
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

            // Check for increment
            if (currVal > prevVal)
            {
                incrementEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Small increments typical of mastery gains
                if (delta >= 1 && delta <= 5)
                {
                    score += 0.15;
                }
            }
            // Should never decrease
            else if (currVal < prevVal)
            {
                onlyIncreases = false;
                score -= 0.4;
            }

            // Should be non-negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Cap at 100 for most systems
            if (currVal > 100)
            {
                score -= 0.3;
            }
        }

        // Bonus for increment events
        if (incrementEvents >= 1)
            score += 0.1;

        // Bonus for only increasing
        if (onlyIncreases && history.Count > 2)
            score += 0.2;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short";
    }

    private static bool IsInMasteryRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 500;
        }
        catch
        {
            return false;
        }
    }
}