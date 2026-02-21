using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting damage taken in multiplayer games.
/// Damage taken values typically:
/// - Are integers (0-999999)
/// - Only increase during match
/// - Can spike when taking heavy damage
/// - Reset between matches
/// </summary>
public sealed class DamageTakenHeuristic : IValueHeuristic
{
    public string Name => "Damage Taken Detection";
    public string Category => "Multiplayer";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool onlyIncreases = true;
        int incrementEvents = 0;

        // Check value range (damage typically 0-9999999)
        if (IsInDamageRange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Must be integer
        if (!HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score -= 0.3;
        }
        else
        {
            score += 0.15;
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

            // Check for increment
            if (currVal > prevVal)
            {
                incrementEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Damage varies by hit (1-200 typically)
                if (delta >= 1 && delta <= 500)
                {
                    score += 0.1;
                }
                else if (delta > 500 && delta <= 2000)
                {
                    score += 0.05;
                }
            }
            // Should not decrease during match
            else if (currVal < prevVal)
            {
                onlyIncreases = false;
                // Might reset between matches
                if (prevVal > 1000 && currVal < 100)
                {
                    score += 0.1; // Likely match reset
                }
                else
                {
                    score -= 0.3;
                }
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }
        }

        // Bonus for increment events
        if (incrementEvents >= 1)
            score += 0.15;

        // Bonus for mostly increasing
        if (onlyIncreases && history.Count > 2)
            score += 0.2;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long";
    }

    private static bool IsInDamageRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 999999999;
        }
        catch
        {
            return false;
        }
    }
}