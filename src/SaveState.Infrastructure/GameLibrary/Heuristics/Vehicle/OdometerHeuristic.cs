using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting odometer reading in driving/racing games.
/// Odometer values typically:
/// - Are integers or floats (kilometers or miles)
/// - Start at 0 for new vehicles
/// - Only increase with driving
/// - Never reset (permanent)
/// </summary>
public sealed class OdometerHeuristic : IValueHeuristic
{
    public string Name => "Odometer Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool isNonNegative = false;
        bool neverDecreases = true;
        bool increasesWithMovement = false;

        // Check value range (0 to millions of km/miles)
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue && currentVal.Value >= 0)
        {
            score += 0.3;
            isNonNegative = true;
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

            // Should only increase or stay same
            if (currVal < prevVal)
            {
                neverDecreases = false;
                score -= 0.4;
            }

            // Increases with movement
            if (currVal > prevVal && curr.RelatedAction == PlayerAction.Moved)
            {
                increasesWithMovement = true;
                var delta = currVal.Value - prevVal.Value;
                if (delta > 0 && delta < 10) // Small increments
                {
                    score += 0.15;
                }
            }

            // Values should be non-negative
            if (currVal.Value >= 0)
            {
                score += 0.05;
            }

            // Check for realistic odometer values
            if (currVal.Value >= 0 && currVal.Value <= 1000000)
            {
                score += 0.05;
            }

            // Should not be negative
            if (currVal.Value < 0)
            {
                score -= 0.5;
            }
        }

        // Bonus for non-negative
        if (isNonNegative)
            score += 0.1;

        // Bonus for never decreasing
        if (neverDecreases && history.Count > 3)
            score += 0.2;

        // Bonus for movement correlation
        if (increasesWithMovement)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType) => valueType.ToLowerInvariant() is "float" or "single" or "double" or "int32" or "int";
}