using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting token/special currency in arcade/gacha games.
/// Token values typically:
/// - Are integers (0-9999)
/// - Increase from mini-games, achievements, or daily rewards
/// - Decrease when playing gacha or arcade games
/// </summary>
public sealed class TokenHeuristic : IValueHeuristic
{
    public string Name => "Tokens/Special Currency Detection";
    public string Category => "Resource";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int gainEvents = 0;
        int spendEvents = 0;

        // Check value range (tokens typically 0-9999)
        if (IsInTokenRange(value.CurrentValue))
        {
            score += 0.35;
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

            // Check for gain (mini-games/rewards)
            if (currVal > prevVal)
            {
                gainEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Tokens often awarded in round numbers
                if (delta % 10 == 0 || delta % 50 == 0 || delta % 100 == 0)
                {
                    score += 0.15;
                }
                else if (delta >= 1 && delta <= 500)
                {
                    score += 0.1;
                }
            }

            // Check for spend (gacha/arcade)
            if (currVal < prevVal)
            {
                spendEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Gacha pulls cost round amounts (100, 300, etc.)
                if (delta % 100 == 0 || delta % 50 == 0 || delta % 10 == 0)
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

        // Bonus for round number patterns (typical of tokens)
        if (gainEvents >= 1)
            score += 0.1;
        if (spendEvents >= 1)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long";
    }

    private static bool IsInTokenRange(object? value)
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