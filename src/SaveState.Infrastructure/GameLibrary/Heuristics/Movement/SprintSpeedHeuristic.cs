using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting sprint speed values in game memory.
/// Sprint speed values typically:
/// - Are floats in range 0.0-50.0
/// - Non-zero only when sprinting
/// - Usually 1.5-2x walk speed
/// - May have stamina/time limits
/// </summary>
public sealed class SprintSpeedHeuristic : IValueHeuristic
{
    public string Name => "Sprint Speed Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int zeroWhenNotSprinting = 0;
        int nonZeroCount = 0;
        double prevVal = 0;
        bool hasHigherValues = false;

        // Check value range
        if (IsInSprintSpeedRange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Analyze observation history
        if (history.Count >= 3)
        {
            var nonZeroValues = history
                .Where(h => h.Value != null)
                .Select(h => HeuristicUtilities.ConvertToDouble(h.Value))
                .Where(v => v.HasValue && v.Value > 0.01)
                .Select(v => v!.Value)
                .ToList();

            // Sprint speed should be relatively high when active
            if (nonZeroValues.Count >= 2)
            {
                var avg = nonZeroValues.Average();
                if (avg >= 8.0 && avg <= 30.0)
                {
                    hasHigherValues = true;
                }
            }
        }

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

            // Sprint speed is 0 when not sprinting
            if (i > 0 && history[i].RelatedAction == null && val < 0.01)
            {
                zeroWhenNotSprinting++;
            }

            prevVal = val;

            // Sprint speed should never be negative
            if (val < 0)
            {
                score -= 0.3;
            }
        }

        // Bonus for having higher values when active
        if (hasHigherValues)
        {
            score += 0.25;
        }

        // Bonus for being zero when not sprinting
        if (zeroWhenNotSprinting >= 2)
        {
            score += 0.2;
        }

        // Correlation with position changes
        int movementEvents = history.Count(h => h.RelatedAction == PlayerAction.PositionChanged);
        if (movementEvents >= 3)
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

    private static bool IsInSprintSpeedRange(object? value)
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