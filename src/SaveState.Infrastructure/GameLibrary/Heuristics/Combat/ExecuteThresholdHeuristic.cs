using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting execute threshold values in game memory.
/// Execute threshold values typically:
/// - Are floats in range 0.0-50.0 (percentage of max health)
/// - Static or change with skill upgrades
/// - Enemies below this threshold can be instantly killed
/// - Common in RPG and action games
/// </summary>
public sealed class ExecuteThresholdHeuristic : IValueHeuristic
{
    public string Name => "Execute Threshold Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int increases = 0;
        int decreases = 0;
        int killCorrelations = 0;

        // Check value range
        if (IsInThresholdRange(value.CurrentValue))
        {
            score += 0.4;
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
                increases++;
            else if (delta < 0)
                decreases++;

            // Small changes indicate skill upgrades
            if (Math.Abs(delta) > 0.001 && Math.Abs(delta) < 10)
            {
                score += 0.05;
            }

            // Check for kill correlation (execute often triggers on score increase)
            if (curr.RelatedAction == PlayerAction.ScoreIncreased && delta == 0)
            {
                killCorrelations++;
            }

            // Check for level up correlation
            if (curr.RelatedAction == PlayerAction.LeveledUp && delta > 0)
            {
                score += 0.15;
            }
        }

        // Execute threshold should be relatively static
        if (history.Count > 1)
        {
            var changeRatio = (double)(increases + decreases) / (history.Count - 1);
            if (changeRatio < 0.1)
            {
                score += 0.25;
            }
        }

        // Usually increases with skill upgrades
        if (increases >= decreases)
        {
            score += 0.1;
        }

        // Bonus for kill correlations
        if (killCorrelations >= 2)
        {
            score += 0.1;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsInThresholdRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            // Execute threshold typically 0-50% of health
            return (val >= 0.0 && val <= 50.0) || (val >= 0.0 && val <= 0.5);
        }
        catch
        {
            return false;
        }
    }
}