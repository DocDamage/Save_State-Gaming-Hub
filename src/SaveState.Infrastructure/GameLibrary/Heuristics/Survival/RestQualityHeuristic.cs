using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting rest/sleep quality values in survival games.
/// Rest quality values typically:
/// - Are floats or integers (0.0-100.0)
/// - Represent how restorative sleep/rest was
/// - Depend on comfort, environment, and sleep duration
/// - Affect fatigue recovery and next-day performance
/// </summary>
public sealed class RestQualityHeuristic : IValueHeuristic
{
    public string Name => "Rest Quality Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int restEvents = 0;
        int qualityVariations = 0;
        bool postRestPattern = false;

        // Check value range (rest quality typically 0-100)
        if (IsInRestQualityRange(value.CurrentValue))
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

            // Check for rest quality after sleeping/resting
            if (curr.RelatedAction == PlayerAction.Idle && 
                prev.RelatedAction == PlayerAction.Moved &&
                currVal > 0)
            {
                restEvents++;
                score += 0.15;
            }

            // Check for quality variation (different rest conditions)
            if (i >= 2)
            {
                var beforePrev = history[i - 2];
                if (beforePrev.Value != null)
                {
                    double? beforePrevVal = HeuristicUtilities.ConvertToDouble(beforePrev.Value);
                    if (beforePrevVal.HasValue && Math.Abs(currVal.Value - beforePrevVal.Value) > 15)
                    {
                        qualityVariations++;
                        score += 0.1;
                    }
                }
            }

            // Check for typical rest quality values
            if (currVal >= 20 && currVal <= 100)
            {
                score += 0.05;
            }

            // Rest quality should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Rest quality typically caps at 100
            if (currVal > 100)
            {
                score -= 0.3;
            }
        }

        // Strong bonus for rest events
        if (restEvents >= 1)
            score += 0.2;

        // Bonus for quality variations (shows environmental sensitivity)
        if (qualityVariations >= 2)
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
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "int32" or "int" or "double" or "int16" or "short";
    }

    private static bool IsInRestQualityRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Rest quality typically in range 0-100
            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}