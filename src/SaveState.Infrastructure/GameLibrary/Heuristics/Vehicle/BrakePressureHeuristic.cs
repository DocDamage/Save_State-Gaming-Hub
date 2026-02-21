using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting brake pressure/hydraulic pressure in driving/racing games.
/// Brake pressure values typically:
/// - Are floats (0-200+ bar or PSI)
/// - 0 when brakes released
/// - Peak during hard braking
/// - Correlate with deceleration
/// </summary>
public sealed class BrakePressureHeuristic : IValueHeuristic
{
    public string Name => "Brake Pressure Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool startsAtZero = false;
        bool hasBrakingEvents = false;

        // Check value range (0-250 bar or PSI)
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue && currentVal.Value >= 0 && currentVal.Value <= 250)
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

            // Pressure builds when braking
            if (currVal > prevVal && currVal.Value > 20)
            {
                hasBrakingEvents = true;
                score += 0.15;
            }

            // Rapid pressure changes
            var delta = Math.Abs(currVal.Value - prevVal.Value);
            if (delta > 10 && delta < 100)
            {
                score += 0.1;
            }

            // Returns to zero when released
            if (currVal.Value < 5 && prevVal.Value > 10)
            {
                score += 0.1;
            }

            // Should not be negative
            if (currVal.Value < 0)
            {
                score -= 0.5;
            }

            // Check for realistic max values
            if (currVal.Value > 50 && currVal.Value <= 200)
            {
                score += 0.05;
            }
        }

        // Bonus for zero start
        if (startsAtZero)
            score += 0.1;

        // Bonus for braking events
        if (hasBrakingEvents)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType) => valueType.ToLowerInvariant() is "float" or "single" or "double" or "int32" or "int";
}