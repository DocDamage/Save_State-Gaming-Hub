using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting experience points in game memory.
/// XP values typically:
/// - Are integers or floats
/// - Only increase (until level up, then may reset)
/// - Increase on "GainedXp" action
/// - Change by various amounts (not always 1)
/// - May have a "next level" threshold nearby
/// </summary>
public sealed class ExperienceHeuristic : IValueHeuristic
{
    public string Name => "Experience Detection";
    public string Category => "Experience";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int xpGainEvents = 0;
        int levelUpEvents = 0;
        int increases = 0;
        int decreases = 0;

        // Check value range for XP
        if (IsInXpRange(value.CurrentValue))
        {
            score += 0.2;
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

            // Track increases vs decreases
            if (delta > 0)
            {
                increases++;
            }
            else if (delta < 0)
            {
                decreases++;
            }

            // Check for GainedXp action correlation
            if (curr.RelatedAction == PlayerAction.GainedXp)
            {
                xpGainEvents++;
                if (delta > 0)
                {
                    score += 0.15;
                }
            }

            // Check for LeveledUp action correlation (might reset XP)
            if (curr.RelatedAction == PlayerAction.LeveledUp)
            {
                levelUpEvents++;
                if (delta < 0)
                {
                    // XP reset after level up is common
                    score += 0.1;
                }
            }

            // Check for reasonable XP gain amounts
            if (delta > 0 && delta < 100000)
            {
                score += 0.05;
            }
        }

        // XP should mostly increase
        if (history.Count > 1)
        {
            var increaseRatio = (double)increases / (history.Count - 1);
            if (increaseRatio > 0.7)
            {
                score += 0.2;
            }

            // Penalty for too many decreases (unlike health/ammo)
            if (decreases > increases)
            {
                score -= 0.3;
            }
        }

        // Bonus for XP gain events
        if (xpGainEvents >= 2)
        {
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long" or "float" or "single";
    }

    private static bool IsInXpRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 999999999; // XP can get very high
        }
        catch
        {
            return false;
        }
    }
}
