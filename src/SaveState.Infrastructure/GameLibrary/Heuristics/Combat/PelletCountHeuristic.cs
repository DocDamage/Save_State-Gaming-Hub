using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting pellet count values in game memory.
/// Pellet count values typically:
/// - Are small integers (4-20)
/// - Static per weapon
/// - Indicate projectiles per shot (shotguns)
/// - Common in shotgun and spread weapons
/// </summary>
public sealed class PelletCountHeuristic : IValueHeuristic
{
    public string Name => "Pellet Count Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int changes = 0;
        int weaponSwitchCorrelations = 0;

        // Check value range
        if (IsInPelletCountRange(value.CurrentValue))
        {
            score += 0.5;
        }

        // Pellet count is always an integer
        if (IsIntegerValue(value.CurrentValue))
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

            // Track changes
            if (Math.Abs(delta) > 0.001)
            {
                changes++;
            }

            // Pellet count changes on equipment change (tracked via attack)
            if (curr.RelatedAction == PlayerAction.Attacked && delta != 0)
            {
                weaponSwitchCorrelations++;
                score += 0.2;
            }
        }

        // Pellet count should be completely static per weapon
        if (history.Count > 1)
        {
            var changeRatio = (double)changes / (history.Count - 1);
            if (changeRatio < 0.05)
            {
                score += 0.3;
            }
        }

        // Strong bonus for weapon switch correlations
        if (weaponSwitchCorrelations >= 1)
        {
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "byte";
    }

    private static bool IsInPelletCountRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            // Pellet count typically 4-20 (shotgun shells)
            return val >= 4 && val <= 20;
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