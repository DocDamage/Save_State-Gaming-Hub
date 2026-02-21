using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting ammo/magazine values in game memory.
/// Ammo values typically:
/// - Are small integers (0-999)
/// - Decrease by 1 on "UsedAmmo"
/// - Jump up on "Reloaded"
/// - Often paired with "max ammo" value
/// - Reset to max on weapon switch or respawn
/// </summary>
public sealed class AmmoHeuristic : IValueHeuristic
{
    public string Name => "Ammo Detection";
    public string Category => "Ammo";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int usedAmmoEvents = 0;
        int reloadEvents = 0;
        int decrementByOneCount = 0;

        // Check value range for ammo
        if (IsInAmmoRange(value.CurrentValue))
        {
            score += 0.3;
        }

        // Ammo is typically an integer
        if (IsIntegerValue(value.CurrentValue))
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

            var delta = currVal.Value - prevVal.Value;

            // Check for decrease by 1 (typical ammo usage)
            if (Math.Abs(delta - (-1)) < 0.001)
            {
                decrementByOneCount++;
            }

            // Check for UsedAmmo action correlation
            if (curr.RelatedAction == PlayerAction.UsedAmmo)
            {
                usedAmmoEvents++;
                if (delta < 0)
                {
                    score += 0.15;
                }
            }

            // Check for Reloaded action correlation
            if (curr.RelatedAction == PlayerAction.Reloaded)
            {
                reloadEvents++;
                if (delta > 0)
                {
                    score += 0.15;
                }
            }

            // Ammo should never be negative
            if (currVal < 0)
            {
                score -= 0.3;
            }

            // Large jumps might indicate reloads or weapon switches
            if (delta > 10)
            {
                // Could be a reload - check if it's a common max ammo value
                if (IsCommonMaxAmmo(currVal.Value))
                {
                    score += 0.1;
                }
            }
        }

        // Strong bonus for consistent -1 decrements (shooting)
        if (decrementByOneCount >= 2)
        {
            score += 0.25;
        }

        // Bonus for having both used and reload events
        if (usedAmmoEvents >= 2 && reloadEvents >= 1)
        {
            score += 0.2;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "byte";
    }

    private static bool IsInAmmoRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 999;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsIntegerValue(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return true;

            return Math.Abs(doubleValue.Value % 1) < 0.0001;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsCommonMaxAmmo(double value)
    {
        // Common max ammo values in games
        var commonMaxValues = new[] { 30, 32, 60, 100, 200, 255, 999 };
        return commonMaxValues.Any(v => Math.Abs(value - v) < 0.001);
    }
}
