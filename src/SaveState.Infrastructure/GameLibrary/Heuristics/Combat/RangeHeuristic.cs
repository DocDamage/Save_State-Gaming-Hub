using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting weapon range values in game memory.
/// Range values typically:
/// - Are floats in range 1.0-500.0 (meters/units)
/// - Static or change with weapon upgrades
/// - Higher for rifles, lower for pistols/shotguns
/// - Melee weapons typically have very short range (1-5)
/// </summary>
public sealed class RangeHeuristic : IValueHeuristic
{
    public string Name => "Weapon Range Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int changes = 0;

        // Check value range
        if (IsInRangeRange(value.CurrentValue))
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

            // Track changes
            if (Math.Abs(delta) > 0.001)
            {
                changes++;
            }

            // Range changes typically from weapon upgrades
            if (Math.Abs(delta) > 5 && Math.Abs(delta) < 100)
            {
                score += 0.08;
            }

            // Check for attack correlation (range affects attacks)
            if (curr.RelatedAction == PlayerAction.Attacked && delta != 0)
            {
                score += 0.15;
            }
        }

        // Range should be relatively static per weapon
        if (history.Count > 1)
        {
            var changeRatio = (double)changes / (history.Count - 1);
            if (changeRatio < 0.1)
            {
                score += 0.25;
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsInRangeRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            // Range typically 1-500 (meters/units)
            return val >= 1.0 && val <= 500.0;
        }
        catch
        {
            return false;
        }
    }
}