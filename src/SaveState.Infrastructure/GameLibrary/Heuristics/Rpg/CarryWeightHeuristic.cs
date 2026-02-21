using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting inventory carry weight values in game memory.
/// Carry weight typically:
/// - Are floats in range 0.0-10000.0
/// - Current weight fluctuates
/// - Max weight is static
/// </summary>
public sealed class CarryWeightHeuristic : IValueHeuristic
{
    public string Name => "Carry Weight Detection";
    public string Category => "RPG";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int increases = 0;
        int decreases = 0;

        // Check value range
        if (IsInCarryWeightRange(value.CurrentValue))
        {
            score += 0.3;
        }

        // Analyze observation history
        for (int i = 1; i < history.Count; i++)
        {
            var prev = history[i - 1];
            var curr = history[i];

            if (prev.Value == null || curr.Value == null)
                continue;

            double? prevVal = HeuristicUtilities.ConvertToDouble(prev.Value);
            double? currVal = HeuristicUtilities.ConvertToDouble(curr.Value);

            if (!prevVal.HasValue || !currVal.HasValue)
                continue;

            var delta = currVal.Value - prevVal.Value;

            if (delta > 0)
            {
                increases++;
            }
            else if (delta < 0)
            {
                decreases++;
            }

            // Weight should never be negative
            if (currVal.Value < 0)
            {
                score -= 0.3;
            }
        }

        // Both increases (pick up items) and decreases (drop items) should occur
        if (increases >= 1 && decreases >= 1)
        {
            score += 0.25;
        }

        // Weight fluctuates with inventory changes
        if (increases + decreases >= 2)
        {
            score += 0.2;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double";
    }

    private static bool IsInCarryWeightRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0.0 && val <= 10000.0;
        }
        catch
        {
            return false;
        }
    }
}