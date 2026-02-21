using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting shelter quality/protection values in survival games.
/// Shelter quality values typically:
/// - Are floats or integers (0.0-100.0)
/// - Vary based on structure type and materials
/// - Affect temperature, weather protection, and rest quality
/// - Change when entering/exiting buildings or camps
/// </summary>
public sealed class ShelterQualityHeuristic : IValueHeuristic
{
    public string Name => "Shelter Quality Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int locationChangeEvents = 0;
        int protectionEvents = 0;
        bool stepwisePattern = false;

        // Check value range (shelter quality typically 0-100)
        if (IsInShelterQualityRange(value.CurrentValue))
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

            // Check for shelter changes on movement
            if (Math.Abs(currVal.Value - prevVal.Value) > 10 && 
                Math.Abs(currVal.Value - prevVal.Value) < 80 &&
                curr.RelatedAction == PlayerAction.Moved)
            {
                locationChangeEvents++;
                stepwisePattern = true;
                score += 0.2;
            }

            // Check for protection benefits (high values during harsh conditions)
            if (currVal > 70 && curr.RelatedAction == PlayerAction.Idle)
            {
                protectionEvents++;
                score += 0.1;
            }

            // Check for stable shelter value (buildings maintain quality)
            if (HeuristicUtilities.AreValuesEqual(currVal.Value, prevVal.Value) && 
                curr.RelatedAction == PlayerAction.Idle)
            {
                score += 0.05;
            }

            // Shelter quality should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Shelter quality typically caps at 100
            if (currVal > 100)
            {
                score -= 0.3;
            }

            // Check for typical shelter tier values
            if (currVal == 0 || currVal == 25 || currVal == 50 || currVal == 75 || currVal == 100)
            {
                score += 0.08;
            }
        }

        // Strong bonus for location change events
        if (locationChangeEvents >= 2)
            score += 0.2;

        // Bonus for protection events
        if (protectionEvents >= 2)
            score += 0.12;

        // Bonus for stepwise pattern (distinctive of location-based values)
        if (stepwisePattern)
            score += 0.15;

        // Check for max value near 100
        var maxValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Max();

        if (Math.Abs(maxValue - 100) < 5)
        {
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "int32" or "int" or "double" or "int16" or "short";
    }

    private static bool IsInShelterQualityRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Shelter quality typically in range 0-100
            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}