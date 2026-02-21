using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting noise level values in survival games.
/// Noise level values typically:
/// - Are floats or integers (0.0-100.0 representing decibels or percentage)
/// - Increase when moving, using items, or performing actions
/// - Attract enemies or wildlife when too high
/// - Decrease when remaining still and quiet
/// </summary>
public sealed class NoiseLevelHeuristic : IValueHeuristic
{
    public string Name => "Noise Level Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int actionSpikes = 0;
        int quietPeriods = 0;
        bool actionCorrelation = false;

        // Check value range (noise typically 0-100)
        if (IsInNoiseRange(value.CurrentValue))
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

            // Check for noise spikes during actions
            if (currVal > prevVal && (curr.RelatedAction == PlayerAction.Attacked ||
                                       curr.RelatedAction == PlayerAction.Sprinted ||
                                       curr.RelatedAction == PlayerAction.UsedItem))
            {
                var delta = currVal.Value - prevVal.Value;
                if (delta > 10)
                {
                    actionSpikes++;
                    score += 0.15;
                }
            }

            // Check for quiet periods during idle
            if (currVal < prevVal && curr.RelatedAction == PlayerAction.Idle)
            {
                var delta = prevVal.Value - currVal.Value;
                // Noise decreases when quiet
                if (delta > 0)
                {
                    quietPeriods++;
                    score += 0.08;
                }
            }

            // Check for low baseline when idle
            if (curr.RelatedAction == PlayerAction.Idle && currVal < 20)
            {
                score += 0.05;
            }

            // Noise should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Noise typically caps at 100
            if (currVal > 100)
            {
                score -= 0.3;
            }
        }

        // Strong bonus for action spikes
        if (actionSpikes >= 2)
        {
            score += 0.2;
            actionCorrelation = true;
        }

        // Bonus for quiet periods
        if (quietPeriods >= 3)
            score += 0.15;

        // Check for typical noise pattern (spikes return to baseline)
        var maxValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Max();

        var minValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Min();

        // Noise should vary significantly between quiet and loud
        if (maxValue - minValue > 30)
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

    private static bool IsInNoiseRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Noise typically in range 0-100
            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}