using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting broken bone/fracture values in survival games.
/// Broken bone values typically:
/// - Are integers (0 or 1 for binary states, or 0-100 for severity)
/// - Occur suddenly after falls or combat damage
/// - Heal slowly over time with treatment
/// - Severely limit movement and actions
/// </summary>
public sealed class BrokenBoneHeuristic : IValueHeuristic
{
    public string Name => "Broken Bone Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int injuryEvents = 0;
        int healingEvents = 0;
        bool binaryStatePattern = false;
        bool gradualHealingPattern = false;

        // Check value range (broken bone: binary 0/1 or severity 0-100)
        if (IsInBrokenBoneRange(value.CurrentValue))
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

            // Check for sudden injury (damage event)
            if (currVal > prevVal && (curr.RelatedAction == PlayerAction.Attacked || 
                                       curr.RelatedAction == PlayerAction.Sprinted))
            {
                var delta = currVal.Value - prevVal.Value;
                // Broken bones happen suddenly
                if (delta > 50 || (prevVal == 0 && currVal > 0))
                {
                    injuryEvents++;
                    score += 0.2;
                }
            }

            // Check for healing over time
            if (currVal < prevVal && (curr.RelatedAction == PlayerAction.Healed || 
                                       curr.RelatedAction == PlayerAction.Idle))
            {
                var delta = prevVal.Value - currVal.Value;
                // Healing is very slow for broken bones
                if (delta > 0 && delta < 5)
                {
                    healingEvents++;
                    gradualHealingPattern = true;
                    score += 0.1;
                }
                // Or sudden healing with medical treatment
                else if (delta >= 50 || currVal == 0)
                {
                    healingEvents++;
                    score += 0.15;
                }
            }

            // Check for binary pattern (0 or 1)
            if ((currVal == 0 || currVal == 1) && (prevVal == 0 || prevVal == 1))
            {
                binaryStatePattern = true;
                score += 0.15;
            }

            // Broken bone should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Reasonable max for severity
            if (currVal > 100)
            {
                score -= 0.3;
            }
        }

        // Strong bonus for injury events
        if (injuryEvents >= 1)
            score += 0.2;

        // Bonus for healing events
        if (healingEvents >= 1)
            score += 0.15;

        // Bonus for binary state pattern
        if (binaryStatePattern)
            score += 0.15;

        // Bonus for gradual healing pattern
        if (gradualHealingPattern)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "int32" or "int" or "double" or "int16" or "short" or "bool" or "boolean";
    }

    private static bool IsInBrokenBoneRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Broken bone: binary 0/1 or severity 0-100
            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}