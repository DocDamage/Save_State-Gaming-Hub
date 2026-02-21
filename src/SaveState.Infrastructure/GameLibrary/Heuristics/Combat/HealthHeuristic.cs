using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting health values in game memory.
/// Health values typically:
/// - Are integers 1-10000 or floats 1.0-1000.0
/// - Decrease when "TookDamage" action reported
/// - Increase when "Healed" action reported
/// - Often have a nearby "max health" value
/// - Rarely go above a certain threshold
/// </summary>
public sealed class HealthHeuristic : IValueHeuristic
{
    public string Name => "Health Detection";
    public string Category => "Health";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int healthIndicators = 0;
        int damageEvents = 0;
        int healEvents = 0;

        // Check value range
        if (IsInHealthRange(value.CurrentValue))
        {
            score += 0.2;
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

            // Check for damage patterns (decrease after TookDamage)
            if (curr.RelatedAction == PlayerAction.TookDamage && currVal < prevVal)
            {
                damageEvents++;
                healthIndicators++;

                // Health typically decreases by reasonable amounts
                var delta = prevVal.Value - currVal.Value;
                if (delta > 0 && delta < 1000)
                {
                    score += 0.1;
                }
            }

            // Check for healing patterns (increase after Healed)
            if (curr.RelatedAction == PlayerAction.Healed && currVal > prevVal)
            {
                healEvents++;
                healthIndicators++;
            }

            // Health values rarely go negative
            if (currVal < 0)
            {
                score -= 0.3;
            }

            // Health values should stay within reasonable bounds
            if (currVal > 100000)
            {
                score -= 0.2;
            }
        }

        // Bonus for multiple consistent health indicators
        if (damageEvents >= 2)
            score += 0.2;
        if (healEvents >= 1)
            score += 0.15;

        // Bonus for consistent range behavior
        if (healthIndicators >= 3)
            score += 0.15;

        // Check for max health proximity pattern (if we have enough observations)
        if (history.Count > 5 && HasMaxValuePattern(history))
        {
            score += 0.1;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "float" or "single" or "int16" or "short" or "int64" or "long";
    }

    private static bool IsInHealthRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Health typically in ranges: 1-100, 1-1000, 1-10000
            var val = doubleValue.Value;
            return (val >= 1 && val <= 10000) || (val >= 1.0 && val <= 1000.0);
        }
        catch
        {
            return false;
        }
    }

    private static bool HasMaxValuePattern(List<ValueObservation> history)
    {
        // Look for values that frequently hit the same max value
        var values = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToList();

        if (values.Count < 3) return false;

        var maxValue = values.Max();
        var timesAtMax = values.Count(v => Math.Abs(v - maxValue) < 0.01);

        // If value hits max frequently, might indicate health with max value
        return timesAtMax >= 2;
    }
}
