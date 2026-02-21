using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting parasite/infestation values in survival games.
/// Parasite values typically:
/// - Are floats or integers (0.0-100.0)
/// - Increase slowly over time once contracted
/// - Cause gradual health/nutrition drain
/// - Require specific treatments to remove
/// - Harder to detect and treat than regular infections
/// </summary>
public sealed class ParasitesHeuristic : IValueHeuristic
{
    public string Name => "Parasites Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int contractionEvents = 0;
        int removalEvents = 0;
        bool slowGrowthPattern = false;

        // Check value range (parasites typically 0-100)
        if (IsInParasitesRange(value.CurrentValue))
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

            // Check for parasite growth (slow increase while idle)
            if (currVal > prevVal && curr.RelatedAction == PlayerAction.Idle)
            {
                var delta = currVal.Value - prevVal.Value;
                // Parasites grow very slowly over time
                if (delta > 0 && delta < 1.5)
                {
                    slowGrowthPattern = true;
                    score += 0.12;
                }
            }

            // Check for initial contraction (sudden appearance)
            if (currVal > 0 && prevVal == 0)
            {
                contractionEvents++;
                score += 0.15;
            }

            // Check for removal (specific treatment)
            if (currVal < prevVal && curr.RelatedAction == PlayerAction.UsedItem)
            {
                var delta = prevVal.Value - currVal.Value;
                // Parasite removal is typically complete or significant
                if (delta > 20 || currVal == 0)
                {
                    removalEvents++;
                    score += 0.2;
                }
            }

            // Parasites should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Parasites typically cap at 100
            if (currVal > 100)
            {
                score -= 0.3;
            }
        }

        // Strong bonus for slow growth pattern (distinctive of parasites)
        if (slowGrowthPattern)
            score += 0.25;

        // Bonus for removal events
        if (removalEvents >= 1)
            score += 0.15;

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
            score += 0.1;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "int32" or "int" or "double" or "int16" or "short";
    }

    private static bool IsInParasitesRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Parasites typically in range 0-100
            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}