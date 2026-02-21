using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting radiation resistance values in survival games.
/// Rad resistance values typically:
/// - Are floats or integers (0.0-100.0 or 0-1000)
/// - Based on protective gear, medications, and shelters
/// - Reduce radiation accumulation rate
/// - Critical in nuclear/post-apocalyptic survival scenarios
/// </summary>
public sealed class RadResistanceHeuristic : IValueHeuristic
{
    public string Name => "Radiation Resistance Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int gearChangeEvents = 0;
        int radZoneEvents = 0;
        bool stepwisePattern = false;

        // Check value range (rad resistance: 0-100 or 0-1000)
        if (IsInRadResistanceRange(value.CurrentValue))
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
                // Equipping rad gear adds resistance
                if (delta > 10 && delta < 50)
                {
                    gearChangeEvents++;
                    stepwisePattern = true;
                    score += 0.18;
                }
            }

            // Check for gear removal
            if (currVal < prevVal)
            {
                var delta = prevVal.Value - currVal.Value;
                if (delta > 10 && delta < 50)
                {
                    gearChangeEvents++;
                    stepwisePattern = true;
                    score += 0.12;
                }
            }

            // Check for high resistance in danger zones (value maintained despite risk)
            if (currVal > 70 && curr.RelatedAction == PlayerAction.Idle)
            {
                radZoneEvents++;
                score += 0.05;
            }

            // Rad resistance should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Rad resistance typically caps at 100 or 1000
            if (currVal > 1000)
            {
                score -= 0.3;
            }

            // Check for typical resistance tier values
            if (currVal == 0 || currVal == 25 || currVal == 50 || currVal == 75 || 
                currVal == 100 || currVal == 250 || currVal == 500)
            {
                score += 0.1;
            }
        }

        // Strong bonus for gear change events
        if (gearChangeEvents >= 2)
            score += 0.2;

        // Bonus for stepwise pattern
        if (stepwisePattern)
            score += 0.15;

        // Check for max value (100 or 1000)
        var maxValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Max();

        if (Math.Abs(maxValue - 100) < 5 || Math.Abs(maxValue - 1000) < 50)
        {
            score += 0.2;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "int32" or "int" or "double" or "int16" or "short";
    }

    private static bool IsInRadResistanceRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Rad resistance typically in range 0-1000
            var val = doubleValue.Value;
            return val >= 0 && val <= 1000;
        }
        catch
        {
            return false;
        }
    }
}