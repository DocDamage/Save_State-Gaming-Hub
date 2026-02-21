using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting grenade/explosive count in shooter/action games.
/// Grenade values typically:
/// - Are integers (0-10)
/// - Decrease by 1 when thrown
/// - Increase when resupplying (usually 1-3 at a time)
/// </summary>
public sealed class GrenadeHeuristic : IValueHeuristic
{
    public string Name => "Grenades/Explosives Detection";
    public string Category => "Resource";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int gainEvents = 0;
        int throwEvents = 0;

        // Check value range (grenades typically 0-10, very limited)
        if (IsInGrenadeRange(value.CurrentValue))
        {
            score += 0.5;
        }

        // Must be integer
        if (!HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score -= 0.2;
        }
        else
        {
            score += 0.1;
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

            // Check for gain (resupply)
            if (currVal > prevVal)
            {
                gainEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Grenades gained in very small amounts (1-3)
                if (delta >= 1 && delta <= 5)
                {
                    score += 0.2;
                }
            }

            // Check for throw (using grenade)
            if (currVal < prevVal)
            {
                throwEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Throwing grenade decreases by exactly 1
                if (delta == 1)
                {
                    score += 0.3;
                }
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }
        }

        // Very strong bonus for single-use pattern with low max
        if (throwEvents >= 1)
            score += 0.2;
        if (gainEvents >= 1)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short";
    }

    private static bool IsInGrenadeRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 50;
        }
        catch
        {
            return false;
        }
    }
}