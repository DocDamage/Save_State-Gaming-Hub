using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting round losses in multiplayer games (for round-based modes).
/// Round losses values typically:
/// - Are integers (0-20)
/// - Only increase during match
/// - Track opponent's round wins
/// - Reset between matches
/// </summary>
public sealed class RoundLossesHeuristic : IValueHeuristic
{
    public string Name => "Round Losses Detection";
    public string Category => "Multiplayer";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool onlyIncreases = true;
        int incrementEvents = 0;

        // Check value range (round losses typically 0-30)
        if (IsInRoundLossesRange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Must be integer
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

            // Check for increment
            if (currVal > prevVal)
            {
                incrementEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Usually gain 1 round loss at a time
                if (delta == 1)
                {
                    score += 0.15;
                }
            }
            // Should not decrease during match
            else if (currVal < prevVal)
            {
                onlyIncreases = false;
                // Might reset between matches
                if (prevVal > 5 && currVal < 3)
                {
                    score += 0.1; // Likely match reset
                }
                else
                {
                    score -= 0.3;
                }
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Reasonable max for round losses
            if (currVal > 50)
            {
                score -= 0.4;
            }
        }

        // Bonus for increment events
        if (incrementEvents >= 1)
            score += 0.15;

        // Bonus for only increasing
        if (onlyIncreases && history.Count > 2)
            score += 0.2;

        // Check for common round loss values
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue)
        {
            var commonLosses = new[] { 0.0, 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0, 10.0, 11.0, 12.0, 13.0, 15.0, 16.0 };
            foreach (var common in commonLosses)
            {
                if (Math.Abs(currentVal.Value - common) < 0.5)
                {
                    score += 0.1;
                    break;
                }
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "byte";
    }

    private static bool IsInRoundLossesRange(object? value)
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