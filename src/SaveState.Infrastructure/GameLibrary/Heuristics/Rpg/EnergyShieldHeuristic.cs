using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting energy shield values in RPG games.
/// Energy shield typically:
/// - Are integers or floats in range 0-99999
/// - Act as a secondary health pool
/// - Regenerates over time
/// </summary>
public sealed class EnergyShieldHeuristic : IValueHeuristic
{
    public string Name => "Energy Shield Detection";
    public string Category => "RPG";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int increases = 0;
        int decreases = 0;
        int regenEvents = 0;

        // Check value range
        if (IsInShieldRange(value.CurrentValue))
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

            var delta = currVal.Value - prevVal.Value;

            if (delta > 0)
            {
                increases++;
                // Small increases suggest regeneration
                if (delta < 10)
                {
                    regenEvents++;
                    score += 0.1;
                }
            }
            else if (delta < 0)
            {
                decreases++;
            }

            // Shield should never be negative
            if (currVal.Value < 0)
            {
                score -= 0.5;
            }
        }

        // Shield regenerates (shows increases)
        if (regenEvents >= 1)
        {
            score += 0.15;
        }

        // Takes damage (shows decreases)
        if (decreases >= 1)
        {
            score += 0.1;
        }

        // Both damage and regen expected
        if (increases >= 1 && decreases >= 1)
        {
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "float" or "single" or "double";
    }

    private static bool IsInShieldRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 999999;
        }
        catch
        {
            return false;
        }
    }
}