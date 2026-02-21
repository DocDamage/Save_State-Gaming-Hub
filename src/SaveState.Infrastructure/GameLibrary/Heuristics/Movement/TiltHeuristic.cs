using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting tilt angle values in game memory.
/// Tilt values typically:
/// - Are floats in range -90.0 to +90.0 degrees
/// - Used for camera tilt or head tilt effects
/// - Can indicate daze, dizziness, or special states
/// - Usually returns to 0 quickly
/// </summary>
public sealed class TiltHeuristic : IValueHeuristic
{
    public string Name => "Tilt Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasNegative = false;
        bool hasPositive = false;
        bool nearZeroCommon = false;
        int rapidReturns = 0;

        // Check value range - tilt is typically -90 to +90 degrees
        if (IsInTiltRange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Analyze observation history
        if (history.Count >= 3)
        {
            var values = history
                .Where(h => h.Value != null)
                .Select(h => HeuristicUtilities.ConvertToDouble(h.Value))
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToList();

            if (values.Count >= 3)
            {
                // Tilt usually spends most time near zero
                var nearZeroCount = values.Count(v => Math.Abs(v) < 5.0);
                if (nearZeroCount > values.Count * 0.6)
                {
                    nearZeroCommon = true;
                }
            }
        }

        for (int i = 1; i < history.Count; i++)
        {
            if (history[i].Value == null || history[i - 1].Value == null)
                continue;

            double? currVal = HeuristicUtilities.ConvertToDouble(history[i].Value);
            double? prevVal = HeuristicUtilities.ConvertToDouble(history[i - 1].Value);

            if (!currVal.HasValue || !prevVal.HasValue)
                continue;

            var val = currVal.Value;
            var previous = prevVal.Value;

            // Track positive/negative
            if (val > 5.0) hasPositive = true;
            if (val < -5.0) hasNegative = true;

            // Check for rapid returns to zero
            if (Math.Abs(val) < 2.0 && Math.Abs(previous) > 10.0)
            {
                rapidReturns++;
            }
        }

        // Bonus for near-zero being common
        if (nearZeroCommon)
        {
            score += 0.25;
        }

        // Bonus for having both directions
        if (hasNegative && hasPositive)
        {
            score += 0.15;
        }

        // Bonus for rapid returns (tilt is temporary)
        if (rapidReturns >= 2)
        {
            score += 0.25;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double";
    }

    private static bool IsInTiltRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= -90.0 && val <= 90.0;
        }
        catch
        {
            return false;
        }
    }
}