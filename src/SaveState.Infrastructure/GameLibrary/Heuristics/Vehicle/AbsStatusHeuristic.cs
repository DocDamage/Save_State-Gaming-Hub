using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting ABS (Anti-lock Braking System) status in driving/racing games.
/// ABS status values typically:
/// - Are booleans (0/1) or activation counters
/// - 0 = inactive, 1 = active/pulsing
/// - Active during hard braking with wheel lockup
/// - Rapid on/off cycles during intervention
/// </summary>
public sealed class AbsStatusHeuristic : IValueHeuristic
{
    public string Name => "ABS Status Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasActivation = false;
        bool hasInactiveState = false;
        bool hasRapidCycles = false;

        // Check value range
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue)
        {
            // Boolean
            if (currentVal.Value == 0 || currentVal.Value == 1)
            {
                score += 0.35;
            }
            // Activation intensity
            else if (currentVal.Value >= 0 && currentVal.Value <= 100)
            {
                score += 0.3;
            }
        }

        // Analyze observation history
        int activationCount = 0;

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

            // Check for inactive state
            if (currVal.Value < 0.1 || currVal.Value == 0)
            {
                hasInactiveState = true;
                score += 0.1;
            }

            // Activation during movement/braking
            if (currVal.Value > 0.5 && curr.RelatedAction == PlayerAction.Moved)
            {
                hasActivation = true;
                activationCount++;
                score += 0.1;
            }

            // Rapid on/off cycles (ABS pulsing)
            if ((prevVal.Value > 0.5 && currVal.Value < 0.5) ||
                (prevVal.Value < 0.5 && currVal.Value > 0.5))
            {
                score += 0.1;
            }

            // Should be bounded
            if (currVal.Value >= 0 && currVal.Value <= 1)
            {
                score += 0.05;
            }

            // Should not be negative
            if (currVal.Value < 0)
            {
                score -= 0.4;
            }
        }

        // Multiple rapid activations suggest ABS pulsing
        if (activationCount >= 3)
        {
            hasRapidCycles = true;
            score += 0.15;
        }

        // Bonus for inactive state
        if (hasInactiveState)
            score += 0.1;

        // Bonus for activation
        if (hasActivation)
            score += 0.15;

        // Bonus for rapid cycles
        if (hasRapidCycles)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType) => valueType.ToLowerInvariant() is "float" or "single" or "double" or "int32" or "int";
}