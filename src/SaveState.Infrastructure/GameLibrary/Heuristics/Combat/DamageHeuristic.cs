using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting attack damage values in game memory.
/// Damage values typically:
/// - Are integers in range 1-99999
/// - Static, change only on equipment/level up
/// - Often have min/max damage pair
/// </summary>
public sealed class DamageHeuristic : IValueHeuristic
{
    public string Name => "Damage Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int changes = 0;

        // Check value range
        if (IsInDamageRange(value.CurrentValue))
        {
            score += 0.3;
        }

        // Damage is typically an integer
        if (IsIntegerValue(value.CurrentValue))
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

            // Track changes
            if (Math.Abs(delta) > 0.001)
            {
                changes++;
            }

            // Large jumps might indicate equipment changes
            if (Math.Abs(delta) > 100)
            {
                score += 0.05;
            }
        }

        // Damage should be relatively static (rare changes)
        if (history.Count > 1)
        {
            var changeRatio = (double)changes / (history.Count - 1);
            if (changeRatio < 0.1)
            {
                score += 0.25;
            }
        }

        // Bonus for damage correlation with level up
        int levelUpEvents = history.Count(h => h.RelatedAction == PlayerAction.LeveledUp);
        if (levelUpEvents >= 1 && changes >= 1)
        {
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long" or "int16" or "short";
    }

    private static bool IsInDamageRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 1 && val <= 99999;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsIntegerValue(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return true;

            return Math.Abs(doubleValue.Value % 1) < 0.0001;
        }
        catch
        {
            return false;
        }
    }
}