using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting suspension height in off-road/racing games.
/// Suspension values typically:
/// - Are floats (inches/cm from ground)
/// - Fluctuate over terrain
/// - Change with vehicle load
/// </summary>
public sealed class SuspensionHeightHeuristic : IValueHeuristic
{
    public string Name => "Suspension Height Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasFluctuation = false;

        // Check value range (suspension typically 1-50 inches/cm)
        if (IsInSuspensionRange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Float type preferred
        if (value.ValueType.ToLowerInvariant() is "float" or "single" or "double")
        {
            score += 0.15;
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

            // Check for fluctuation
            if (Math.Abs(currVal.Value - prevVal.Value) > 0.1)
            {
                hasFluctuation = true;
            }

            // Should be positive
            if (currVal <= 0)
            {
                score -= 0.4;
            }

            // Reasonable range
            if (currVal > 100)
            {
                score -= 0.3;
            }
        }

        // Bonus for fluctuation (suspension moves constantly)
        if (hasFluctuation)
            score += 0.25;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsInSuspensionRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}