using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting blood volume/hemorrhage values in survival games.
/// Blood level values typically:
/// - Are floats or integers (0.0-100.0 percentage or 0-5000 ml)
/// - Decrease from bleeding wounds
/// - Recover slowly through natural regeneration or transfusions
/// - Critical for consciousness and survival
/// </summary>
public sealed class BloodLevelHeuristic : IValueHeuristic
{
    public string Name => "Blood Level Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int bleedingEvents = 0;
        int recoveryEvents = 0;
        bool gradualLossPattern = false;

        // Check value range (blood: 0-100% or 0-5000 ml)
        if (IsInBloodLevelRange(value.CurrentValue))
        {
            score += 0.3;
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

            // Check for bleeding from wounds
            if (currVal < prevVal && curr.RelatedAction == PlayerAction.Attacked)
            {
                var delta = prevVal.Value - currVal.Value;
                // Wounds cause immediate blood loss
                if (delta > 5 && delta < 40)
                {
                    bleedingEvents++;
                    score += 0.18;
                }
            }

            // Check for ongoing bleeding
            if (currVal < prevVal && curr.RelatedAction == PlayerAction.Idle)
            {
                var delta = prevVal.Value - currVal.Value;
                // Untreated wounds bleed slowly
                if (delta > 0 && delta < 3)
                {
                    gradualLossPattern = true;
                    score += 0.12;
                }
            }

            // Check for blood recovery (transfusions, bandages)
            if (currVal > prevVal && curr.RelatedAction == PlayerAction.Healed)
            {
                var delta = currVal.Value - prevVal.Value;
                // Medical treatment restores blood
                if (delta > 10 && delta < 50)
                {
                    recoveryEvents++;
                    score += 0.2;
                }
            }

            // Check for slow natural recovery
            if (currVal > prevVal && curr.RelatedAction == PlayerAction.Idle)
            {
                var delta = currVal.Value - prevVal.Value;
                // Natural regeneration is very slow
                if (delta > 0 && delta < 2)
                {
                    recoveryEvents++;
                    score += 0.1;
                }
            }

            // Blood should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Reasonable max values
            if (currVal > 10000)
            {
                score -= 0.3;
            }
        }

        // Strong bonus for bleeding events
        if (bleedingEvents >= 1)
            score += 0.2;

        // Strong bonus for gradual loss pattern (distinctive)
        if (gradualLossPattern)
            score += 0.2;

        // Bonus for recovery events
        if (recoveryEvents >= 1)
            score += 0.15;

        // Check for max value ranges
        var maxValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Max();

        // Common caps: 100 (%) or 4500-5000 ml (human blood volume)
        if (Math.Abs(maxValue - 100) < 5 || (maxValue >= 4500 && maxValue <= 5500))
        {
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "int32" or "int" or "double" or "int16" or "short";
    }

    private static bool IsInBloodLevelRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Blood level: 0-100 (%) or 0-5500 ml
            var val = doubleValue.Value;
            return val >= 0 && val <= 5500;
        }
        catch
        {
            return false;
        }
    }
}