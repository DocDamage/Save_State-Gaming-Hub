using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting step height values in game memory.
/// Step height values typically:
/// - Are floats in range 0.0-2.0
/// - Static character property
/// - Affects ability to climb stairs/small obstacles
/// - Usually 0.3-0.5 for humans
/// </summary>
public sealed class StepHeightHeuristic : IValueHeuristic
{
    public string Name => "Step Height Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check value range
        if (IsInStepHeightRange(value.CurrentValue))
        {
            score += 0.4;
        }

        // Check if value is near common step heights (0.3-0.5 meters)
        if (IsNearNormalStepHeight(value.CurrentValue))
        {
            score += 0.15;
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
                // Calculate variance - step height should be static
                var avg = values.Average();
                var variance = values.Average(v => Math.Pow(v - avg, 2));

                // Very low variance means static
                if (variance < 0.001)
                {
                    score += 0.35;
                }

                // All values should be positive
                var positiveCount = values.Count(v => v > 0);
                if (positiveCount == values.Count)
                {
                    score += 0.1;
                }
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double";
    }

    private static bool IsInStepHeightRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0.0 && val <= 2.0;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsNearNormalStepHeight(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0.2 && val <= 0.6;
        }
        catch
        {
            return false;
        }
    }
}