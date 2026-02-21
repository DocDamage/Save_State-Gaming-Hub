using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting oxygen/breath values in underwater/space survival games.
/// Oxygen values typically:
/// - Are floats or integers (0-100 or 0-1.0)
/// - Decrease when underwater or in vacuum
/// - Refill quickly when returning to breathable atmosphere
/// - Critical for survival (damage/death at 0)
/// </summary>
public sealed class OxygenHeuristic : IValueHeuristic
{
    public string Name => "Oxygen/Breath Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int depletionEvents = 0;
        int refillEvents = 0;
        bool rapidRefill = false;

        // Check value range (oxygen typically 0-100)
        if (IsInOxygenRange(value.CurrentValue))
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

            // Check for oxygen depletion (underwater/vacuum)
            if (currVal < prevVal && curr.RelatedAction == PlayerAction.Sprinted)
            {
                depletionEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Oxygen depletes at steady rate
                if (delta > 0 && delta < 10)
                {
                    score += 0.1;
                }
            }

            // Check for refill (surfacing/returning to air)
            if (currVal > prevVal)
            {
                refillEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Oxygen refills rapidly (20-100 units at once)
                if (delta > 20)
                {
                    rapidRefill = true;
                    score += 0.15;
                }
            }

            // Oxygen should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Oxygen typically caps at 100
            if (currVal > 100)
            {
                score -= 0.3;
            }
        }

        // Bonus for depletion pattern
        if (depletionEvents >= 2)
            score += 0.15;

        // Strong bonus for rapid refill (distinctive of oxygen)
        if (rapidRefill)
            score += 0.2;

        // Bonus for refill events
        if (refillEvents >= 1)
            score += 0.1;

        // Check for max value of 100
        var maxValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Max();

        if (Math.Abs(maxValue - 100) < 5)
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

    private static bool IsInOxygenRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Oxygen typically in range 0-100
            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}