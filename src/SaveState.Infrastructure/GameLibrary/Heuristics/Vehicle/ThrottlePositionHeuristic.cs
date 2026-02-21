using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting throttle position percentage in driving/racing games.
/// Throttle position values typically:
/// - Are floats (0.0-100.0) or normalized (0.0-1.0)
/// - 0 = closed, 100 = wide open throttle (WOT)
/// - Correlate with acceleration
/// - Change rapidly with input
/// </summary>
public sealed class ThrottlePositionHeuristic : IValueHeuristic
{
    public string Name => "Throttle Position Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasClosedPosition = false;
        bool hasWideOpen = false;
        bool hasDynamicRange = false;

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

        // Analyze observation history
        double minVal = double.MaxValue;
        double maxVal = double.MinValue;

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

            minVal = Math.Min(minVal, currVal.Value);
            maxVal = Math.Max(maxVal, currVal.Value);

            // Check for closed throttle
            if (currVal.Value < 5 || currVal.Value < 0.05)
            {
                hasClosedPosition = true;
                score += 0.1;
            }

            // Check for wide open throttle
            if (currVal.Value > 90 || currVal.Value > 0.9)
            {
                hasWideOpen = true;
                score += 0.1;
            }

            // Rapid changes with sprinting
            if (curr.RelatedAction == PlayerAction.Sprinted)
            {
                var delta = Math.Abs(currVal.Value - prevVal.Value);
                if (delta > 10 || delta > 0.1)
                {
                    score += 0.1;
                }
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

        // Check for full range (0 to 100)
        if ((minVal < 5 && maxVal > 95) || (minVal < 0.05 && maxVal > 0.95))
        {
            hasDynamicRange = true;
            score += 0.25;
        }

        // Bonus for closed position
        if (hasClosedPosition)
            score += 0.1;

        // Bonus for WOT
        if (hasWideOpen)
            score += 0.1;

        // Bonus for full range
        if (hasDynamicRange)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType) => valueType.ToLowerInvariant() is "float" or "single" or "double" or "int32" or "int";
}