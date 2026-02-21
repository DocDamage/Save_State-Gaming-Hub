using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting checkpoint/respawn point IDs in linear games.
/// Checkpoint values typically:
/// - Are integers starting from 0 or 1
/// - Only increase as player progresses
/// - Never decrease (unless restarting level)
/// </summary>
public sealed class CheckpointHeuristic : IValueHeuristic
{
    public string Name => "Checkpoint/Respawn Point Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool onlyIncreases = true;
        int incrementEvents = 0;
        bool smallValues = true;

        // Check value range (checkpoints typically 0-100)
        if (IsInCheckpointRange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Must be integer type
        if (!HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score -= 0.3;
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

            // Check for small values
            if (currVal > 100)
            {
                smallValues = false;
            }

            // Check for increment by 1
            if (currVal == prevVal + 1)
            {
                incrementEvents++;
                score += 0.2;
            }
            // Larger increments are less common
            else if (currVal > prevVal + 1)
            {
                score += 0.05;
            }

            // Check for any decrease
            if (currVal < prevVal)
            {
                onlyIncreases = false;
                score -= 0.3;
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }
        }

        // Bonus for increment by 1 pattern
        if (incrementEvents >= 2)
            score += 0.2;

        // Bonus for only increasing
        if (onlyIncreases && history.Count > 2)
            score += 0.15;

        // Bonus for small values
        if (smallValues)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "byte";
    }

    private static bool IsInCheckpointRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 500;
        }
        catch
        {
            return false;
        }
    }
}