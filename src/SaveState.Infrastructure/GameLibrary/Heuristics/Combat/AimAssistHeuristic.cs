using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting aim assist strength in console shooters.
/// Aim assist values typically:
/// - Are floats (0.0-1.0) representing strength
/// - Stable during gameplay
/// - Changed in settings
/// </summary>
public sealed class AimAssistHeuristic : IValueHeuristic
{
    public string Name => "Aim Assist Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool stableValue = true;

        // Check value range (aim assist 0.0-1.0)
        if (IsInAimAssistRange(value.CurrentValue))
        {
            score += 0.45;
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

            // Check for stability (settings don't change often)
            if (Math.Abs(currVal.Value - prevVal.Value) > 0.01)
            {
                stableValue = false;
            }

            // Common values
            var commonValues = new[] { 0.0, 0.25, 0.5, 0.75, 1.0 };
            foreach (var common in commonValues)
            {
                if (Math.Abs(currVal.Value - common) < 0.01)
                {
                    score += 0.15;
                    break;
                }
            }

            // Should be 0-1
            if (currVal < 0 || currVal > 1)
            {
                score -= 0.5;
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

    private static bool IsInAimAssistRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 1;
        }
        catch
        {
            return false;
        }
    }
}