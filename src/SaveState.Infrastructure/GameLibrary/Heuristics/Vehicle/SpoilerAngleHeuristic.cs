using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting active spoiler/wing angle in driving/racing games.
/// Spoiler angle values typically:
/// - Are floats (0-45 degrees) or integers
/// - 0 = retracted, higher values = more aggressive
/// - Change dynamically with speed (DRS, active aero)
/// - Found on performance vehicles
/// </summary>
public sealed class SpoilerAngleHeuristic : IValueHeuristic
{
    public string Name => "Spoiler Angle Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasDynamicChange = false;
        bool hasRetractedState = false;
        bool hasDeployedState = false;

        // Check value range (spoiler angle: 0-90 degrees, typically 0-45)
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue && currentVal.Value >= 0 && currentVal.Value <= 90)
        {
            score += 0.4;
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

            // Check for retracted state (0 degrees)
            if (currVal.Value < 5)
            {
                hasRetractedState = true;
                score += 0.1;
            }

            // Check for deployed state (active aero)
            if (currVal.Value > 10)
            {
                hasDeployedState = true;
                score += 0.1;
            }

            // Check for dynamic changes (DRS, active aero)
            if (prevVal.Value != currVal.Value)
            {
                hasDynamicChange = true;
                var delta = Math.Abs(currVal.Value - prevVal.Value);
                if (delta > 5 && delta < 45)
                {
                    score += 0.1;
                }

                // Often changes during high speed
                if (curr.RelatedAction == PlayerAction.Sprinted)
                {
                    score += 0.1;
                }
            }

            // Should not be negative
            if (currVal.Value < 0)
            {
                score -= 0.4;
            }

            // Should not exceed 90 degrees
            if (currVal.Value > 90)
            {
                score -= 0.3;
            }
        }

        // Bonus for dynamic changes (characteristic of active aero)
        if (hasDynamicChange && history.Count > 2)
            score += 0.15;

        // Bonus for retracted state detection
        if (hasRetractedState)
            score += 0.1;

        // Bonus for deployed state
        if (hasDeployedState)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType) => valueType.ToLowerInvariant() is "float" or "single" or "double" or "int32" or "int";
}