using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting weapon holster speed values in game memory.
/// Holster speed values typically:
/// - Are floats in range 0.1-3.0 (seconds)
/// - Static or change with weapon/skill upgrades
/// - Time to stow/unequip the weapon
/// - Often inverse of draw speed or separate stat
/// </summary>
public sealed class HolsterSpeedHeuristic : IValueHeuristic
{
    public string Name => "Holster Speed Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int changes = 0;
        int weaponSwitchCorrelations = 0;

        // Check value range
        if (IsInHolsterSpeedRange(value.CurrentValue))
        {
            score += 0.45;
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

            // Holster speed changes typically from attachments/skills
            if (Math.Abs(delta) > 0.05 && Math.Abs(delta) < 1.0)
            {
                score += 0.1;
            }

            // Holster speed changes on equipment change (tracked via attack)
            if (curr.RelatedAction == PlayerAction.Attacked && delta != 0)
            {
                weaponSwitchCorrelations++;
                score += 0.2;
            }
        }

        // Holster speed should be relatively static per weapon
        if (history.Count > 1)
        {
            var changeRatio = (double)changes / (history.Count - 1);
            if (changeRatio < 0.1)
            {
                score += 0.25;
            }
        }

        // Strong bonus for weapon switch correlations
        if (weaponSwitchCorrelations >= 1)
        {
            score += 0.1;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double";
    }

    private static bool IsInHolsterSpeedRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            // Holster speed typically 0.1-3.0 seconds
            return val >= 0.1 && val <= 3.0;
        }
        catch
        {
            return false;
        }
    }
}