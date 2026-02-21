using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting bullet/firearm ammunition count in shooter games.
/// Bullet values typically:
/// - Are integers (0-999)
/// - Decrease rapidly when firing (burst fire patterns)
/// - Increase when reloading or picking up ammo boxes
/// </summary>
public sealed class BulletHeuristic : IValueHeuristic
{
    public string Name => "Bullets/Firearm Ammo Detection";
    public string Category => "Resource";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int gainEvents = 0;
        int fireEvents = 0;
        int rapidFirePattern = 0;

        // Check value range (bullets typically 0-999)
        if (IsInBulletRange(value.CurrentValue))
        {
            score += 0.35;
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

            // Check for gain (picking up ammo/reloading reserves)
            if (currVal > prevVal)
            {
                gainEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Ammo pickup gives 15-60 rounds typically
                if (delta >= 10 && delta <= 120)
                {
                    score += 0.15;
                }
            }

            // Check for fire (shooting)
            if (currVal < prevVal)
            {
                fireEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Single shots
                if (delta == 1)
                {
                    score += 0.2;
                }
                // Burst fire (3-5 rounds)
                else if (delta >= 3 && delta <= 5)
                {
                    score += 0.25;
                    rapidFirePattern++;
                }
                // Full auto bursts
                else if (delta >= 5 && delta <= 30)
                {
                    score += 0.15;
                    rapidFirePattern++;
                }
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }
        }

        // Strong bonus for rapid fire patterns (unique to firearms)
        if (rapidFirePattern >= 2)
            score += 0.2;
        if (fireEvents >= 3)
            score += 0.15;
        if (gainEvents >= 1)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short";
    }

    private static bool IsInBulletRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 9999;
        }
        catch
        {
            return false;
        }
    }
}