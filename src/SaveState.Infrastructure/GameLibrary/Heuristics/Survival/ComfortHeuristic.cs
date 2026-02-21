using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting comfort/well-being values in survival games.
/// Comfort values typically:
/// - Are floats or integers (0.0-100.0)
/// - Decrease when exposed to harsh conditions
/// - Increase when resting in comfortable environments
/// - Affects sanity, rest quality, and overall performance
/// </summary>
public sealed class ComfortHeuristic : IValueHeuristic
{
    public string Name => "Comfort Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int comfortImprovements = 0;
        int comfortDecreases = 0;
        bool shelterCorrelation = false;

        // Check value range (comfort typically 0-100)
        if (IsInComfortRange(value.CurrentValue))
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

            // Check for comfort increase in safe environments (idle)
            if (currVal > prevVal && curr.RelatedAction == PlayerAction.Idle)
            {
                var delta = currVal.Value - prevVal.Value;
                // Comfort improves gradually when resting
                if (delta > 0 && delta < 5)
                {
                    comfortImprovements++;
                    score += 0.1;
                }
                // Larger jumps from using comfort items
                else if (delta >= 5 && delta < 30)
                {
                    comfortImprovements++;
                    score += 0.15;
                }
            }

            // Check for comfort decrease during harsh activities
            if (currVal < prevVal && (curr.RelatedAction == PlayerAction.Sprinted ||
                                       curr.RelatedAction == PlayerAction.Attacked))
            {
                var delta = prevVal.Value - currVal.Value;
                if (delta > 0)
                {
                    comfortDecreases++;
                    score += 0.08;
                }
            }

            // Comfort should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Comfort typically caps at 100
            if (currVal > 100)
            {
                score -= 0.3;
            }

            // Comfort is often expressed in pleasant ranges
            if (currVal >= 50 && currVal <= 100)
            {
                score += 0.05;
            }
        }

        // Bonus for comfort improvements
        if (comfortImprovements >= 2)
            score += 0.15;

        // Bonus for decrease pattern (shows it responds to stress)
        if (comfortDecreases >= 1)
            score += 0.1;

        // Check for max value near 100
        var maxValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Max();

        if (Math.Abs(maxValue - 100) < 5)
        {
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "int32" or "int" or "double" or "int16" or "short";
    }

    private static bool IsInComfortRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Comfort typically in range 0-100
            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}