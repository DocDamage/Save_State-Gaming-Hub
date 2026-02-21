using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting replay/recording ID in multiplayer games.
/// Replay ID values typically:
/// - Are large integers or unique identifiers
/// - Stay constant during replay viewing
/// - Change between different replays
/// - Often sequential or hash-based
/// </summary>
public sealed class ReplayIdHeuristic : IValueHeuristic
{
    public string Name => "Replay ID Detection";
    public string Category => "Multiplayer";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool isConstant = true;
        bool isLargeValue = false;

        // Check value range (replay IDs are typically large)
        if (IsInReplayIdRange(value.CurrentValue))
        {
            score += 0.35;
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

            // Check for constancy (replay ID should not change during viewing)
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
            score += 0.05;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long" or "uint32" or "uint" or "uint64" or "ulong";
    }

    private static bool IsInReplayIdRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            // Replay IDs can range from small to very large
            return val >= 1 && val <= 999999999999;
        }
        catch
        {
            return false;
        }
    }
}