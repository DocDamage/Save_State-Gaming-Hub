using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting body/environment temperature values in survival games.
/// Temperature values typically:
/// - Are floats with decimal precision (36.6, -5.2, etc.)
/// - Have an optimal range (comfort zone)
/// - Fluctuate based on environment (fire, cold areas, weather)
/// - Affect player health/stamina when extreme
/// </summary>
public sealed class TemperatureHeuristic : IValueHeuristic
{
    public string Name => "Temperature Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasFluctuation = false;
        bool hasDecimalPrecision = false;
        int optimalRangeCount = 0;

        // Check value range (temperature typically -50 to +100)
        if (IsInTemperatureRange(value.CurrentValue))
        {
            score += 0.25;
        }

        // Check for float type (temperature usually has decimals)
        if (IsFloatType(value.ValueType))
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

            // Check for fluctuation (temperature changes)
            if (Math.Abs(currVal.Value - prevVal.Value) > 0.1)
            {
                hasFluctuation = true;
                
                // Check if change correlates with environment
                if (curr.RelatedAction == PlayerAction.Idle)
                {
                    score += 0.05;
                }
            }

            // Check for decimal precision (temperatures are rarely whole numbers)
            if (currVal.Value != Math.Floor(currVal.Value))
            {
                hasDecimalPrecision = true;
            }

            // Check for optimal range (body temp ~36-37, comfort zone)
            if ((currVal >= 35 && currVal <= 39) || // Body temperature
                (currVal >= 18 && currVal <= 25))   // Room comfort
            {
                optimalRangeCount++;
            }

            // Extreme temperatures should trigger effects
            if (currVal < -40 || currVal > 60)
            {
                score -= 0.3;
            }
        }

        // Bonus for fluctuation pattern
        if (hasFluctuation)
            score += 0.15;

        // Bonus for decimal precision (distinctive of temperature)
        if (hasDecimalPrecision)
            score += 0.2;

        // Bonus for values in optimal range
        if (optimalRangeCount > 0)
            score += 0.1;

        // Check for values near common temperature references
        var avgValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Average();

        // Common reference points: 0 (freezing), 20-25 (room), 36.6 (body), 100 (boiling)
        var referencePoints = new[] { 0.0, 20.0, 25.0, 36.6, 37.0, 100.0 };
        foreach (var refPoint in referencePoints)
        {
            if (Math.Abs(avgValue - refPoint) < 2.0)
            {
                score += 0.1;
                break;
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsInTemperatureRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Temperature typically in range -100 to +200
            var val = doubleValue.Value;
            return val >= -100 && val <= 200;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsFloatType(string valueType)
    {
        var normalized = valueType.ToLowerInvariant();
        return normalized is "float" or "single" or "double";
    }
}