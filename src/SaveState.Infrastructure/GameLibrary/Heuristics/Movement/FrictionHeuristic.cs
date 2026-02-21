using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting friction coefficient values in game memory.
/// Friction values typically:
/// - Are floats in range 0.0-1.0 (0 = no friction, 1 = full friction)
/// - Static for most surfaces, changing only on different terrain types
/// - Often near 0.8-1.0 for normal ground
/// </summary>
public sealed class FrictionHeuristic : IValueHeuristic
{
    public string Name => "Friction Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check value range - friction is typically 0.0 to 1.0
        if (IsInFrictionRange(value.CurrentValue))
        {
            score += 0.4;
        }

        // Check if value is near common friction values (0.8-1.0 for normal ground)
        if (IsNearNormalFriction(value.CurrentValue))
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
                // Calculate variance - friction should be mostly static per surface
                var avg = values.Average();
                var variance = values.Average(v => Math.Pow(v - avg, 2));

                // Low variance means mostly static
                if (variance < 0.05)
                {
                    score += 0.3;
                }

                // Check for discrete changes (surface changes)
                var uniqueValues = values.Select(v => Math.Round(v, 2)).Distinct().Count();
                var changeRatio = (double)uniqueValues / values.Count;
                if (changeRatio < 0.3)
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

    private static bool IsInFrictionRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0.0 && val <= 1.0;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsNearNormalFriction(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0.5 && val <= 1.0;
        }
        catch
        {
            return false;
        }
    }
}