using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting matchmaking rating (MMR/ELO) in multiplayer games.
/// MMR values typically:
/// - Are integers (0-10000+)
/// - Change with wins/losses
/// - Start around 1000-2500
/// - Change by small amounts (+/- 10-30)
/// </summary>
public sealed class MatchmakingRatingHeuristic : IValueHeuristic
{
    public string Name => "Matchmaking Rating Detection";
    public string Category => "Multiplayer";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool moderateValues = true;
        int changeEvents = 0;

        // Check value range (MMR typically 0-10000)
        if (IsInMmrRange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Must be integer type
        if (!HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score -= 0.3;
        }
        else
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

            // Check for moderate values
            if (currVal > 50000)
            {
                moderateValues = false;
            }

            // Check for changes (MMR adjustments)
            if (currVal != prevVal)
            {
                changeEvents++;
                var delta = Math.Abs(currVal.Value - prevVal.Value);
                // MMR usually changes by 5-50 points
                if (delta >= 5 && delta <= 50)
                {
                    score += 0.2;
                }
                else if (delta > 50 && delta <= 100)
                {
                    score += 0.1;
                }
                else if (delta > 100)
                {
                    score -= 0.1; // Unusual large change
                }
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }
        }

        // MMR changes should be infrequent
        if (changeEvents == 0 && history.Count > 10)
            score += 0.15;
        else if (changeEvents <= 3)
            score += 0.1;

        // Bonus for moderate values
        if (moderateValues && history.Count > 1)
            score += 0.15;

        // Check for common MMR starting values
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue)
        {
            // Common starting MMRs: 1000, 1500, 2000, 2500
            if (currentVal.Value >= 500 && currentVal.Value <= 5000)
            {
                score += 0.1;
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short";
    }

    private static bool IsInMmrRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 50000;
        }
        catch
        {
            return false;
        }
    }
}