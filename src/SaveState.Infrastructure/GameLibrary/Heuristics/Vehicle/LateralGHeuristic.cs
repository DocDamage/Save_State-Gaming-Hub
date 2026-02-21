using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting lateral G-force (cornering) in driving/racing games.
/// Lateral G values typically:
/// - Are floats (-3.0 to +3.0 Gs)
/// - 0 = driving straight
/// - Negative = left turn, positive = right turn
/// - Peak during hard cornering
/// </summary>
public sealed class LateralGHeuristic : IValueHeuristic
{
    public string Name => "Lateral G-Force Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasZeroStraight = false;
        bool hasBilateral = false;
        bool hasCorneringGs = false;

        // Check value range (Lateral G: -5.0 to +5.0)
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue && currentVal.Value >= -5 && currentVal.Value <= 5)
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

            // Near zero when driving straight
            if (Math.Abs(currVal.Value) < 0.2)
            {
                hasZeroStraight = true;
                score += 0.1;
            }

            // High Gs during movement (cornering)
            if (Math.Abs(currVal.Value) > 1.0 && curr.RelatedAction == PlayerAction.Moved)
            {
                hasCorneringGs = true;
                score += 0.15;
            }

            // Check for bilateral (left and right turns)
            if (currVal.Value < 0 && maxVal > 0)
            {
                hasBilateral = true;
            }
            if (currVal.Value > 0 && minVal < 0)
            {
                hasBilateral = true;
            }

            // Should be within realistic bounds
            if (Math.Abs(currVal.Value) > 5)
            {
                score -= 0.4;
            }
        }

        // Bonus for zero when straight
        if (hasZeroStraight)
            score += 0.15;

        // Bonus for cornering Gs
        if (hasCorneringGs)
            score += 0.15;

        // Bonus for bilateral turns
        if (hasBilateral)
            score += 0.2;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType) => valueType.ToLowerInvariant() is "float" or "single" or "double" or "int32" or "int";
}