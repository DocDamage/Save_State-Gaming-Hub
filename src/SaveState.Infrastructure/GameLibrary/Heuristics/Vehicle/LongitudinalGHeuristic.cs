using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting longitudinal G-force (acceleration/braking) in driving/racing games.
/// Longitudinal G values typically:
/// - Are floats (-2.0 to +2.0 Gs, with extremes up to 5+ for race cars)
/// - Negative = braking, positive = acceleration
/// - 0 = coasting
/// - Peak during hard braking or launch
/// </summary>
public sealed class LongitudinalGHeuristic : IValueHeuristic
{
    public string Name => "Longitudinal G-Force Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasCoasting = false;
        bool hasAcceleration = false;
        bool hasBraking = false;

        // Check value range (Longitudinal G: -5.0 to +5.0)
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue && currentVal.Value >= -5 && currentVal.Value <= 5)
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

            // Near zero when coasting
            if (Math.Abs(currVal.Value) < 0.1)
            {
                hasCoasting = true;
                score += 0.1;
            }

            // Positive G during acceleration
            if (currVal.Value > 0.3 && curr.RelatedAction == PlayerAction.Sprinted)
            {
                hasAcceleration = true;
                score += 0.15;
            }

            // Negative G during braking
            if (currVal.Value < -0.3)
            {
                hasBraking = true;
                score += 0.15;
            }

            // Rapid changes typical
            var delta = Math.Abs(currVal.Value - prevVal.Value);
            if (delta > 0.2 && delta < 3)
            {
                score += 0.05;
            }

            // Should be within realistic bounds
            if (Math.Abs(currVal.Value) > 6)
            {
                score -= 0.4;
            }
        }

        // Bonus for coasting detection
        if (hasCoasting)
            score += 0.1;

        // Bonus for acceleration detection
        if (hasAcceleration)
            score += 0.15;

        // Bonus for braking detection
        if (hasBraking)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType) => valueType.ToLowerInvariant() is "float" or "single" or "double" or "int32" or "int";
}