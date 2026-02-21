using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting vehicle fuel values in driving/racing games.
/// Fuel values typically:
/// - Are floats or integers representing liters/gallons or percentage
/// - Decrease when vehicle is moving
/// - Can be refilled at gas stations
/// - Critical for vehicle operation
/// </summary>
public sealed class FuelHeuristic : IValueHeuristic
{
    public string Name => "Vehicle Fuel Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int consumptionEvents = 0;
        int refuelEvents = 0;
        bool hasMovementConsumption = false;

        // Check value range (fuel typically 0-100 or 0-1000 liters)
        if (IsInFuelRange(value.CurrentValue))
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

            // Check for fuel consumption during movement
            if (currVal < prevVal && (curr.RelatedAction == PlayerAction.Sprinted || 
                                       curr.RelatedAction == PlayerAction.Moved))
            {
                consumptionEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Fuel consumption is typically gradual
                if (delta > 0 && delta < 5)
                {
                    hasMovementConsumption = true;
                    score += 0.1;
                }
            }

            // Check for refueling (significant increase)
            if (currVal > prevVal)
            {
                var delta = currVal.Value - prevVal.Value;
                // Refueling adds significant amount
                if (delta > 10)
                {
                    refuelEvents++;
                    score += 0.15;
                }
            }

            // Fuel values should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Fuel values typically cap at 100 (percentage) or tank capacity
            if (currVal > 2000)
            {
                score -= 0.3;
            }
        }

        // Bonus for movement consumption pattern
        if (hasMovementConsumption && consumptionEvents >= 2)
            score += 0.2;

        // Bonus for refuel events
        if (refuelEvents >= 1)
            score += 0.15;

        // Check for common max values (100 for percentage, or typical tank sizes)
        var maxValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Max();

        // Common fuel max values: 100 (percentage), 50-100 (liters)
        var commonCaps = new[] { 100.0, 50.0, 60.0, 70.0, 80.0 };
        foreach (var cap in commonCaps)
        {
            if (Math.Abs(maxValue - cap) < 5)
            {
                score += 0.1;
                break;
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "int32" or "int" or "double" or "int16" or "short";
    }

    private static bool IsInFuelRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Fuel typically in range 0-1000 (liters or percentage)
            var val = doubleValue.Value;
            return val >= 0 && val <= 1000;
        }
        catch
        {
            return false;
        }
    }
}