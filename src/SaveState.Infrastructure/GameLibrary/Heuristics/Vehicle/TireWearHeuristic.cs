using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting tire wear percentage in racing games.
/// Tire wear values typically:
/// - Are floats (0.0-100.0) representing percentage
/// - Gradually increase during racing
/// - Affect grip/handling
/// </summary>
public sealed class TireWearHeuristic : IValueHeuristic
{
    public string Name => "Tire Wear Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool gradualIncrease = true;

        // Check value range (wear 0-100%)
        if (IsInWearRange(value.CurrentValue))
        {
            score += 0.4;
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

            // Check for gradual increase
            if (currVal > prevVal)
            {
                var delta = currVal.Value - prevVal.Value;
                // Wear increases gradually
                if (delta > 0 && delta < 5)
                {
                    score += 0.15;
                }
                // Sudden jumps are suspicious
                else if (delta > 20)
                {
                    gradualIncrease = false;
                }
            }
            // Should not decrease (except pit stop)
            else if (currVal < prevVal && Math.Abs(currVal.Value - prevVal.Value) > 50)
            {
                score += 0.1; // Pit stop tire change
            }

            // Should not exceed 100
            if (currVal > 100)
            {
                score -= 0.5;
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }
        }

        // Bonus for gradual increase
        if (gradualIncrease && history.Count > 2)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsInWearRange(object? value)
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