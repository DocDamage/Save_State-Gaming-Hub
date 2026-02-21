using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting turbo/supercharger boost pressure in driving/racing games.
/// Boost pressure values typically:
/// - Are floats (0.0-3.0+ bar or 0-40+ PSI)
/// - Start at 0 and build with RPM
/// - Fluctuate with throttle input
/// - Higher in high-performance turbo vehicles
/// </summary>
public sealed class BoostPressureHeuristic : IValueHeuristic
{
    public string Name => "Boost Pressure Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool startsAtZero = false;
        bool hasFluctuation = false;
        bool hasPressureBuildup = false;

        // Check value range (Boost: 0-4 bar or 0-60 PSI)
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue && currentVal.Value >= 0 && currentVal.Value <= 60)
        {
            score += 0.3;
        }

        // Analyze observation history
        if (history.Count > 0)
        {
            var firstVal = HeuristicUtilities.ConvertToDouble(history[0].Value);
            if (firstVal.HasValue && firstVal.Value < 0.1)
            {
                startsAtZero = true;
                score += 0.15;
            }
        }

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

            // Check for pressure buildup (characteristic of boost)
            if (currVal > prevVal && curr.RelatedAction == PlayerAction.Sprinted)
            {
                hasPressureBuildup = true;
                score += 0.1;
            }

            // Check for fluctuations
            var delta = Math.Abs(currVal.Value - prevVal.Value);
            if (delta > 0.1 && delta < 2.0)
            {
                hasFluctuation = true;
                score += 0.05;
            }

            // Boost drops rapidly when throttle released
            if (currVal < prevVal * 0.5 && delta > 0.5)
            {
                score += 0.1;
            }

            // Should not be negative
            if (currVal.Value < 0)
            {
                score -= 0.5;
            }

            // Realistic max values
            if (currVal.Value > 4 && currVal.Value <= 60) // Could be PSI
            {
                score += 0.05;
            }
            else if (currVal.Value > 60)
            {
                score -= 0.3;
            }
        }

        // Bonus for pressure buildup pattern
        if (hasPressureBuildup)
            score += 0.15;

        // Bonus for fluctuation
        if (hasFluctuation && history.Count > 3)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType) => valueType.ToLowerInvariant() is "float" or "single" or "double" or "int32" or "int";
}