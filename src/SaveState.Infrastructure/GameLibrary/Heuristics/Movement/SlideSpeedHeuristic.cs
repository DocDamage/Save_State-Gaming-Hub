using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting slide speed values in game memory.
/// Slide speed values typically:
/// - Are floats in range 0.0-50.0
/// - High initial value that decays over time
/// - Zero when not sliding
/// - Common in modern FPS games (Apex, Titanfall, etc.)
/// </summary>
public sealed class SlideSpeedHeuristic : IValueHeuristic
{
    public string Name => "Slide Speed Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int zeroWhenNotSliding = 0;
        int decayPatternCount = 0;
        double prevVal = 0;

        // Check value range
        if (IsInSlideSpeedRange(value.CurrentValue))
        {
            score += 0.3;
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

            // Detect decay pattern (current < previous while sliding)
            if (i > 0 && val > 0.01 && val < prevVal - 0.5)
            {
                decayPatternCount++;
            }

            // Slide speed is 0 when not sliding
            if (i > 0 && history[i].RelatedAction == null && val < 0.01)
            {
                zeroWhenNotSliding++;
            }

            prevVal = val;

            // Slide speed should never be negative
            if (val < 0)
            {
                score -= 0.3;
            }
        }

        // Bonus for decay pattern (sliding slows down)
        if (decayPatternCount >= 2)
        {
            score += 0.35;
        }

        // Bonus for being zero when not sliding
        if (zeroWhenNotSliding >= 2)
        {
            score += 0.2;
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

    private static bool IsInSlideSpeedRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0.0 && val <= 50.0;
        }
        catch
        {
            return false;
        }
    }
}