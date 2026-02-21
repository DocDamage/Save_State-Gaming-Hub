using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting humidity values in survival games.
/// Humidity values typically:
/// - Are floats or integers (0.0-100.0 representing percentage)
/// - Affect temperature perception and stamina consumption
/// - High humidity makes heat worse, low humidity makes cold worse
/// - Changes slowly with weather and environment
/// </summary>
public sealed class HumidityHeuristic : IValueHeuristic
{
    public string Name => "Humidity Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool percentageRange = false;
        bool gradualChangePattern = false;
        int environmentCorrelations = 0;

        // Check value range (humidity is 0-100%)
        if (IsInHumidityRange(value.CurrentValue))
        {
            score += 0.35;
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

            // Check for percentage range (0-100)
            if (currVal >= 0 && currVal <= 100)
            {
                percentageRange = true;
            }

            // Check for gradual change (humidity changes slowly)
            if (curr.RelatedAction == PlayerAction.Idle)
            {
                var delta = Math.Abs(currVal.Value - prevVal.Value);
                if (delta > 0 && delta < 5)
                {
                    gradualChangePattern = true;
                    score += 0.08;
                }
            }

            // Check for environment correlation (humidity changes when moving to new areas)
            if (curr.RelatedAction == PlayerAction.Moved)
            {
                var delta = Math.Abs(currVal.Value - prevVal.Value);
                if (delta > 10)
                {
                    environmentCorrelations++;
                    score += 0.1;
                }
            }

            // Humidity should not go outside 0-100
            if (currVal < 0 || currVal > 100)
            {
                score -= 0.5;
            }

            // Check for typical humidity values
            if (currVal >= 20 && currVal <= 90)
            {
                score += 0.05;
            }
        }

        // Strong bonus for percentage range
        if (percentageRange)
            score += 0.2;

        // Bonus for gradual change pattern
        if (gradualChangePattern)
            score += 0.15;

        // Bonus for environment correlations
        if (environmentCorrelations >= 2)
            score += 0.15;

        // Check for average near realistic humidity (20-80%)
        var avgValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(50)
            .Average();

        if (avgValue >= 20 && avgValue <= 80)
        {
            score += 0.1;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "int32" or "int" or "double" or "int16" or "short";
    }

    private static bool IsInHumidityRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Humidity is 0-100%
            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}