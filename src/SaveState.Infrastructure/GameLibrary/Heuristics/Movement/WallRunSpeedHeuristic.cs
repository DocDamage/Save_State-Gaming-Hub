using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting wall run speed values in game memory.
/// Wall run speed values typically:
/// - Are floats in range 0.0-40.0
/// - Non-zero only when running on walls
/// - Often combined with wall cling/grind mechanics
/// - Common in parkour games (Titanfall, Mirror's Edge, etc.)
/// </summary>
public sealed class WallRunSpeedHeuristic : IValueHeuristic
{
    public string Name => "Wall Run Speed Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int zeroWhenNotWallRunning = 0;
        int nonZeroCount = 0;
        double prevVal = 0;

        // Check value range
        if (IsInWallRunSpeedRange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Analyze observation history
        for (int i = 0; i < history.Count; i++)
        {
            if (history[i].Value == null)
                continue;

            double? currVal = HeuristicUtilities.ConvertToDouble(history[i].Value);
            if (!currVal.HasValue)
                continue;

            var val = currVal.Value;

            // Track non-zero values
            if (val > 0.01)
                nonZeroCount++;

            // Wall run speed is 0 when not wall running
            if (i > 0 && history[i].RelatedAction == null && val < 0.01)
            {
                zeroWhenNotWallRunning++;
            }

            prevVal = val;

            // Wall run speed should never be negative
            if (val < 0)
            {
                score -= 0.3;
            }
        }

        // Bonus for being zero when not wall running (intermittent pattern)
        if (zeroWhenNotWallRunning >= 2)
        {
            score += 0.25;
        }

        // Bonus for rare activation (wall running is special)
        if (nonZeroCount >= 1 && nonZeroCount < history.Count * 0.3)
        {
            score += 0.25;
        }

        // Correlation with position changes
        int movementEvents = history.Count(h => h.RelatedAction == PlayerAction.PositionChanged);
        if (movementEvents >= 2)
        {
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double";
    }

    private static bool IsInWallRunSpeedRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0.0 && val <= 40.0;
        }
        catch
        {
            return false;
        }
    }
}