using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting individual wheel steering angle in driving/racing games.
/// Wheel angle values typically:
/// - Are floats (-45 to +45 degrees typical)
/// - Differ between front and rear wheels (4WS)
/// - Change with steering input
/// - Used for Ackermann geometry detection
/// </summary>
public sealed class WheelAngleHeuristic : IValueHeuristic
{
    public string Name => "Wheel Angle Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasCenteredState = false;
        bool hasBilateral = false;

        // Check value range (wheel angle: -45 to +45 degrees)
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue && currentVal.Value >= -45 && currentVal.Value <= 45)
        {
            score += 0.4;
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

            // Check for centered state
            if (Math.Abs(currVal.Value) < 2)
            {
                hasCenteredState = true;
                score += 0.1;
            }

            // Check for bilateral
            if (currVal.Value < 0 && maxVal > 0)
            {
                hasBilateral = true;
            }
            if (currVal.Value > 0 && minVal < 0)
            {
                hasBilateral = true;
            }

            // Changes with movement
            var delta = Math.Abs(currVal.Value - prevVal.Value);
            if (delta > 0 && delta < 20)
            {
                score += 0.05;
            }

            // Should not exceed 60 degrees (typical max)
            if (Math.Abs(currVal.Value) > 60)
            {
                score -= 0.4;
            }
        }

        // Bonus for centered state
        if (hasCenteredState)
            score += 0.15;

        // Bonus for bilateral
        if (hasBilateral)
            score += 0.2;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType) => valueType.ToLowerInvariant() is "float" or "single" or "double" or "int32" or "int";
}