using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting gravity multiplier values in game memory.
/// Gravity values typically:
/// - Are floats in range 0.0-5.0 (1.0 = normal gravity)
/// - Mostly static
/// - Rarely change (usually during special effects)
/// </summary>
public sealed class GravityHeuristic : IValueHeuristic
{
    public string Name => "Gravity Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check value range
        if (IsInGravityRange(value.CurrentValue))
        {
            score += 0.4;
        }

        // Check if value is near 1.0 (normal gravity)
        if (IsNearNormalGravity(value.CurrentValue))
        {
            score += 0.2;
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
                // Calculate variance - gravity should be mostly static
                var avg = values.Average();
                var variance = values.Average(v => Math.Pow(v - avg, 2));

                // Low variance means mostly static
                if (variance < 0.01)
                {
                    score += 0.3;
                }

                // Check for rare changes (less than 10% of observations)
                var uniqueValues = values.Select(v => Math.Round(v, 2)).Distinct().Count();
                var changeRatio = (double)uniqueValues / values.Count;
                if (changeRatio < 0.1)
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

    private static bool IsInGravityRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0.0 && val <= 5.0;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsNearNormalGravity(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return Math.Abs(val - 1.0) < 0.1;
        }
        catch
        {
            return false;
        }
    }
}
