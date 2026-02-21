using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting Ackermann steering angle difference in driving/racing games.
/// Ackermann angle values typically:
/// - Are small floats (0-5 degrees difference between inner/outer wheels)
/// - Present when vehicle is turning
/// - Near zero when driving straight
/// - Important for tire wear and handling
/// </summary>
public sealed class AckermannAngleHeuristic : IValueHeuristic
{
    public string Name => "Ackermann Angle Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasZeroWhenStraight = false;
        bool hasSmallAngleWhenTurning = false;

        // Check value range (Ackermann: 0-10 degrees)
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue && currentVal.Value >= 0 && currentVal.Value <= 10)
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

            // Near zero when not turning (straight line)
            if (currVal.Value < 0.5 && curr.RelatedAction != PlayerAction.Moved)
            {
                hasZeroWhenStraight = true;
                score += 0.1;
            }

            // Small non-zero when turning
            if (currVal.Value > 0 && currVal.Value < 8 && curr.RelatedAction == PlayerAction.Moved)
            {
                hasSmallAngleWhenTurning = true;
                score += 0.1;
            }

            // Values should be small
            if (currVal.Value >= 0 && currVal.Value <= 10)
            {
                score += 0.1;
            }

            // Should not be negative
            if (currVal.Value < 0)
            {
                score -= 0.3;
            }

            // Should not exceed 20 degrees
            if (currVal.Value > 20)
            {
                score -= 0.4;
            }
        }

        // Bonus for zero when straight
        if (hasZeroWhenStraight)
            score += 0.15;

        // Bonus for angle when turning
        if (hasSmallAngleWhenTurning)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType) => valueType.ToLowerInvariant() is "float" or "single" or "double" or "int32" or "int";
}