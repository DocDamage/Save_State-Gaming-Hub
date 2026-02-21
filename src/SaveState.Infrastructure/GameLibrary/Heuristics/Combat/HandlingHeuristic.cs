using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting weapon handling values in game memory.
/// Handling values typically:
/// - Are floats in range 0.0-100.0 (handling rating)
/// - Static or change with weapon/attachment upgrades
/// - Affects aim down sights speed and weapon swap speed
/// - Higher values mean more responsive weapons
/// </summary>
public sealed class HandlingHeuristic : IValueHeuristic
{
    public string Name => "Weapon Handling Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int increases = 0;
        int decreases = 0;

        // Check value range
        if (IsInHandlingRange(value.CurrentValue))
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

            // Changes typically from attachments/upgrades
            if (Math.Abs(delta) > 0.001 && Math.Abs(delta) < 20)
            {
                score += 0.05;
            }

            // Check for attack correlation (handling affects attacks)
            if (curr.RelatedAction == PlayerAction.Attacked && delta != 0)
            {
                score += 0.15;
            }

            // Check for upgrade correlation
            if (curr.RelatedAction == PlayerAction.LeveledUp && delta > 0)
            {
                score += 0.1;
            }
        }

        // Handling should be relatively static per weapon
        if (history.Count > 1)
        {
            var changeRatio = (double)(increases + decreases) / (history.Count - 1);
            if (changeRatio < 0.1)
            {
                score += 0.2;
            }
        }

        // Usually increases with better gear
        if (increases >= decreases)
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

    private static bool IsInHandlingRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            // Handling typically 0-100
            return val >= 0.0 && val <= 100.0;
        }
        catch
        {
            return false;
        }
    }
}