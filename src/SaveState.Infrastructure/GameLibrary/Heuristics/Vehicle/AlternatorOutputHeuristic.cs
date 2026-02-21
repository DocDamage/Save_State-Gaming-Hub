using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting alternator output current/amperage in driving/racing games.
/// Alternator output values typically:
/// - Are floats (0-150+ amps)
/// - 0 when engine off
/// - Higher when electrical load is high
/// - Varies with RPM
/// </summary>
public sealed class AlternatorOutputHeuristic : IValueHeuristic
{
    public string Name => "Alternator Output Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool startsAtZero = false;
        bool hasPositiveOutput = false;

        // Check value range (0-200 amps)
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue && currentVal.Value >= 0 && currentVal.Value <= 200)
        {
            score += 0.35;
        }

        // Check if starts at zero
        if (history.Count > 0)
        {
            var firstVal = HeuristicUtilities.ConvertToDouble(history[0].Value);
            if (firstVal.HasValue && firstVal.Value < 5)
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

            // Positive output when engine running
            if (currVal > 10)
            {
                hasPositiveOutput = true;
                score += 0.1;
            }

            // Output varies with RPM/action
            if (curr.RelatedAction == PlayerAction.Sprinted && currVal > prevVal)
            {
                score += 0.1;
            }

            // Values should be non-negative
            if (currVal.Value >= 0)
            {
                score += 0.05;
            }

            // Check for common output ranges
            if (currVal.Value >= 30 && currVal.Value <= 120)
            {
                score += 0.1;
            }

            // Should not be negative
            if (currVal.Value < 0)
            {
                score -= 0.5;
            }

            // Should not exceed 300 amps (extreme cases)
            if (currVal.Value > 300)
            {
                score -= 0.4;
            }
        }

        // Bonus for zero start (engine off)
        if (startsAtZero)
            score += 0.1;

        // Bonus for positive output
        if (hasPositiveOutput)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType) => valueType.ToLowerInvariant() is "float" or "single" or "double" or "int32" or "int";
}