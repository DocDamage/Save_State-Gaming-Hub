using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting pet/minion loyalty in RPG games.
/// Pet loyalty values typically:
/// - Are integers in range 0-100 (happiness/loyalty percentage)
/// - Decrease if neglected, increase with care
/// - Affect pet performance and obedience
/// </summary>
public sealed class PetLoyaltyHeuristic : IValueHeuristic
{
    public string Name => "Pet Loyalty Detection";
    public string Category => "RPG";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int increases = 0;
        int decreases = 0;

        // Check value range (loyalty typically 0-100)
        if (IsInLoyaltyRange(value.CurrentValue))
        {
            score += 0.4;
        }

        // Loyalty is typically integer
        if (HeuristicUtilities.IsIntegerValue(value.CurrentValue))
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

            if (delta > 0)
            {
                increases++;
                // Small increases from feeding/caring
                if (delta <= 5)
                {
                    score += 0.1;
                }
            }
            else if (delta < 0)
            {
                decreases++;
                // Slow decay when neglected
                if (Math.Abs(delta) <= 2)
                {
                    score += 0.1;
                }
            }

            // Should be non-negative
            if (currVal.Value < 0)
            {
                score -= 0.5;
            }

            // Should not exceed 100
            if (currVal.Value > 100)
            {
                score -= 0.3;
            }
        }

        // Loyalty changes slowly in both directions
        if (increases > 0 && decreases > 0)
        {
            score += 0.1;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "float" or "single";
    }

    private static bool IsInLoyaltyRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}