using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting prestige/renown resource in RPG/strategy games.
/// Prestige values typically:
/// - Are integers (0-99999)
/// - Increase from achievements, victories, or reputation
/// - Usually does not decrease (persistent progression)
/// </summary>
public sealed class PrestigeHeuristic : IValueHeuristic
{
    public string Name => "Prestige/Renown Detection";
    public string Category => "Resource";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int gainEvents = 0;
        int decreaseEvents = 0;

        // Check value range (prestige typically 0-99999)
        if (IsInPrestigeRange(value.CurrentValue))
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

            // Check for gain (achievements/victories)
            if (currVal > prevVal)
            {
                gainEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Prestige gained from significant events
                if (delta >= 10 && delta <= 5000)
                {
                    score += 0.15;
                }
            }

            // Prestige rarely decreases
            if (currVal < prevVal)
            {
                decreaseEvents++;
                score -= 0.3;
            }

            // Should never be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }
        }

        // Strong bonus for only-increasing pattern
        if (gainEvents >= 2 && decreaseEvents == 0)
            score += 0.25;
        else if (decreaseEvents > 0)
            score -= 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long";
    }

    private static bool IsInPrestigeRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 9999999;
        }
        catch
        {
            return false;
        }
    }
}