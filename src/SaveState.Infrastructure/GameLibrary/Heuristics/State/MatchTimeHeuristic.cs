using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting match/round time in multiplayer games.
/// Match time values typically:
/// - Are integers or floats (seconds remaining)
/// - Count down from set time
/// - Cause match end at 0
/// </summary>
public sealed class MatchTimeHeuristic : IValueHeuristic
{
    public string Name => "Match/Round Time Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool steadyDecrease = true;
        double totalDecrease = 0;

        // Check value range (match time typically 0-3600 seconds)
        if (IsInMatchTimeRange(value.CurrentValue))
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

            // Time should decrease
            if (currVal < prevVal)
            {
                totalDecrease += prevVal.Value - currVal.Value;
            }
            else if (currVal > prevVal)
            {
                // Time rarely increases (overtime only)
                steadyDecrease = false;
            }

            // Check for common match times
            var commonTimes = new[] { 300.0, 600.0, 900.0, 1200.0, 1800.0, 3600.0 };
            foreach (var time in commonTimes)
            {
                if (Math.Abs(prevVal.Value - time) < 10 && currVal < prevVal)
                {
                    score += 0.2;
                    break;
                }
            }

            // Should not go negative
            if (currVal < 0)
            {
                score -= 0.3;
            }
        }

        // Bonus for steady decrease
        if (steadyDecrease && history.Count > 2)
            score += 0.2;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsInMatchTimeRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 7200;
        }
        catch
        {
            return false;
        }
    }
}