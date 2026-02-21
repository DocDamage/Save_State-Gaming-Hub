using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting nails/fasteners resource in crafting/construction games.
/// Nail values typically:
/// - Are integers (0-999)
/// - Increase when crafting or purchasing
/// - Decrease when building structures
/// </summary>
public sealed class NailHeuristic : IValueHeuristic
{
    public string Name => "Nails/Fasteners Detection";
    public string Category => "Resource";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int gainEvents = 0;
        int spendEvents = 0;

        // Check value range (nails typically 0-999, stackable in batches)
        if (IsInNailRange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Must be integer
        if (!HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score -= 0.2;
        }
        else
        {
            score += 0.1;
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

            // Check for gain (crafting/purchasing)
            if (currVal > prevVal)
            {
                gainEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Usually gain 10-50 nails at a time
                if (delta >= 5 && delta <= 100)
                {
                    score += 0.15;
                }
            }

            // Check for spend (building)
            if (currVal < prevVal)
            {
                spendEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Building uses 2-20 nails per structure
                if (delta >= 2 && delta <= 50)
                {
                    score += 0.12;
                }
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }
        }

        // Bonus for transaction patterns
        if (gainEvents >= 2)
            score += 0.15;
        if (spendEvents >= 1)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short";
    }

    private static bool IsInNailRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 9999;
        }
        catch
        {
            return false;
        }
    }
}