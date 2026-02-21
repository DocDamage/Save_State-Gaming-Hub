using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting individual skill levels in RPG games.
/// Skill levels typically:
/// - Are integers starting from 0 or 1
/// - Only increase (never decrease)
/// - Increment by 1 when skill improves
/// - Have maximum caps (50, 100, 255 common)
/// </summary>
public sealed class SkillLevelHeuristic : IValueHeuristic
{
    public string Name => "Skill Level Detection";
    public string Category => "RPG";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int incrementEvents = 0;
        bool onlyIncreases = true;
        bool startsLow = false;

        // Check value range (skill levels typically 0-255)
        if (IsInSkillRange(value.CurrentValue))
        {
            score += 0.3;
        }

        // Must be integer type
        if (!HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score -= 0.3;
        }

        // Check initial value
        var firstValue = history.FirstOrDefault(o => o.Value != null);
        if (firstValue != null)
        {
            var val = HeuristicUtilities.ConvertToDouble(firstValue.Value);
            if (val.HasValue && val.Value >= 0 && val.Value <= 10)
            {
                startsLow = true;
                score += 0.15;
            }
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

            // Check for increment (skill improvement)
            if (currVal > prevVal)
            {
                var delta = currVal.Value - prevVal.Value;
                // Skills typically increase by 1
                if (delta == 1)
                {
                    incrementEvents++;
                    score += 0.15;
                }
                else if (delta > 1 && delta <= 5)
                {
                    // Might be a large skill jump
                    score += 0.05;
                }
            }

            // Check for any decrease (skills should never decrease)
            if (currVal < prevVal)
            {
                onlyIncreases = false;
                score -= 0.5;
            }

            // Skill levels should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Common skill caps
            if (currVal > 999)
            {
                score -= 0.3;
            }
        }

        // Bonus for increment by 1 pattern
        if (incrementEvents >= 1)
            score += 0.15;

        // Strong bonus for only increasing
        if (onlyIncreases && history.Count > 2)
            score += 0.2;

        // Check for common max values (caps)
        var maxValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Max();

        // Common skill caps: 50, 100, 255
        var commonCaps = new[] { 50.0, 100.0, 255.0, 99.0 };
        foreach (var cap in commonCaps)
        {
            if (Math.Abs(maxValue - cap) < 2)
            {
                score += 0.15;
                break;
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "byte" or "int64" or "long";
    }

    private static bool IsInSkillRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Skill levels typically in range 0-999
            var val = doubleValue.Value;
            return val >= 0 && val <= 999;
        }
        catch
        {
            return false;
        }
    }
}