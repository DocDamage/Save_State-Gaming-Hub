using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting ascension/prestige levels in RPG games.
/// Ascension levels typically:
/// - Are integers in range 0-100
/// - Increase when reaching max level and resetting
/// - Provide permanent bonuses per ascension
/// </summary>
public sealed class AscensionLevelHeuristic : IValueHeuristic
{
    public string Name => "Ascension Level Detection";
    public string Category => "RPG";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool onlyIncreases = true;
        int incrementEvents = 0;

        // Check value range (ascension typically 0-100)
        if (IsInAscensionRange(value.CurrentValue))
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

            // Check for increment (usually +1 per ascension)
            if (currVal > prevVal)
            {
                incrementEvents++;
                var delta = currVal.Value - prevVal.Value;
                if (delta == 1)
                {
                    score += 0.25;
                }
            }
            // Should never decrease
            else if (currVal < prevVal)
            {
                onlyIncreases = false;
                score -= 0.5;
            }

            // Should be non-negative
            if (currVal < 0)
            {
                score -= 0.5;
            }
        }

        // Bonus for increment events
        if (incrementEvents >= 1)
            score += 0.1;

        // Bonus for only increasing
        if (onlyIncreases && history.Count > 2)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short";
    }

    private static bool IsInAscensionRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 1000;
        }
        catch
        {
            return false;
        }
    }
}