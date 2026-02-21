using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting engine oil temperature in racing sims.
/// Oil temp values typically:
/// - Are floats (Celsius)
/// - Start cold and warm up
/// - Overheat if pushed too hard
/// </summary>
public sealed class OilTemperatureHeuristic : IValueHeuristic
{
    public string Name => "Oil Temperature Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool warmUpDetected = false;

        // Check value range (oil temp typically 20-150°C)
        if (IsInOilTempRange(value.CurrentValue))
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

            // Check for warm-up pattern
            if (currVal > prevVal && prevVal < 60 && currVal < 100)
            {
                warmUpDetected = true;
            }

            // Common operating temps
            var commonTemps = new[] { 60.0, 70.0, 80.0, 90.0, 100.0, 110.0 };
            foreach (var temp in commonTemps)
            {
                if (Math.Abs(currVal.Value - temp) < 5)
                {
                    score += 0.1;
                    break;
                }
            }

            // Reasonable range
            if (currVal > 200 || currVal < -20)
            {
                score -= 0.3;
            }
        }

        // Bonus for warm-up pattern
        if (warmUpDetected)
            score += 0.2;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsInOilTempRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= -20 && val <= 200;
        }
        catch
        {
            return false;
        }
    }
}