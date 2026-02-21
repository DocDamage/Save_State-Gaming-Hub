using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting win streak count in multiplayer games.
/// Win streak values typically:
/// - Are integers (0-50+)
/// - Reset to 0 on loss
/// - Only increase or reset
/// - Used for bonus rewards
/// </summary>
public sealed class WinStreakHeuristic : IValueHeuristic
{
    public string Name => "Win Streak Detection";
    public string Category => "Multiplayer";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool onlyIncreasesOrResets = true;
        int incrementEvents = 0;
        int resetEvents = 0;

        // Check value range (win streak typically 0-100)
        if (IsInWinStreakRange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Must be integer
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

            // Check for increment (win)
            if (currVal == prevVal + 1)
            {
                incrementEvents++;
                score += 0.15;
            }
            // Check for reset (loss)
            else if (currVal == 0 && prevVal > 0)
            {
                resetEvents++;
                score += 0.1;
            }
            // Other changes are suspicious
            else if (currVal != prevVal)
            {
                onlyIncreasesOrResets = false;
                score -= 0.2;
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Reasonable max for win streak
            if (currVal > 1000)
            {
                score -= 0.4;
            }
        }

        // Bonus for increment events
        if (incrementEvents >= 1)
            score += 0.15;

        // Bonus for reset events (normal behavior)
        if (resetEvents >= 1)
            score += 0.1;

        // Bonus for expected behavior
        if (onlyIncreasesOrResets && history.Count > 2)
            score += 0.2;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "byte";
    }

    private static bool IsInWinStreakRange(object? value)
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