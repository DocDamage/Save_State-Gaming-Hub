using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting life steal percentage in RPG games.
/// Life steal values typically:
/// - Are floats (0.0-50.0) representing percentage
/// - Relatively stable, change with gear
/// - Stackable from multiple sources
/// </summary>
public sealed class LifeStealHeuristic : IValueHeuristic
{
    public string Name => "Life Steal Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool stableValue = true;

        // Check value range (life steal typically 0-100%)
        if (IsInLifeStealRange(value.CurrentValue))
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

            // Check for stability (life steal changes rarely)
            if (Math.Abs(currVal.Value - prevVal.Value) > 5)
            {
                stableValue = false;
            }

            // Common percentage values
            var commonPcts = new[] { 5.0, 10.0, 15.0, 20.0, 25.0, 30.0 };
            foreach (var pct in commonPcts)
            {
                if (Math.Abs(currVal.Value - pct) < 1)
                {
                    score += 0.15;
                    break;
                }
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Very high values are suspicious
            if (currVal > 100)
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

    private static bool IsInLifeStealRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}