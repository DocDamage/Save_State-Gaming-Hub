using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting brake temperature in racing sims.
/// Brake temp values typically:
/// - Are floats (Celsius or Fahrenheit)
/// - Increase during braking
/// - Cool down gradually
/// </summary>
public sealed class BrakeTemperatureHeuristic : IValueHeuristic
{
    public string Name => "Brake Temperature Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int heatEvents = 0;
        int coolEvents = 0;

        // Check value range (brake temp typically 20-1000°C)
        if (IsInBrakeTempRange(value.CurrentValue))
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

            // Check for heating (braking)
            if (currVal > prevVal)
            {
                heatEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Braking heats up brakes quickly
                if (delta > 10 && delta < 200)
                {
                    score += 0.15;
                }
            }

            // Check for cooling
            if (currVal < prevVal)
            {
                coolEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Cooling is gradual
                if (delta > 0 && delta < 50)
                {
                    score += 0.1;
                }
            }

            // Reasonable temperature range
            if (currVal > 1200 || currVal < -50)
            {
                score -= 0.3;
            }
        }

        // Bonus for heat/cool patterns
        if (heatEvents >= 2)
            score += 0.15;
        if (coolEvents >= 2)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsInBrakeTempRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= -50 && val <= 1500;
        }
        catch
        {
            return false;
        }
    }
}