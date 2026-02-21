using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting match/game ID in multiplayer games.
/// Match ID values typically:
/// - Are large integers or unique identifiers
/// - Stay constant during a match
/// - Change between matches
/// - Often sequential or random unique values
/// </summary>
public sealed class MatchIdHeuristic : IValueHeuristic
{
    public string Name => "Match ID Detection";
    public string Category => "Multiplayer";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool isConstant = true;
        bool isLargeValue = false;

        // Check value range (match IDs are typically large)
        if (IsInMatchIdRange(value.CurrentValue))
        {
            score += 0.3;
        }

        // Must be integer type
        if (!HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score -= 0.3;
        }
        else
        {
            score += 0.15;
        }

        // Check if it's a large value (typical for IDs)
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue && currentVal.Value > 1000)
        {
            isLargeValue = true;
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

            // Check for constancy (match ID should not change during match)
            if (currVal != prevVal)
            {
                isConstant = false;
                score -= 0.1;
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.4;
            }

            // Should not be zero
            if (currVal == 0)
            {
                score -= 0.2;
            }
        }

        // Strong bonus for being constant during session
        if (isConstant && history.Count > 2)
            score += 0.3;

        // Bonus for large values (typical for unique IDs)
        if (isLargeValue)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long" or "uint32" or "uint" or "uint64" or "ulong";
    }

    private static bool IsInMatchIdRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            // Match IDs can range from small to very large
            return val >= 1 && val <= 9999999999;
        }
        catch
        {
            return false;
        }
    }
}