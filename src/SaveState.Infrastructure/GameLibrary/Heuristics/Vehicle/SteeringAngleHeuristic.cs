using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting steering wheel angle in driving/racing games.
/// Steering angle values typically:
/// - Are floats (-540 to +540 degrees, representing lock-to-lock)
/// - 0 = centered, negative = left, positive = right
/// - Change with player input
/// - Return to 0 when released (steering centering)
/// </summary>
public sealed class SteeringAngleHeuristic : IValueHeuristic
{
    public string Name => "Steering Angle Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasCenteredState = false;
        bool hasFullLock = false;
        bool hasBilateral = false;

        // Check value range (steering: -720 to +720 degrees)
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue && currentVal.Value >= -720 && currentVal.Value <= 720)
        {
            score += 0.35;
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

            // Check for centered state (0 degrees)
            if (Math.Abs(currVal.Value) < 5)
            {
                hasCenteredState = true;
                score += 0.1;
            }

            // Check for full lock
            if (Math.Abs(currVal.Value) > 360)
            {
                hasFullLock = true;
                score += 0.1;
            }

            // Check for bilateral (positive and negative values)
            if (currVal.Value < 0 && maxVal > 0)
            {
                hasBilateral = true;
            }
            if (currVal.Value > 0 && minVal < 0)
            {
                hasBilateral = true;
            }

            // Steering changes with movement/action
            var delta = Math.Abs(currVal.Value - prevVal.Value);
            if (delta > 0 && delta < 180)
            {
                score += 0.05;
            }

            // Should not exceed typical max lock
            if (Math.Abs(currVal.Value) > 1080)
            {
                score -= 0.4;
            }
        }

        // Bonus for centered state
        if (hasCenteredState)
            score += 0.15;

        // Bonus for full lock detection
        if (hasFullLock)
            score += 0.1;

        // Bonus for bilateral (left and right steering)
        if (hasBilateral)
            score += 0.2;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType) => valueType.ToLowerInvariant() is "float" or "single" or "double" or "int32" or "int";
}