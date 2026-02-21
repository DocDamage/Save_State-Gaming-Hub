using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting wind chill values in survival games.
/// Wind chill values typically:
/// - Are floats with decimal precision
/// - Represent perceived temperature due to wind
/// - Are always equal to or lower than actual temperature
/// - Affect body temperature and frostbite risk
/// </summary>
public sealed class WindChillHeuristic : IValueHeuristic
{
    public string Name => "Wind Chill Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool negativeValues = false;
        bool correlatedWithMovement = false;
        int movementCorrelation = 0;

        // Check value range (wind chill typically -100 to +50)
        if (IsInWindChillRange(value.CurrentValue))
        {
            score += 0.3;
        }

        // Check for float type (wind chill usually has decimals)
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

            // Check for negative values (common in cold environments)
            if (currVal < 0)
            {
                negativeValues = true;
            }

            // Check for correlation with movement (wind chill increases when moving)
            if (curr.RelatedAction == PlayerAction.Sprinted || 
                curr.RelatedAction == PlayerAction.Moved)
            {
                if (currVal < prevVal) // Wind chill gets worse (lower) when moving
                {
                    movementCorrelation++;
                }
            }

            // Check for decimal precision
            if (currVal.Value != Math.Floor(currVal.Value))
            {
                score += 0.05;
            }

            // Wind chill typically doesn't exceed 50°C
            if (currVal > 50)
            {
                score -= 0.3;
            }

            // Extreme wind chill should be rare
            if (currVal < -80)
            {
                score -= 0.2;
            }
        }

        // Bonus for negative values (common in wind chill)
        if (negativeValues)
            score += 0.15;

        // Bonus for movement correlation
        if (movementCorrelation >= 2)
        {
            score += 0.2;
            correlatedWithMovement = true;
        }

        // Check for typical wind chill values
        var avgValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Average();

        // Common wind chill ranges: -40 to +20
        if (avgValue >= -40 && avgValue <= 20)
        {
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsInWindChillRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Wind chill typically in range -100 to +50
            var val = doubleValue.Value;
            return val >= -100 && val <= 50;
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