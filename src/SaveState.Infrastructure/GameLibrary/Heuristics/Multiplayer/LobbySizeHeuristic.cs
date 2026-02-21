using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting current lobby size in multiplayer games.
/// Lobby size values typically:
/// - Are small integers (1-100)
/// - Change as players join/leave
/// - Affect lobby status display
/// - Never exceed max players
/// </summary>
public sealed class LobbySizeHeuristic : IValueHeuristic
{
    public string Name => "Lobby Size Detection";
    public string Category => "Multiplayer";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool smallValues = true;
        int changeEvents = 0;

        // Check value range (lobby size typically 1-100)
        if (IsInLobbySizeRange(value.CurrentValue))
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
            if (currVal > 200)
            {
                smallValues = false;
            }

            // Check for changes (joins/leaves)
            if (currVal != prevVal)
            {
                changeEvents++;
                var delta = Math.Abs(currVal.Value - prevVal.Value);
                // Usually changes by 1 (one player)
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

            // Should have reasonable max
            if (currVal > 1000)
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

        // Check for common lobby sizes
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue)
        {
            var commonSizes = new[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 8.0, 10.0, 16.0, 24.0, 32.0, 64.0, 100.0 };
            foreach (var common in commonSizes)
            {
                if (Math.Abs(currentVal.Value - common) < 0.5)
                {
                    score += 0.1;
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

    private static bool IsInLobbySizeRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 200;
        }
        catch
        {
            return false;
        }
    }
}