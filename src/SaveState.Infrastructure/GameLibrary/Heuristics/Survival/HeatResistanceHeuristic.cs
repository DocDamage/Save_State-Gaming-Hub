using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting heat resistance/cooling values in survival games.
/// Heat resistance values typically:
/// - Are floats or integers (0.0-100.0)
/// - Based on clothing breathability, shade, and hydration
/// - Reduce heat accumulation in hot environments
/// - Affects stamina and heatstroke risk
/// </summary>
public sealed class HeatResistanceHeuristic : IValueHeuristic
{
    public string Name => "Heat Resistance Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int gearChangeEvents = 0;
        int coolingEvents = 0;
        bool stepwisePattern = false;

        // Check value range (heat resistance typically 0-100)
        if (IsInHeatResistanceRange(value.CurrentValue))
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

            // Check for gear changes (stepwise changes)
            if (Math.Abs(currVal.Value - prevVal.Value) > 5 && 
                Math.Abs(currVal.Value - prevVal.Value) < 30 &&
                (curr.RelatedAction == PlayerAction.UsedItem || curr.RelatedAction == PlayerAction.Moved))
            {
                gearChangeEvents++;
                stepwisePattern = true;
                score += 0.15;
            }

            // Check for cooling actions
            if (currVal > prevVal && curr.RelatedAction == PlayerAction.Healed)
            {
                var delta = currVal.Value - prevVal.Value;
                // Cooling drinks/shade increase resistance temporarily
                if (delta > 10 && delta < 40)
                {
                    coolingEvents++;
                    score += 0.12;
                }
            }

            // Check for heat resistance decay in extreme heat
            if (currVal < prevVal && curr.RelatedAction == PlayerAction.Sprinted)
            {
                var delta = prevVal.Value - currVal.Value;
                // Overexertion reduces effective heat resistance
                if (delta > 0 && delta < 10)
                {
                    score += 0.08;
                }
            }

            // Heat resistance should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Heat resistance typically caps at 100
            if (currVal > 100)
            {
                score -= 0.3;
            }

            // Check for typical tier values
            if (currVal == 0 || currVal == 10 || currVal == 25 || currVal == 50 || 
                currVal == 75 || currVal == 100)
            {
                score += 0.08;
            }
        }

        // Bonus for gear change events
        if (gearChangeEvents >= 2)
            score += 0.15;

        // Bonus for cooling events
        if (coolingEvents >= 1)
            score += 0.12;

        // Bonus for stepwise pattern
        if (stepwisePattern)
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

    private static bool IsInHeatResistanceRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Heat resistance typically in range 0-100
            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}