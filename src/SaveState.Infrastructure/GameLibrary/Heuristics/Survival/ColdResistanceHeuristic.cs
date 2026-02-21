using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting cold resistance/insulation values in survival games.
/// Cold resistance values typically:
/// - Are floats or integers (0.0-100.0)
/// - Based on clothing, gear, and adaptations
/// - Reduce temperature loss rate in cold environments
/// - Stack with multiple clothing items
/// </summary>
public sealed class ColdResistanceHeuristic : IValueHeuristic
{
    public string Name => "Cold Resistance Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int gearChangeEvents = 0;
        int environmentalTests = 0;
        bool stepwiseIncreasePattern = false;

        // Check value range (cold resistance typically 0-100)
        if (IsInColdResistanceRange(value.CurrentValue))
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

            // Check for gear changes (stepwise increases)
            if (currVal > prevVal && (curr.RelatedAction == PlayerAction.UsedItem || 
                                       curr.RelatedAction == PlayerAction.Moved))
            {
                var delta = currVal.Value - prevVal.Value;
                // Equipping cold gear adds resistance in steps
                if (delta > 5 && delta < 30)
                {
                    gearChangeEvents++;
                    stepwiseIncreasePattern = true;
                    score += 0.15;
                }
            }

            // Check for gear removal (stepwise decreases)
            if (currVal < prevVal)
            {
                var delta = prevVal.Value - currVal.Value;
                // Unequipping removes resistance
                if (delta > 5 && delta < 30)
                {
                    gearChangeEvents++;
                    stepwiseIncreasePattern = true;
                    score += 0.1;
                }
            }

            // Check for effectiveness in cold (value stays high when it should drop)
            if (currVal > 50 && curr.RelatedAction == PlayerAction.Idle)
            {
                environmentalTests++;
                score += 0.05;
            }

            // Cold resistance should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Cold resistance typically caps at 100
            if (currVal > 100)
            {
                score -= 0.3;
            }

            // Check for typical resistance values (clothing tiers)
            if (currVal == 0 || currVal == 10 || currVal == 25 || currVal == 50 || 
                currVal == 75 || currVal == 100)
            {
                score += 0.08;
            }
        }

        // Bonus for gear change events
        if (gearChangeEvents >= 2)
            score += 0.15;

        // Strong bonus for stepwise pattern (distinctive of gear-based stats)
        if (stepwiseIncreasePattern)
            score += 0.2;

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

    private static bool IsInColdResistanceRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Cold resistance typically in range 0-100
            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}