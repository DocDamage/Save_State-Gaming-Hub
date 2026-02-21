using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting friend count in multiplayer games.
/// Friend count values typically:
/// - Are small integers (0-500)
/// - Change when adding/removing friends
/// - Relatively stable during gameplay
/// - Often capped by platform limits
/// </summary>
public sealed class FriendCountHeuristic : IValueHeuristic
{
    public string Name => "Friend Count Detection";
    public string Category => "Multiplayer";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool smallValues = true;
        int changeEvents = 0;

        // Check value range (friend count typically 0-1000)
        if (IsInFriendCountRange(value.CurrentValue))
        {
            score += 0.4;
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

            // Check for small values
            if (currVal > 2000)
            {
                smallValues = false;
            }

            // Check for changes (friends added/removed)
            if (currVal != prevVal)
            {
                changeEvents++;
                var delta = Math.Abs(currVal.Value - prevVal.Value);
                // Usually changes by 1
                if (delta == 1)
                {
                    score += 0.15;
                }
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Platform limits (usually 1000-5000)
            if (currVal > 10000)
            {
                score -= 0.4;
            }
        }

        // Bonus for change events
        if (changeEvents >= 1)
            score += 0.1;

        // Bonus for small values
        if (smallValues && history.Count > 1)
            score += 0.15;

        // Check for common friend count ranges
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue)
        {
            // Most players have 0-200 friends
            if (currentVal.Value >= 0 && currentVal.Value <= 200)
            {
                score += 0.1;
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "byte";
    }

    private static bool IsInFriendCountRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 5000;
        }
        catch
        {
            return false;
        }
    }
}