using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting crossbow bolt ammunition count in RPG/action games.
/// Bolt values typically:
/// - Are integers (0-500)
/// - Decrease by 1 when fired from crossbow
/// - Increase when crafting or purchasing in smaller batches than arrows
/// </summary>
public sealed class BoltHeuristic : IValueHeuristic
{
    public string Name => "Crossbow Bolts Detection";
    public string Category => "Resource";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int gainEvents = 0;
        int fireEvents = 0;

        // Check value range (bolts typically 0-500, lower stack than arrows)
        if (IsInBoltRange(value.CurrentValue))
        {
            score += 0.4;
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

            // Check for gain (crafting/purchasing)
            if (currVal > prevVal)
            {
                gainEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Bolts gained in smaller batches (3-30)
                if (delta >= 3 && delta <= 50)
                {
                    score += 0.15;
                }
            }

            // Check for fire (shooting bolt)
            if (currVal < prevVal)
            {
                fireEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Shooting bolt decreases by exactly 1
                if (delta == 1)
                {
                    score += 0.25;
                }
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }
        }

        // Strong bonus for single-shot pattern
        if (fireEvents >= 3)
            score += 0.25;
        if (gainEvents >= 1)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short";
    }

    private static bool IsInBoltRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 5000;
        }
        catch
        {
            return false;
        }
    }
}