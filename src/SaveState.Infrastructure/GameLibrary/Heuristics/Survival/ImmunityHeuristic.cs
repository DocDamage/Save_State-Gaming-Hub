using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting immunity values in survival games.
/// Immunity values typically:
/// - Are floats or integers (0.0-100.0)
/// - Decrease when exposed to diseases or pathogens
/// - Recover slowly over time with rest and nutrition
/// - High immunity resists infections, low immunity increases susceptibility
/// </summary>
public sealed class ImmunityHeuristic : IValueHeuristic
{
    public string Name => "Immunity Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int exposureEvents = 0;
        int recoveryEvents = 0;
        bool slowRecoveryPattern = false;

        // Check value range (immunity typically 0-100)
        if (IsInImmunityRange(value.CurrentValue))
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

            // Check for immunity drop (exposure to disease)
            if (currVal < prevVal)
            {
                var delta = prevVal.Value - currVal.Value;
                // Sudden drops indicate exposure events
                if (delta > 5 && delta < 40)
                {
                    exposureEvents++;
                    score += 0.12;
                }
            }

            // Check for slow recovery (rest/nutrition)
            if (currVal > prevVal && curr.RelatedAction == PlayerAction.Idle)
            {
                var delta = currVal.Value - prevVal.Value;
                // Recovery is typically slow and gradual
                if (delta > 0 && delta < 3)
                {
                    recoveryEvents++;
                    slowRecoveryPattern = true;
                }
            }

            // Immunity should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Immunity typically caps at 100
            if (currVal > 150)
            {
                score -= 0.3;
            }
        }

        // Bonus for exposure pattern
        if (exposureEvents >= 1)
            score += 0.15;

        // Bonus for slow recovery pattern (distinctive of immunity)
        if (slowRecoveryPattern && recoveryEvents >= 3)
            score += 0.25;

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

    private static bool IsInImmunityRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Immunity typically in range 0-100
            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}