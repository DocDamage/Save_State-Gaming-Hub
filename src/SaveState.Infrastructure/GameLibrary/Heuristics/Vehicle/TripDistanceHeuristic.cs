using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting trip distance in driving/racing games.
/// Trip distance values typically:
/// - Are floats (kilometers or miles)
/// - Can be reset to 0
/// - Accumulate during driving
/// - Smaller values than odometer
/// </summary>
public sealed class TripDistanceHeuristic : IValueHeuristic
{
    public string Name => "Trip Distance Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasReset = false;
        bool increasesWithMovement = false;

        // Check value range (0 to 9999 km/miles typical)
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue && currentVal.Value >= 0 && currentVal.Value <= 10000)
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

            // Trip can be reset (drops to 0)
            if (currVal.Value < prevVal.Value && currVal.Value < 1)
            {
                hasReset = true;
                score += 0.2;
            }

            // Increases with movement
            if (currVal > prevVal && curr.RelatedAction == PlayerAction.Moved)
            {
                increasesWithMovement = true;
                var delta = currVal.Value - prevVal.Value;
                if (delta > 0 && delta < 5)
                {
                    score += 0.15;
                }
            }

            // Values should be non-negative
            if (currVal.Value >= 0)
            {
                score += 0.05;
            }

            // Should not be negative
            if (currVal.Value < 0)
            {
                score -= 0.5;
            }

            // Typically smaller than odometer
            if (currVal.Value > 50000)
            {
                score -= 0.2;
            }
        }

        // Bonus for reset detection (characteristic of trip)
        if (hasReset)
            score += 0.15;

        // Bonus for movement correlation
        if (increasesWithMovement)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType) => valueType.ToLowerInvariant() is "float" or "single" or "double" or "int32" or "int";
}