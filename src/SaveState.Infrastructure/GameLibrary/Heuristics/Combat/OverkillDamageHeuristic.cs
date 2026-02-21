using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting overkill damage values in game memory.
/// Overkill damage values typically:
/// - Are integers in range 0-999999
/// - Track excess damage dealt beyond enemy's remaining health
/// - Reset between encounters
/// - Used for scoring or special effects
/// </summary>
public sealed class OverkillDamageHeuristic : IValueHeuristic
{
    public string Name => "Overkill Damage Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int killCorrelations = 0;
        int zeroResets = 0;

        // Check value range
        if (IsInOverkillRange(value.CurrentValue))
        {
            score += 0.3;
        }

        // Overkill is typically an integer
        if (IsIntegerValue(value.CurrentValue))
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

            var delta = currVal.Value - prevVal.Value;

            // Overkill should never be negative
            if (currVal < 0)
            {
                score -= 0.3;
            }

            // Check for kill correlation (overkill happens on kills/score increase)
            if (curr.RelatedAction == PlayerAction.ScoreIncreased && delta >= 0)
            {
                killCorrelations++;
                score += 0.2;
            }

            // Check for reset to zero (new encounter)
            if (currVal == 0 && prevVal > 0)
            {
                zeroResets++;
                score += 0.1;
            }

            // Overkill values spike after combat
            if (delta > 100 && prevVal == 0)
            {
                score += 0.15;
            }

            // Check for used ability correlation
            if (curr.RelatedAction == PlayerAction.UsedAbility && delta >= 0)
            {
                killCorrelations++;
            }
        }

        // Strong bonus for kill correlations
        if (killCorrelations >= 2)
        {
            score += 0.2;
        }

        // Bonus for reset patterns
        if (zeroResets >= 1)
        {
            score += 0.1;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long" or "int16" or "short";
    }

    private static bool IsInOverkillRange(object? value)
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

    private static bool IsIntegerValue(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return true;

            return Math.Abs(doubleValue.Value % 1) < 0.0001;
        }
        catch
        {
            return false;
        }
    }
}