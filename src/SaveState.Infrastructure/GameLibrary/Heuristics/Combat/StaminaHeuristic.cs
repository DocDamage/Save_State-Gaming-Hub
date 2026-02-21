using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting stamina values used for sprinting, dodging, and physical actions.
/// Stamina values typically:
/// - Are floats (0.0-100.0) or integers (0-100, 0-200)
/// - Deplete during physical exertion (sprinting, dodging)
/// - Recover during rest/idle states
/// - Have a maximum cap (usually 100)
/// </summary>
public sealed class StaminaHeuristic : IValueHeuristic
{
    public string Name => "Stamina Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int depletionEvents = 0;
        int recoveryEvents = 0;
        bool sprintDepletion = false;

        // Check value range (stamina typically 0-200)
        if (IsInStaminaRange(value.CurrentValue))
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

            // Check for stamina depletion during sprint/physical action
            if (currVal < prevVal && curr.RelatedAction == PlayerAction.Sprinted)
            {
                depletionEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Stamina depletes faster than health/shield
                if (delta > 0 && delta < 20)
                {
                    sprintDepletion = true;
                    score += 0.15;
                }
            }

            // Check for recovery during idle
            if (currVal > prevVal && curr.RelatedAction == PlayerAction.Idle)
            {
                recoveryEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Recovery is typically steady
                if (delta > 0 && delta < 5)
                {
                    score += 0.1;
                }
            }

            // Stamina should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Stamina typically caps at 100 or 200
            if (currVal > 500)
            {
                score -= 0.3;
            }
        }

        // Bonus for sprint depletion pattern
        if (sprintDepletion)
            score += 0.15;

        // Bonus for depletion events
        if (depletionEvents >= 2)
            score += 0.1;

        // Bonus for recovery events
        if (recoveryEvents >= 2)
            score += 0.1;

        // Check for common max value (100)
        var maxValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Max();

        if (Math.Abs(maxValue - 100) < 5 || Math.Abs(maxValue - 200) < 10)
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

    private static bool IsInStaminaRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Stamina typically in range 0-500
            var val = doubleValue.Value;
            return val >= 0 && val <= 500;
        }
        catch
        {
            return false;
        }
    }
}