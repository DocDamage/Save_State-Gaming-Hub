using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting differential lock percentage in driving/racing games.
/// Differential lock values typically:
/// - Are floats (0.0-1.0) or integers (0-100) representing lock percentage
/// - 0 = open diff, 1.0 = fully locked
/// - Used in off-road and racing vehicles
/// - Change based on traction needs
/// </summary>
public sealed class DifferentialLockHeuristic : IValueHeuristic
{
    public string Name => "Differential Lock Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasPartialLock = false;
        bool hasFullLock = false;

        // Check value range (0.0-1.0 or 0-100)
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue)
        {
            if (currentVal.Value >= 0 && currentVal.Value <= 1.0)
            {
                score += 0.35;
            }
            else if (currentVal.Value >= 0 && currentVal.Value <= 100)
            {
                score += 0.35;
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

            // Check for partial lock (traction control)
            if ((currVal.Value > 0.1 && currVal.Value < 0.9) ||
                (currVal.Value > 10 && currVal.Value < 90))
            {
                hasPartialLock = true;
                score += 0.1;
            }

            // Check for fully locked state
            if (currVal.Value >= 0.9 || currVal.Value >= 90)
            {
                hasFullLock = true;
                score += 0.1;
            }

            // Diff lock often engages during off-road or slippery conditions
            if (curr.RelatedAction == PlayerAction.Moved && currVal > prevVal)
            {
                score += 0.05;
            }

            // Should be bounded 0-1 or 0-100
            if (currVal.Value < 0 || currVal.Value > 100)
            {
                score -= 0.5;
            }
        }

        // Bonus for partial lock (characteristic of modern systems)
        if (hasPartialLock)
            score += 0.2;

        // Bonus for full lock detection
        if (hasFullLock)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType) => valueType.ToLowerInvariant() is "float" or "single" or "double" or "int32" or "int";
}