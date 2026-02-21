using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting special/heavy ammo in shooter games.
/// Special ammo values typically:
/// - Are integers (0-50)
/// - Rare and valuable
/// - Used for powerful weapons
/// </summary>
public sealed class AmmoSpecialHeuristic : IValueHeuristic
{
    public string Name => "Special/Heavy Ammo Detection";
    public string Category => "Resource";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int pickupEvents = 0;
        int useEvents = 0;
        bool smallValues = true;

        // Check value range (special ammo typically 0-50)
        if (IsInSpecialAmmoRange(value.CurrentValue))
        {
            score += 0.4;
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

            // Check for small values throughout
            if (currVal > 50)
            {
                smallValues = false;
            }

            // Check for pickup
            if (currVal > prevVal)
            {
                pickupEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Usually find 1-5 special ammo
                if (delta >= 1 && delta <= 5)
                {
                    score += 0.15;
                }
            }

            // Check for use
            if (currVal < prevVal)
            {
                useEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Usually use 1 at a time
                if (delta == 1)
                {
                    score += 0.15;
                }
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }
        }

        // Bonus for patterns
        if (pickupEvents >= 1)
            score += 0.1;
        if (useEvents >= 1)
            score += 0.1;

        // Strong bonus for consistently small values
        if (smallValues && history.Count > 1)
            score += 0.2;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short";
    }

    private static bool IsInSpecialAmmoRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}