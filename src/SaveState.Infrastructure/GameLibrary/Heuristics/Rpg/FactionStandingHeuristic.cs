using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting faction standing in RPG games.
/// Faction standing values typically:
/// - Are integers in range -100 to 100 (or 0-10 for tiers)
/// - Represent relationship with NPC factions
/// - Affect prices, quests, and hostility
/// </summary>
public sealed class FactionStandingHeuristic : IValueHeuristic
{
    public string Name => "Faction Standing Detection";
    public string Category => "RPG";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int increases = 0;
        int decreases = 0;

        // Check value range
        if (IsInStandingRange(value.CurrentValue))
        {
            score += 0.4;
        }

        // Standing is typically integer
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
                // Increases from faction quests
                score += 0.1;
            }
            else if (delta < 0)
            {
                decreases++;
                // Decreases from hostile actions
                score += 0.05;
            }

            // Check for common standing boundaries
            if (Math.Abs(currVal.Value) <= 100 || (currVal.Value >= 0 && currVal.Value <= 10))
            {
                score += 0.1;
            }
        }

        // Standing changes in both directions are normal
        if (increases > 0 || decreases > 0)
        {
            score += 0.1;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short";
    }

    private static bool IsInStandingRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            // Can be negative (hostile) to positive (friendly)
            // Or 0-10 for tier systems
            return (val >= -100 && val <= 100) || (val >= 0 && val <= 20);
        }
        catch
        {
            return false;
        }
    }
}