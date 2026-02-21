using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting weight distribution percentage in driving/racing games.
/// Weight distribution values typically:
/// - Are floats (0-100%) or normalized (0.0-1.0)
/// - Represent front/rear weight bias
/// - 50 = balanced, higher = front-heavy, lower = rear-heavy
/// - Change dynamically during braking/acceleration
/// </summary>
public sealed class WeightDistributionHeuristic : IValueHeuristic
{
    public string Name => "Weight Distribution Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasDynamicChange = false;
        bool hasRealisticRange = false;

        // Check value range (0-100% or 0.0-1.0)
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue)
        {
            if (currentVal.Value >= 0 && currentVal.Value <= 1.0)
            {
                score += 0.3;
                hasRealisticRange = true;
            }
            else if (currentVal.Value >= 0 && currentVal.Value <= 100)
            {
                score += 0.3;
                hasRealisticRange = true;
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

            // Weight shifts during acceleration/braking
            if (prevVal.Value != currVal.Value)
            {
                hasDynamicChange = true;
                var delta = Math.Abs(currVal.Value - prevVal.Value);
                if (delta > 0.01 && delta < 30) // Gradual shift
                {
                    score += 0.1;
                }

                // Shift to rear during acceleration
                if (currVal.Value < prevVal.Value && curr.RelatedAction == PlayerAction.Sprinted)
                {
                    score += 0.15;
                }
            }

            // Check for realistic values (30-70% typical)
            if ((currVal.Value >= 0.3 && currVal.Value <= 0.7) ||
                (currVal.Value >= 30 && currVal.Value <= 70))
            {
                score += 0.1;
            }

            // Should not be negative
            if (currVal.Value < 0)
            {
                score -= 0.5;
            }

            // Should not exceed 100
            if (currVal.Value > 100)
            {
                score -= 0.4;
            }
        }

        // Bonus for dynamic changes (weight transfer)
        if (hasDynamicChange && history.Count > 3)
            score += 0.2;

        // Bonus for realistic range
        if (hasRealisticRange)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType) => valueType.ToLowerInvariant() is "float" or "single" or "double" or "int32" or "int";
}