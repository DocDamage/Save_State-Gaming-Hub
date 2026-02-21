using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting engine damage percentage in driving/racing games.
/// Engine damage values typically:
/// - Are floats (0.0-100.0) representing percentage
/// - Start at 0 for healthy engines
/// - Increase from overheating, over-revving, or crashes
/// - Affect performance (power loss)
/// </summary>
public sealed class EngineDamageHeuristic : IValueHeuristic
{
    public string Name => "Engine Damage Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool startsAtZero = false;
        bool hasGradualDamage = false;

        // Check value range (0-100%)
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue && currentVal.Value >= 0 && currentVal.Value <= 100)
        {
            score += 0.4;
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

            // Engine damage increases gradually (overheating, wear)
            if (currVal > prevVal)
            {
                var delta = currVal.Value - prevVal.Value;
                if (delta > 0 && delta < 20)
                {
                    hasGradualDamage = true;
                    score += 0.15;
                }
                // Sudden damage from crash
                else if (delta >= 20 && delta < 80)
                {
                    score += 0.1;
                }
            }

            // Damage often increases during hard use
            if (currVal > prevVal && curr.RelatedAction == PlayerAction.Sprinted)
            {
                score += 0.1;
            }

            // Should not decrease (unless repaired, rare)
            if (currVal < prevVal && i > 3)
            {
                score -= 0.1;
            }

            // Should not be negative
            if (currVal.Value < 0)
            {
                score -= 0.5;
            }

            // Should not exceed 100
            if (currVal.Value > 100)
            {
                score -= 0.4;
            }
        }

        // Bonus for zero start
        if (startsAtZero)
            score += 0.1;

        // Bonus for gradual damage pattern
        if (hasGradualDamage)
            score += 0.2;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType) => valueType.ToLowerInvariant() is "float" or "single" or "double" or "int32" or "int";
}