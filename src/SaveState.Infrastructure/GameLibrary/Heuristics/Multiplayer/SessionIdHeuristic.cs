using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting game session ID in multiplayer games.
/// Session ID values typically:
/// - Are large unique integers
/// - Stay constant during a play session
/// - Change between sessions
/// - Used for tracking and analytics
/// </summary>
public sealed class SessionIdHeuristic : IValueHeuristic
{
    public string Name => "Game Session ID Detection";
    public string Category => "Multiplayer";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool isConstant = true;
        bool isLargeValue = false;

        // Check value range (session IDs are typically large)
        if (IsInSessionIdRange(value.CurrentValue))
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

        // Check if it's a large value (typical for session IDs)
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue && currentVal.Value > 10000)
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

            // Check for constancy (session ID should not change during session)
            if (currVal != prevVal)
            {
                isConstant = false;
                score -= 0.15;
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

        // Strong bonus for being constant
        if (isConstant && history.Count > 2)
            score += 0.3;

        // Bonus for large values
        if (isLargeValue)
            score += 0.05;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long" or "uint32" or "uint" or "uint64" or "ulong";
    }

    private static bool IsInSessionIdRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 1 && val <= 999999999999;
        }
        catch
        {
            return false;
        }
    }
}