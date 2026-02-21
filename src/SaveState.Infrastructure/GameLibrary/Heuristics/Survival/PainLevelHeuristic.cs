using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting pain level values in survival games.
/// Pain values typically:
/// - Are floats or integers (0.0-100.0 or 0-10 scale)
/// - Increase with injuries and wounds
/// - Decrease with painkillers and healing
/// - Affects movement speed and accuracy
/// </summary>
public sealed class PainLevelHeuristic : IValueHeuristic
{
    public string Name => "Pain Level Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int injuryEvents = 0;
        int painkillerEvents = 0;
        bool gradualIncreasePattern = false;

        // Check value range (pain: 0-100 or 0-10)
        if (IsInPainLevelRange(value.CurrentValue))
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

            // Check for pain from injury
            if (currVal > prevVal && curr.RelatedAction == PlayerAction.Attacked)
            {
                var delta = currVal.Value - prevVal.Value;
                // Injuries cause immediate pain
                if (delta > 10 && delta < 60)
                {
                    injuryEvents++;
                    score += 0.2;
                }
            }

            // Check for pain increase from existing wounds
            if (currVal > prevVal && curr.RelatedAction == PlayerAction.Idle)
            {
                var delta = currVal.Value - prevVal.Value;
                // Pain can worsen from untreated injuries
                if (delta > 0 && delta < 5)
                {
                    gradualIncreasePattern = true;
                    score += 0.1;
                }
            }

            // Check for painkiller use
            if (currVal < prevVal && curr.RelatedAction == PlayerAction.UsedItem)
            {
                var delta = prevVal.Value - currVal.Value;
                // Painkillers reduce pain significantly
                if (delta > 15 && delta < 80)
                {
                    painkillerEvents++;
                    score += 0.22;
                }
            }

            // Check for healing reducing pain
            if (currVal < prevVal && curr.RelatedAction == PlayerAction.Healed)
            {
                var delta = prevVal.Value - currVal.Value;
                if (delta > 5)
                {
                    painkillerEvents++;
                    score += 0.15;
                }
            }

            // Check for 0-10 scale values
            if (currVal >= 0 && currVal <= 10 && HeuristicUtilities.IsIntegerValue(currVal.Value))
            {
                score += 0.1;
            }

            // Pain should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Pain typically caps at 100 or 10
            if (currVal > 100)
            {
                score -= 0.3;
            }
        }

        // Strong bonus for injury events
        if (injuryEvents >= 1)
            score += 0.2;

        // Bonus for painkiller events
        if (painkillerEvents >= 1)
            score += 0.18;

        // Bonus for gradual increase pattern
        if (gradualIncreasePattern)
            score += 0.12;

        // Check for max value
        var maxValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Max();

        // Common caps: 100 (%) or 10 (scale)
        if (Math.Abs(maxValue - 100) < 5 || Math.Abs(maxValue - 10) < 0.5)
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

    private static bool IsInPainLevelRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Pain: 0-100 (%) or 0-10 (scale)
            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}