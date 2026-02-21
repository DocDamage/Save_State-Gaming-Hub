using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting headshot/critical hit counters in shooter games.
/// Headshot values typically:
/// - Are positive integers starting from 0
/// - Only increase (never decrease)
/// - Increment by 1 per headshot
/// - Usually less than or equal to total kill count
/// </summary>
public sealed class HeadshotCountHeuristic : IValueHeuristic
{
    public string Name => "Headshot Count Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int incrementEvents = 0;
        bool onlyIncreases = true;
        bool smallValues = false;

        // Check value range (headshots typically 0-9999)
        if (IsInHeadshotRange(value.CurrentValue))
        {
            score += 0.3;
        }

        // Must be integer type
        if (!HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score -= 0.3;
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

            // Check for small values (headshots are typically infrequent)
            if (currVal >= 0 && currVal <= 50)
            {
                smallValues = true;
            }

            // Check for increment by exactly 1 (headshot)
            if (currVal == prevVal + 1)
            {
                incrementEvents++;
                score += 0.15;
            }
            // Larger increments are less likely to be headshots
            else if (currVal > prevVal + 1)
            {
                score -= 0.1;
            }

            // Check for any decrease
            if (currVal < prevVal)
            {
                onlyIncreases = false;
                score -= 0.5;
            }

            // Should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }
        }

        // Bonus for increment by 1 pattern
        if (incrementEvents >= 2)
            score += 0.2;

        // Bonus for only increasing
        if (onlyIncreases && history.Count > 2)
            score += 0.15;

        // Bonus for small values (headshots are usually less frequent than kills)
        if (smallValues)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "int64" or "long" or "byte";
    }

    private static bool IsInHeadshotRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 99999;
        }
        catch
        {
            return false;
        }
    }
}