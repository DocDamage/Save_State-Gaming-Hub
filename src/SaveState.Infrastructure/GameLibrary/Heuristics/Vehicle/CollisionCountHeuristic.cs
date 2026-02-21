using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting collision/impact count in driving/racing games.
/// Collision count values typically:
/// - Are integers starting at 0
/// - Increment by 1 per collision
/// - Never decrease
/// - Used for statistics and damage calculations
/// </summary>
public sealed class CollisionCountHeuristic : IValueHeuristic
{
    public string Name => "Collision Count Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool startsAtZero = false;
        bool isInteger = false;
        bool neverDecreases = true;

        // Check value range (0 to high numbers)
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue && currentVal.Value >= 0)
        {
            score += 0.3;

            // Check if integer
            if (HeuristicUtilities.IsIntegerValue(value.CurrentValue) ||
                Math.Abs(currentVal.Value - Math.Round(currentVal.Value)) < 0.001)
            {
                isInteger = true;
                score += 0.2;
            }
        }

        // Check if starts at zero
        if (history.Count > 0)
        {
            var firstVal = HeuristicUtilities.ConvertToDouble(history[0].Value);
            if (firstVal.HasValue && firstVal.Value < 1)
            {
                startsAtZero = true;
                score += 0.15;
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

            // Should only increase or stay same
            if (currVal < prevVal)
            {
                neverDecreases = false;
                score -= 0.3;
            }

            // Increments are typically by 1
            if (currVal > prevVal)
            {
                var delta = currVal.Value - prevVal.Value;
                if (Math.Abs(delta - 1.0) < 0.1)
                {
                    score += 0.15;
                }
                else if (delta > 0 && delta < 5)
                {
                    score += 0.05;
                }
            }

            // Often increases during collisions/movement
            if (currVal > prevVal && curr.RelatedAction == PlayerAction.Moved)
            {
                score += 0.1;
            }

            // Should not be negative
            if (currVal.Value < 0)
            {
                score -= 0.5;
            }
        }

        // Bonus for zero start
        if (startsAtZero)
            score += 0.1;

        // Bonus for integer values
        if (isInteger)
            score += 0.1;

        // Bonus for never decreasing
        if (neverDecreases && history.Count > 3)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType) => valueType.ToLowerInvariant() is "float" or "single" or "double" or "int32" or "int";
}