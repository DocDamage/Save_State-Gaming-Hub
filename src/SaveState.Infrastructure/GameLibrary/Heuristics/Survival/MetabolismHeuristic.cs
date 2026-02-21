using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting metabolic rate values in survival games.
/// Metabolism values typically:
/// - Are floats (0.5-2.0 multiplier or 0-100% efficiency)
/// - Affect how quickly hunger and thirst deplete
/// - Increase with physical activity and cold exposure
/// - Decrease during rest and hibernation
/// </summary>
public sealed class MetabolismHeuristic : IValueHeuristic
{
    public string Name => "Metabolism Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int activityCorrelations = 0;
        int restCorrelations = 0;
        bool floatPrecisionPattern = false;

        // Check value range (metabolism typically 0.5-2.0 or 0-100)
        if (IsInMetabolismRange(value.CurrentValue))
        {
            score += 0.3;
        }

        // Check for float type (metabolism often uses multipliers)
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

            // Check for increased metabolism during activity
            if (currVal > prevVal && (curr.RelatedAction == PlayerAction.Sprinted || 
                                       curr.RelatedAction == PlayerAction.Attacked))
            {
                var delta = currVal.Value - prevVal.Value;
                if (delta > 0 && delta < 0.5)
                {
                    activityCorrelations++;
                    score += 0.15;
                }
            }

            // Check for decreased metabolism during rest
            if (currVal < prevVal && curr.RelatedAction == PlayerAction.Idle)
            {
                var delta = prevVal.Value - currVal.Value;
                if (delta > 0 && delta < 0.3)
                {
                    restCorrelations++;
                    score += 0.12;
                }
            }

            // Check for decimal precision (metabolism multipliers)
            if (currVal.Value != Math.Floor(currVal.Value))
            {
                floatPrecisionPattern = true;
            }

            // Metabolism should be positive
            if (currVal <= 0)
            {
                score -= 0.5;
            }

            // Reasonable max for metabolism
            if (currVal > 5.0)
            {
                score -= 0.3;
            }

            // Check for typical metabolic values
            if (currVal >= 0.5 && currVal <= 2.0)
            {
                score += 0.1;
            }
        }

        // Bonus for activity correlations
        if (activityCorrelations >= 2)
            score += 0.15;

        // Bonus for rest correlations
        if (restCorrelations >= 2)
            score += 0.12;

        // Bonus for float precision
        if (floatPrecisionPattern)
            score += 0.15;

        // Check for typical average (1.0 baseline)
        var avgValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(1.0)
            .Average();

        if (avgValue >= 0.8 && avgValue <= 1.5)
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

    private static bool IsInMetabolismRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Metabolism: 0.5-2.0 (multiplier) or 0-100 (percentage)
            var val = doubleValue.Value;
            return (val >= 0.5 && val <= 2.0) || (val >= 0 && val <= 100);
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