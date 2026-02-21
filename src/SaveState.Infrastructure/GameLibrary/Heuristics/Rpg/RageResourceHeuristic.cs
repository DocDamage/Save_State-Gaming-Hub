using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting rage/fury resource in RPG games.
/// Rage values typically:
/// - Are integers or floats in range 0-100
/// - Generate when dealing or taking damage
/// - Deplete when using special abilities
/// </summary>
public sealed class RageResourceHeuristic : IValueHeuristic
{
    public string Name => "Rage Resource Detection";
    public string Category => "RPG";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int increases = 0;
        int decreases = 0;
        int maxHits = 0;

        // Check value range (rage typically 0-100 or 0-1000)
        if (IsInRageRange(value.CurrentValue))
        {
            score += 0.4;
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

            var delta = currVal.Value - prevVal.Value;

            if (delta > 0)
            {
                increases++;
                // Rage often generates in combat
                if (curr.RelatedAction == PlayerAction.Attacked || curr.RelatedAction == PlayerAction.TookDamage)
                {
                    score += 0.1;
                }
            }
            else if (delta < 0)
            {
                decreases++;
                // Large drops suggest ability use
                if (delta < -10)
                {
                    score += 0.1;
                }
            }

            // Track hitting max (rage often caps)
            if (currVal.Value >= 100 || currVal.Value >= 1000)
            {
                maxHits++;
            }

            // Rage should never be negative
            if (currVal.Value < 0)
            {
                score -= 0.5;
            }
        }

        // Rage should fluctuate (generate and spend)
        if (increases >= 1 && decreases >= 1)
        {
            score += 0.2;
        }

        // Bonus for hitting max (common rage behavior)
        if (maxHits >= 1)
        {
            score += 0.1;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "float" or "single" or "double";
    }

    private static bool IsInRageRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            // Usually 0-100 or 0-1000 or 0-120
            return val >= 0 && val <= 5000;
        }
        catch
        {
            return false;
        }
    }
}