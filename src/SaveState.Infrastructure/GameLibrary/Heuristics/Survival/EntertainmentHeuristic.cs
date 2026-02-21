using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting entertainment/morale values in survival games.
/// Entertainment values typically:
/// - Are floats or integers (0.0-100.0)
/// - Decrease during monotonous activities
/// - Increase through recreational activities or items
/// - Affects sanity, motivation, and mental well-being
/// </summary>
public sealed class EntertainmentHeuristic : IValueHeuristic
{
    public string Name => "Entertainment Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int recreationEvents = 0;
        int boredomDecay = 0;
        bool itemUsageCorrelation = false;

        // Check value range (entertainment typically 0-100)
        if (IsInEntertainmentRange(value.CurrentValue))
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

            // Check for entertainment boost from recreational items/activities
            if (currVal > prevVal && curr.RelatedAction == PlayerAction.UsedItem)
            {
                var delta = currVal.Value - prevVal.Value;
                // Entertainment items provide significant boosts
                if (delta > 10 && delta < 50)
                {
                    recreationEvents++;
                    score += 0.18;
                    itemUsageCorrelation = true;
                }
            }

            // Check for boredom decay during repetitive activities
            if (currVal < prevVal && curr.RelatedAction == PlayerAction.Idle)
            {
                var delta = prevVal.Value - currVal.Value;
                // Boredom increases slowly
                if (delta > 0 && delta < 3)
                {
                    boredomDecay++;
                    score += 0.08;
                }
            }

            // Entertainment should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Entertainment typically caps at 100
            if (currVal > 100)
            {
                score -= 0.3;
            }

            // Check for typical entertainment ranges
            if (currVal >= 10 && currVal <= 90)
            {
                score += 0.05;
            }
        }

        // Strong bonus for recreation events
        if (recreationEvents >= 1)
            score += 0.2;

        // Bonus for item usage correlation
        if (itemUsageCorrelation)
            score += 0.1;

        // Bonus for boredom decay pattern
        if (boredomDecay >= 3)
            score += 0.12;

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

    private static bool IsInEntertainmentRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Entertainment typically in range 0-100
            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}