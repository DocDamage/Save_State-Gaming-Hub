using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting guild reputation in RPG games.
/// Guild reputation values typically:
/// - Are integers in range -10000 to 10000 (or 0-100 as percentage)
/// - Increase through guild quests and activities
/// - May decrease for negative actions
/// </summary>
public sealed class GuildReputationHeuristic : IValueHeuristic
{
    public string Name => "Guild Reputation Detection";
    public string Category => "RPG";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int increases = 0;
        int decreases = 0;

        // Check value range
        if (IsInReputationRange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Reputation is typically integer
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
                // Increases from completing quests
                if (curr.RelatedAction == PlayerAction.ScoreIncreased)
                {
                    score += 0.15;
                }
            }
            else if (delta < 0)
            {
                decreases++;
                // Can decrease for failed quests or hostile actions
                score += 0.05;
            }

            // Check for common reputation caps
            if (Math.Abs(currVal.Value - 100) < 2 || Math.Abs(currVal.Value - 1000) < 10 ||
                Math.Abs(currVal.Value - 10000) < 100)
            {
                score += 0.1;
            }
        }

        // Reputation changes in both directions are normal
        if (increases > 0 || decreases > 0)
        {
            score += 0.1;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long";
    }

    private static bool IsInReputationRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            // Can be negative (hostile) to very positive
            return val >= -50000 && val <= 50000;
        }
        catch
        {
            return false;
        }
    }
}