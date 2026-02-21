using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting bleeding status in survival/hardcore games.
/// Bleeding values typically:
/// - Are floats (0.0-100.0) representing severity
/// - Increase when taking damage
/// - Decrease with bandages/medical treatment
/// </summary>
public sealed class BleedingHeuristic : IValueHeuristic
{
    public string Name => "Bleeding Status Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int increaseEvents = 0;
        int decreaseEvents = 0;

        // Check value range (bleeding typically 0-100)
        if (IsInBleedingRange(value.CurrentValue))
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

            // Check for increase (taking damage)
            if (currVal > prevVal && curr.RelatedAction == PlayerAction.TookDamage)
            {
                increaseEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Bleeding increases when hit
                if (delta > 10 && delta <= 50)
                {
                    score += 0.2;
                }
            }

            // Check for decrease (healing)
            if (currVal < prevVal)
            {
                decreaseEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Medical treatment reduces bleeding significantly
                if (delta > 20)
                {
                    score += 0.2;
                }
                // Natural clotting is slower
                else if (delta > 0)
                {
                    score += 0.08;
                }
            }

            // Should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Typically caps at 100
            if (currVal > 200)
            {
                score -= 0.3;
            }
        }

        // Bonus for patterns
        if (increaseEvents >= 1)
            score += 0.15;
        if (decreaseEvents >= 1)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "int32" or "int" or "double";
    }

    private static bool IsInBleedingRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 200;
        }
        catch
        {
            return false;
        }
    }
}