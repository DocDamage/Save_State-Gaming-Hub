using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting sensor health/integrity percentage in driving/racing games.
/// Sensor health values typically:
/// - Are floats (0.0-100.0) or normalized (0.0-1.0)
/// - Start at 100% (or 1.0) for healthy sensors
/// - Decrease with damage or failures
/// - Affect vehicle systems and diagnostics
/// </summary>
public sealed class SensorHealthHeuristic : IValueHeuristic
{
    public string Name => "Sensor Health Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool startsHealthy = false;
        bool hasDegradation = false;

        // Check value range (0-100% or 0.0-1.0)
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue)
        {
            if (currentVal.Value >= 0 && currentVal.Value <= 1.0)
            {
                score += 0.3;
            }
            else if (currentVal.Value >= 0 && currentVal.Value <= 100)
            {
                score += 0.3;
            }
        }

        // Check if starts at healthy value
        if (history.Count > 0)
        {
            var firstVal = HeuristicUtilities.ConvertToDouble(history[0].Value);
            if (firstVal.HasValue && (firstVal.Value >= 95 || firstVal.Value >= 0.95))
            {
                startsHealthy = true;
                score += 0.2;
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

            // Health typically decreases (degrades) or stays same
            if (currVal < prevVal)
            {
                hasDegradation = true;
                var delta = prevVal.Value - currVal.Value;
                if (delta > 0 && delta < 20)
                {
                    score += 0.15;
                }
            }

            // Health decreases with damage/collisions
            if (currVal < prevVal && curr.RelatedAction == PlayerAction.Moved)
            {
                score += 0.1;
            }

            // Values should be in valid range
            if (currVal.Value >= 0 && currVal.Value <= 100)
            {
                score += 0.05;
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

        // Bonus for healthy start
        if (startsHealthy)
            score += 0.15;

        // Bonus for degradation pattern
        if (hasDegradation)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType) => valueType.ToLowerInvariant() is "float" or "single" or "double" or "int32" or "int";
}