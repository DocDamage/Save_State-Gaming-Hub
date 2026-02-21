using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting party/group size in multiplayer games.
/// Party size values typically:
/// - Are small integers (1-8 typically)
/// - Change as players join/leave party
/// - Affects matchmaking and bonuses
/// - Often capped at specific limits (4, 6, 8)
/// </summary>
public sealed class PartySizeHeuristic : IValueHeuristic
{
    public string Name => "Party Size Detection";
    public string Category => "Multiplayer";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool smallValues = true;
        int changeEvents = 0;

        // Check value range (party size typically 1-8)
        if (IsInPartySizeRange(value.CurrentValue))
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
            if (currVal > 50)
            {
                smallValues = false;
            }

            // Check for changes (players join/leave)
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

            // Should not be zero or negative
            if (currVal < 1)
            {
                score -= 0.4;
            }

            // Reasonable max for party size
            if (currVal > 100)
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

        // Check for common party size values
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue)
        {
            var commonSizes = new[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 8.0, 10.0, 12.0, 16.0, 20.0, 25.0, 40.0, 50.0 };
            foreach (var common in commonSizes)
            {
                if (Math.Abs(currentVal.Value - common) < 0.5)
                {
                    score += 0.15;
                    break;
                }
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "byte";
    }

    private static bool IsInPartySizeRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 1 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}