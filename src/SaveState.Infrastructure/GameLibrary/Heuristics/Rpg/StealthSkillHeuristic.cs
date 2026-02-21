using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting stealth/sneaking skill levels in RPG games.
/// Stealth skill values typically:
/// - Are integers in range 0-100
/// - Increase through successful stealth actions
/// - Affect detection radius and sneak attack damage
/// </summary>
public sealed class StealthSkillHeuristic : IValueHeuristic
{
    public string Name => "Stealth Skill Detection";
    public string Category => "RPG";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int increases = 0;
        int decreases = 0;

        // Check value range
        if (IsInSkillRange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Skill levels are always integers
        if (HeuristicUtilities.IsIntegerValue(value.CurrentValue))
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

            if (delta > 0)
            {
                increases++;
                // Small increments typical of skill gains
                if (delta <= 5)
                {
                    score += 0.15;
                }
            }
            else if (delta < 0)
            {
                decreases++;
                // Skills rarely decrease
                score -= 0.3;
            }

            // Skills should never be negative
            if (currVal.Value < 0)
            {
                score -= 0.5;
            }
        }

        // Skills mostly increase
        if (increases > 0 && decreases == 0)
        {
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long" or "int16" or "short";
    }

    private static bool IsInSkillRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 500;
        }
        catch
        {
            return false;
        }
    }
}