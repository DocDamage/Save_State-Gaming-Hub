using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting ignore/blocked player count in multiplayer games.
/// Ignore list count values typically:
/// - Are small integers (0-500)
/// - Change when players are blocked/unblocked
/// - Relatively stable
/// - Often capped by platform limits
/// </summary>
public sealed class IgnoreListCountHeuristic : IValueHeuristic
{
    public string Name => "Ignore List Count Detection";
    public string Category => "Multiplayer";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool smallValues = true;
        int changeEvents = 0;

        // Check value range (ignore list typically 0-1000)
        if (IsInIgnoreListRange(value.CurrentValue))
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

            // Check for changes (players blocked/unblocked)
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

            // Platform limits
            if (currVal > 5000)
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

        // Bonus for zero (common case - no blocked players)
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue && currentVal.Value == 0)
        {
            score += 0.1;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "byte";
    }

    private static bool IsInIgnoreListRange(object? value)
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