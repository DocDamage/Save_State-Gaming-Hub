using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting weapon recoil values in game memory.
/// Recoil values typically:
/// - Are floats in range 0.0-100.0 (recoil magnitude)
/// - Static or change with weapon/attachment upgrades
/// - Higher for powerful weapons, lower for stable weapons
/// - May have vertical and horizontal components
/// </summary>
public sealed class RecoilHeuristic : IValueHeuristic
{
    public string Name => "Weapon Recoil Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int changes = 0;
        int firingCorrelations = 0;

        // Check value range
        if (IsInRecoilRange(value.CurrentValue))
        {
            score += 0.35;
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

            // Recoil changes typically from attachments/upgrades
            if (Math.Abs(delta) > 1 && Math.Abs(delta) < 20)
            {
                score += 0.05;
            }

            // Check for firing correlation
            if (curr.RelatedAction == PlayerAction.UsedAmmo && delta == 0)
            {
                firingCorrelations++;
            }

            // Check for attack correlation (recoil affects attacks)
            if (curr.RelatedAction == PlayerAction.Attacked && delta != 0)
            {
                score += 0.15;
            }
        }

        // Recoil should be relatively static per weapon
        if (history.Count > 1)
        {
            var changeRatio = (double)changes / (history.Count - 1);
            if (changeRatio < 0.1)
            {
                score += 0.25;
            }
        }

        // Bonus for firing correlations
        if (firingCorrelations >= 2)
        {
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsInRecoilRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            // Recoil typically 0-100
            return val >= 0.0 && val <= 100.0;
        }
        catch
        {
            return false;
        }
    }
}