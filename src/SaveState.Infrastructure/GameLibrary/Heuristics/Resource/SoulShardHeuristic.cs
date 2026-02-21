using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting soul shard/spirit resource in dark fantasy/RPG games.
/// Soul shard values typically:
/// - Are integers (0-999)
/// - Increase from defeating enemies or capturing souls
/// - Decrease when enchanting or summoning
/// </summary>
public sealed class SoulShardHeuristic : IValueHeuristic
{
    public string Name => "Soul Shards/Spirit Essence Detection";
    public string Category => "Resource";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int gainEvents = 0;
        int spendEvents = 0;

        // Check value range (soul shards typically 0-999)
        if (IsInSoulShardRange(value.CurrentValue))
        {
            score += 0.4;
        }

        // Must be integer
        if (!HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score -= 0.2;
        }
        else
        {
            score += 0.1;
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

            // Check for gain (defeating enemies)
            if (currVal > prevVal)
            {
                gainEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Souls gained based on enemy strength (1-100)
                if (delta >= 1 && delta <= 20)
                {
                    score += 0.18;
                }
                else if (delta >= 1 && delta <= 100)
                {
                    score += 0.12;
                }
            }

            // Check for spend (enchanting/summoning)
            if (currVal < prevVal)
            {
                spendEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Magic uses 10-500 soul shards
                if (delta >= 10 && delta <= 500)
                {
                    score += 0.15;
                }
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }
        }

        // Bonus for combat-gain pattern
        if (gainEvents >= 2)
            score += 0.15;
        if (spendEvents >= 1)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short";
    }

    private static bool IsInSoulShardRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 9999;
        }
        catch
        {
            return false;
        }
    }
}