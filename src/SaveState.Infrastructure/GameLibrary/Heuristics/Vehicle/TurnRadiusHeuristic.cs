using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting turn radius in driving/racing games.
/// Turn radius values typically:
/// - Are static floats (2-15 meters for cars)
/// - Smaller for nimble vehicles, larger for trucks
/// - Remain constant per vehicle
/// - Affect handling and maneuverability
/// </summary>
public sealed class TurnRadiusHeuristic : IValueHeuristic
{
    public string Name => "Turn Radius Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool isStable = true;

        // Check value range (turn radius: 2-20 meters)
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue)
        {
            if (currentVal.Value >= 2 && currentVal.Value <= 7)
            {
                score += 0.45; // Sports cars
            }
            else if (currentVal.Value > 7 && currentVal.Value <= 15)
            {
                score += 0.4; // Typical cars
            }
            else if (currentVal.Value > 15 && currentVal.Value <= 25)
            {
                score += 0.35; // Trucks/buses
            }
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

            // Turn radius is static per vehicle
            if (!HeuristicUtilities.AreValuesEqual(prevVal.Value, currVal.Value))
            {
                var delta = Math.Abs(currVal.Value - prevVal.Value);
                if (delta > 0.5 && i > 1)
                {
                    isStable = false;
                }
            }

            // Check for common turn radius values
            var commonRadius = new[] { 3.0, 4.0, 5.0, 5.5, 6.0, 7.0, 8.0, 9.0, 10.0, 12.0 };
            foreach (var radius in commonRadius)
            {
                if (Math.Abs(currVal.Value - radius) < 0.5)
                {
                    score += 0.1;
                    break;
                }
            }

            // Should not be negative
            if (currVal.Value < 0)
            {
                score -= 0.5;
            }

            // Should not exceed 30m (buses/trucks)
            if (currVal.Value > 30)
            {
                score -= 0.3;
            }
        }

        // Bonus for stability
        if (isStable && history.Count > 3)
            score += 0.25;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType) => valueType.ToLowerInvariant() is "float" or "single" or "double" or "int32" or "int";
}