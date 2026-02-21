using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting honor points in RPG games.
/// Honor points typically:
/// - Are integers in range 0-99999
/// - Earned through PvP victories and honorable kills
/// - Spent on PvP rewards
/// </summary>
public sealed class HonorPointsHeuristic : IValueHeuristic
{
    public string Name => "Honor Points Detection";
    public string Category => "RPG";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int increases = 0;
        int decreases = 0;

        // Check value range
        if (IsInHonorRange(value.CurrentValue))
        {
            score += 0.3;
        }

        // Honor points are always integers
        if (HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score += 0.2;
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
                // Increases from PvP victories
                if (curr.RelatedAction == PlayerAction.Attacked)
                {
                    score += 0.15;
                }
            }
            else if (delta < 0)
            {
                decreases++;
                // Decreases when spending on rewards
                score += 0.05;
            }

            // Should never be negative
            if (currVal.Value < 0)
            {
                score -= 0.5;
            }
        }

        // Should have both increases and decreases
        if (increases >= 1 && decreases >= 1)
        {
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long";
    }

    private static bool IsInHonorRange(object? value)
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