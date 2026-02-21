using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting combo/hit count in fighting/action games.
/// Combo values typically:
/// - Are positive integers (1-999)
/// - Increase with consecutive hits
/// - Reset to 0 when combo breaks
/// - Often shown in fighting games, action RPGs, and hack-and-slash games
/// </summary>
public sealed class ComboCountHeuristic : IValueHeuristic
{
    public string Name => "Combo/Hits Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int increaseEvents = 0;
        int resetEvents = 0;
        int smallValues = 0;

        // Check value range (combos typically 0-999)
        if (IsInComboRange(value.CurrentValue))
        {
            score += 0.3;
        }

        // Must be integer type
        if (!HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score -= 0.3;
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

            // Count small values (most combos are short)
            if (currVal >= 0 && currVal <= 20)
            {
                smallValues++;
            }

            // Check for increase during combat
            if (currVal > prevVal && curr.RelatedAction == PlayerAction.Attacked)
            {
                increaseEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Combos usually increment by 1
                if (delta == 1)
                {
                    score += 0.15;
                }
            }

            // Check for reset (combo broken - goes to 0)
            if (currVal == 0 && prevVal > 0)
            {
                resetEvents++;
                score += 0.2;
            }

            // Combo should not decrease (only reset or increase)
            if (currVal < prevVal && currVal != 0)
            {
                score -= 0.4;
            }

            // Combo values should be positive
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Combo values rarely exceed 999
            if (currVal > 9999)
            {
                score -= 0.3;
            }
        }

        // Bonus for increase events
        if (increaseEvents >= 2)
            score += 0.15;

        // Bonus for reset pattern
        if (resetEvents >= 1)
            score += 0.15;

        // Bonus for mostly small values
        if (smallValues >= 3)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "int64" or "long" or "byte";
    }

    private static bool IsInComboRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Combo typically in range 0-9999
            var val = doubleValue.Value;
            return val >= 0 && val <= 9999;
        }
        catch
        {
            return false;
        }
    }
}