using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting faction reputation values in game memory.
/// Reputation values typically:
/// - Are integers in range -10000 to 10000
/// - Slowly change based on actions
/// - Can be negative (hostile) or positive (friendly)
/// </summary>
public sealed class ReputationHeuristic : IValueHeuristic
{
    public string Name => "Reputation Detection";
    public string Category => "RPG";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasNegative = false;
        bool hasPositive = false;
        int smallChanges = 0;

        // Check value range
        if (IsInReputationRange(value.CurrentValue))
        {
            score += 0.3;
        }

        // Reputation is typically an integer
        if (HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score += 0.15;
        }

        // Analyze observation history
        for (int i = 0; i < history.Count; i++)
        {
            if (history[i].Value == null)
                continue;

            double? currVal = HeuristicUtilities.ConvertToDouble(history[i].Value);
            if (!currVal.HasValue)
                continue;

            if (currVal.Value < 0) hasNegative = true;
            if (currVal.Value > 0) hasPositive = true;

            // Track small changes (slow reputation changes)
            if (i > 0 && history[i - 1].Value != null)
            {
                double? prevVal = HeuristicUtilities.ConvertToDouble(history[i - 1].Value);
                if (prevVal.HasValue)
                {
                    var delta = Math.Abs(currVal.Value - prevVal.Value);
                    if (delta > 0 && delta <= 100)
                    {
                        smallChanges++;
                    }
                }
            }
        }

        // Bonus for having both negative and positive values
        if (hasNegative && hasPositive)
        {
            score += 0.2;
        }

        // Bonus for small, gradual changes
        if (smallChanges >= 2)
        {
            score += 0.2;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long" or "int16" or "short";
    }

    private static bool IsInReputationRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= -10000 && val <= 10000;
        }
        catch
        {
            return false;
        }
    }
}
