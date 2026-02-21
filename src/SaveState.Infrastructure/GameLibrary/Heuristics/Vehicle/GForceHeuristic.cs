using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting total G-force in driving/racing games.
/// G-force values typically:
/// - Are floats (0.0-5.0+ Gs typical for racing)
/// - Combine lateral and longitudinal forces
/// - Peak during cornering, braking, and acceleration
/// - 1.0 = normal gravity when stationary
/// </summary>
public sealed class GForceHeuristic : IValueHeuristic
{
    public string Name => "G-Force Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasIdleGs = false;
        bool hasHighGs = false;
        bool hasDynamicChange = false;

        // Check value range (G-force: 0.0-10.0)
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue && currentVal.Value >= 0 && currentVal.Value <= 10)
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

            // Check for idle Gs (~1.0 when stationary)
            if (Math.Abs(currVal.Value - 1.0) < 0.2)
            {
                hasIdleGs = true;
                score += 0.1;
            }

            // Check for high Gs during action
            if (currVal.Value > 2.0 && curr.RelatedAction == PlayerAction.Sprinted)
            {
                hasHighGs = true;
                score += 0.15;
            }

            // Check for dynamic changes
            var delta = Math.Abs(currVal.Value - prevVal.Value);
            if (delta > 0.5 && delta < 5)
            {
                hasDynamicChange = true;
                score += 0.1;
            }

            // Should be non-negative
            if (currVal.Value < 0)
            {
                score -= 0.4;
            }

            // Extreme values suspicious
            if (currVal.Value > 10)
            {
                score -= 0.3;
            }
        }

        // Bonus for idle G detection
        if (hasIdleGs)
            score += 0.15;

        // Bonus for high G detection
        if (hasHighGs)
            score += 0.15;

        // Bonus for dynamic changes
        if (hasDynamicChange && history.Count > 3)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType) => valueType.ToLowerInvariant() is "float" or "single" or "double" or "int32" or "int";
}