using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting backpedal (backward movement) speed values in game memory.
/// Backpedal speed values typically:
/// - Are floats in range 0.0 to 20.0
/// - Always negative or treated as negative (backward)
/// - Usually 50-70% of forward speed
/// - Zero when moving forward or stationary
/// </summary>
public sealed class BackpedalSpeedHeuristic : IValueHeuristic
{
    public string Name => "Backpedal Speed Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int zeroWhenNotBackpedaling = 0;
        int nonZeroCount = 0;
        bool valuesAreLow = false;

        // Check value range
        if (IsInBackpedalSpeedRange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Analyze observation history
        if (history.Count >= 3)
        {
            var nonZeroValues = history
                .Where(h => h.Value != null)
                .Select(h => HeuristicUtilities.ConvertToDouble(h.Value))
                .Where(v => v.HasValue && Math.Abs(v.Value) > 0.01)
                .Select(v => Math.Abs(v!.Value))
                .ToList();

            // Backpedal is typically slower than forward movement
            if (nonZeroValues.Count >= 2 && nonZeroValues.Average() <= 10.0)
            {
                valuesAreLow = true;
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
            if (Math.Abs(val) > 0.01)
                nonZeroCount++;

            // Backpedal speed is 0 when not backpedaling
            if (i > 0 && history[i].RelatedAction == null && Math.Abs(val) < 0.01)
            {
                zeroWhenNotBackpedaling++;
            }
        }

        // Bonus for lower speed values
        if (valuesAreLow)
        {
            score += 0.25;
        }

        // Bonus for being zero when not backpedaling
        if (zeroWhenNotBackpedaling >= 2)
        {
            score += 0.25;
        }

        // Correlation with position changes
        int positionEvents = history.Count(h => h.RelatedAction == PlayerAction.PositionChanged);
        if (positionEvents >= 2)
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

    private static bool IsInBackpedalSpeedRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            // Can be stored as negative or positive
            return Math.Abs(val) >= 0.0 && Math.Abs(val) <= 20.0;
        }
        catch
        {
            return false;
        }
    }
}