using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting sanity/mental health in horror games.
/// Sanity values typically:
/// - Are floats (0.0-100.0) starting at maximum
/// - Decrease when witnessing horror events
/// - Recover slowly in safe areas
/// </summary>
public sealed class SanityHeuristic : IValueHeuristic
{
    public string Name => "Sanity/Mental Health Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int decreaseEvents = 0;
        int recoveryEvents = 0;

        // Check value range (sanity typically 0-100)
        if (IsInSanityRange(value.CurrentValue))
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

            // Check for decrease (horror events)
            if (currVal < prevVal)
            {
                decreaseEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Horror causes significant sanity loss
                if (delta >= 5 && delta <= 30)
                {
                    score += 0.15;
                }
            }

            // Check for recovery (safe areas)
            if (currVal > prevVal && curr.RelatedAction == PlayerAction.Idle)
            {
                recoveryEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Recovery is slow
                if (delta > 0 && delta < 5)
                {
                    score += 0.1;
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
        if (decreaseEvents >= 2)
            score += 0.15;
        if (recoveryEvents >= 1)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "int32" or "int" or "double";
    }

    private static bool IsInSanityRange(object? value)
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