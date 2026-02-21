using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting traction control engagement/status in driving/racing games.
/// Traction control values typically:
/// - Are booleans (0/1), integers (0-5 levels), or percentages
/// - 0 = off, higher = more intervention
/// - Active during wheel slip
/// - Reduce power or apply brakes
/// </summary>
public sealed class TractionControlHeuristic : IValueHeuristic
{
    public string Name => "Traction Control Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasIntervention = false;
        bool hasOffState = false;

        // Check value range
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue)
        {
            // Boolean or percentage
            if (currentVal.Value == 0 || currentVal.Value == 1)
            {
                score += 0.3;
            }
            else if (currentVal.Value >= 0 && currentVal.Value <= 100)
            {
                score += 0.3;
            }
            // Mode levels (0-5)
            else if (currentVal.Value >= 0 && currentVal.Value <= 10)
            {
                score += 0.25;
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

            // Check for off state
            if (currVal.Value < 0.1 || currVal.Value == 0)
            {
                hasOffState = true;
                score += 0.1;
            }

            // Active during acceleration
            if (currVal.Value > prevVal.Value && curr.RelatedAction == PlayerAction.Sprinted)
            {
                hasIntervention = true;
                score += 0.15;
            }

            // Values should be bounded
            if (currVal.Value >= 0 && currVal.Value <= 100)
            {
                score += 0.05;
            }

            // Should not be negative
            if (currVal.Value < 0)
            {
                score -= 0.4;
            }

            // Should not exceed 100
            if (currVal.Value > 100)
            {
                score -= 0.3;
            }
        }

        // Bonus for off state detection
        if (hasOffState)
            score += 0.1;

        // Bonus for intervention detection
        if (hasIntervention)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType) => valueType.ToLowerInvariant() is "float" or "single" or "double" or "int32" or "int";
}