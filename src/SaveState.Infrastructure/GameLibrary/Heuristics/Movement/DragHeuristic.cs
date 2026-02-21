using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting air drag coefficient values in game memory.
/// Drag values typically:
/// - Are floats in range 0.0-2.0 (air resistance multiplier)
/// - Static for characters, may change for special abilities or vehicles
/// - Affects deceleration when no input is applied
/// </summary>
public sealed class DragHeuristic : IValueHeuristic
{
    public string Name => "Air Drag Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check value range - drag coefficient typically 0.0 to 2.0
        if (IsInDragRange(value.CurrentValue))
        {
            score += 0.4;
        }

        // Check if value is near common drag values (0.0-0.5 for most games)
        if (IsNearNormalDrag(value.CurrentValue))
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
                // Calculate variance - drag should be mostly static
                var avg = values.Average();
                var variance = values.Average(v => Math.Pow(v - avg, 2));

                // Low variance means mostly static
                if (variance < 0.01)
                {
                    score += 0.3;
                }

                // All values should be non-negative
                var negativeCount = values.Count(v => v < 0);
                if (negativeCount == 0)
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

    private static bool IsInDragRange(object? value)
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

    private static bool IsNearNormalDrag(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0.0 && val <= 0.5;
        }
        catch
        {
            return false;
        }
    }
}