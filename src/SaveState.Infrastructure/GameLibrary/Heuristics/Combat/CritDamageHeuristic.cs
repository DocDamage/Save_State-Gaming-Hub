using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting critical damage multiplier in RPG games.
/// Crit damage values typically:
/// - Are floats (1.5-5.0) representing multiplier
/// - Relatively stable, change with gear/skills
/// - Usually between 1.5x and 3.0x base
/// </summary>
public sealed class CritDamageHeuristic : IValueHeuristic
{
    public string Name => "Critical Damage Multiplier Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool stableValue = true;

        // Check value range (crit damage typically 1.0-10.0)
        if (IsInCritDamageRange(value.CurrentValue))
        {
            score += 0.4;
        }

        // Float type preferred
        if (value.ValueType.ToLowerInvariant() is "float" or "single" or "double")
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

            // Check for stability (crit damage changes rarely)
            if (Math.Abs(currVal.Value - prevVal.Value) > 0.5)
            {
                stableValue = false;
            }

            // Common multiplier values
            var commonMults = new[] { 1.5, 2.0, 2.5, 3.0, 3.5, 4.0, 4.5, 5.0 };
            foreach (var mult in commonMults)
            {
                if (Math.Abs(currVal.Value - mult) < 0.1)
                {
                    score += 0.1;
                    break;
                }
            }

            // Should be positive
            if (currVal <= 0)
            {
                score -= 0.5;
            }

            // Unreasonably high
            if (currVal > 20)
            {
                score -= 0.3;
            }
        }

        // Bonus for stability
        if (stableValue && history.Count > 2)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsInCritDamageRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 1.0 && val <= 20.0;
        }
        catch
        {
            return false;
        }
    }
}