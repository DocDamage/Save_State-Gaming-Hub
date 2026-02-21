using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting clutch engagement percentage in driving/racing games.
/// Clutch engagement values typically:
/// - Are floats (0.0-1.0) or integers (0-100) representing percentage
/// - 0 = fully disengaged, 1.0/100 = fully engaged
/// - Change with gear shifts
/// - Correlate with RPM changes
/// </summary>
public sealed class ClutchEngagementHeuristic : IValueHeuristic
{
    public string Name => "Clutch Engagement Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasPartialEngagement = false;
        bool hasFullRange = false;

        // Check value range (0.0-1.0 or 0-100)
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue)
        {
            if (currentVal.Value >= 0 && currentVal.Value <= 1.0)
            {
                score += 0.35; // Normalized 0-1 range
            }
            else if (currentVal.Value >= 0 && currentVal.Value <= 100)
            {
                score += 0.35; // Percentage 0-100 range
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

            // Check for partial engagement (shifting)
            if ((currVal.Value > 0 && currVal.Value < 1.0) ||
                (currVal.Value > 0 && currVal.Value < 100))
            {
                hasPartialEngagement = true;
                score += 0.1;
            }

            // Check for rapid changes during shifting
            var delta = Math.Abs(currVal.Value - prevVal.Value);
            if (delta > 0.2 && delta < 1.0)
            {
                score += 0.05;
            }

            // Should be bounded 0-1 or 0-100
            if (currVal.Value < 0 || currVal.Value > 100)
            {
                score -= 0.5;
            }

            // If normalized, check bounds
            if (maxVal <= 1.0 && (currVal.Value < 0 || currVal.Value > 1.0))
            {
                score -= 0.5;
            }
        }

        // Check for full range coverage (disengaged to engaged)
        if (minVal <= 0.05 && maxVal >= 0.95)
        {
            hasFullRange = true;
            score += 0.25;
        }
        else if (minVal <= 5 && maxVal >= 95)
        {
            hasFullRange = true;
            score += 0.25;
        }

        // Bonus for partial engagement detection
        if (hasPartialEngagement)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType) => valueType.ToLowerInvariant() is "float" or "single" or "double" or "int32" or "int";
}