using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting aerodynamic lift/downforce in driving/racing games.
/// Lift values typically:
/// - Are floats (can be negative for downforce)
/// - Measured in kg, lbs, or Newtons
/// - Change with speed
/// - Higher at racing speeds
/// </summary>
public sealed class LiftHeuristic : IValueHeuristic
{
    public string Name => "Aerodynamic Lift Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasSpeedCorrelation = false;
        bool hasDownforce = false;

        // Check value range (lift: -5000 to +2000 kg/lbs)
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue)
        {
            if (currentVal.Value >= -5000 && currentVal.Value <= 2000)
            {
                score += 0.35;
            }
            // Negative values indicate downforce (common in racing)
            if (currentVal.Value < 0)
            {
                hasDownforce = true;
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

            // Lift typically increases with speed/movement
            if (curr.RelatedAction == PlayerAction.Sprinted && currVal.Value < prevVal.Value)
            {
                hasSpeedCorrelation = true;
                score += 0.1; // More downforce (negative lift) with speed
            }

            // Check for magnitude changes with movement
            var delta = Math.Abs(currVal.Value - prevVal.Value);
            if (delta > 0 && delta < 1000)
            {
                score += 0.05;
            }

            // Extreme values are suspicious
            if (Math.Abs(currVal.Value) > 10000)
            {
                score -= 0.3;
            }
        }

        // Bonus for speed correlation
        if (hasSpeedCorrelation)
            score += 0.2;

        // Bonus for downforce detection (racing cars)
        if (hasDownforce)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType) => valueType.ToLowerInvariant() is "float" or "single" or "double" or "int32" or "int";
}