using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting mass values in game memory.
/// Mass values typically:
/// - Are floats in range 1.0-500.0 (character/object mass in kg)
/// - Static for characters, rarely changes
/// - Affects physics calculations (gravity, collision)
/// </summary>
public sealed class MassHeuristic : IValueHeuristic
{
    public string Name => "Mass Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check value range
        if (IsInMassRange(value.CurrentValue))
        {
            score += 0.4;
        }

        // Check if value is near common mass values (50-100kg for characters)
        if (IsNearNormalMass(value.CurrentValue))
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
                // Calculate variance - mass should be static
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
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsInMassRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 1.0 && val <= 500.0;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsNearNormalMass(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 50.0 && val <= 150.0;
        }
        catch
        {
            return false;
        }
    }
}