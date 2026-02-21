using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting jetpack fuel values in game memory.
/// Jetpack fuel values typically:
/// - Are floats in range 0.0-100.0
/// - Deplete when flying, recharge when not
/// - Often shown as percentage
/// - Common in sci-fi games with vertical mobility
/// </summary>
public sealed class JetpackFuelHeuristic : IValueHeuristic
{
    public string Name => "Jetpack Fuel Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int depletionCount = 0;
        int rechargeCount = 0;
        double prevVal = 0;
        bool hasBeenZero = false;
        bool hasBeenFull = false;

        // Check value range
        if (IsInJetpackFuelRange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Analyze observation history
        for (int i = 0; i < history.Count; i++)
        {
            if (history[i].Value == null)
                continue;

            double? currVal = HeuristicUtilities.ConvertToDouble(history[i].Value);
            if (!currVal.HasValue)
                continue;

            var val = currVal.Value;

            // Track if has been zero or full
            if (val < 1.0) hasBeenZero = true;
            if (val > 95.0) hasBeenFull = true;

            // Detect depletion (fuel going down)
            if (i > 0 && val < prevVal - 0.1)
            {
                depletionCount++;
            }

            // Detect recharge (fuel going up)
            if (i > 0 && val > prevVal + 0.1)
            {
                rechargeCount++;
            }

            prevVal = val;

            // Fuel should never be negative
            if (val < 0)
            {
                score -= 0.3;
            }
        }

        // Bonus for both depletion and recharge patterns
        if (depletionCount >= 2 && rechargeCount >= 2)
        {
            score += 0.35;
        }

        // Bonus for having been at zero
        if (hasBeenZero)
        {
            score += 0.1;
        }

        // Bonus for having been full
        if (hasBeenFull)
        {
            score += 0.1;
        }

        // Correlation with vertical position changes
        int movementEvents = history.Count(h => h.RelatedAction == PlayerAction.PositionChanged);
        if (movementEvents >= 2)
        {
            score += 0.1;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsInJetpackFuelRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0.0 && val <= 100.0;
        }
        catch
        {
            return false;
        }
    }
}