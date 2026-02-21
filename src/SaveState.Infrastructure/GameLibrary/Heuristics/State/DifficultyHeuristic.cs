using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting game difficulty level values in game memory.
/// Difficulty values typically:
/// - Are integers in range 0-4 (Easy, Normal, Hard, Nightmare, etc.)
/// - Static
/// - Rarely change
/// </summary>
public sealed class DifficultyHeuristic : IValueHeuristic
{
    public string Name => "Difficulty Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int changes = 0;

        // Check value range
        if (IsInDifficultyRange(value.CurrentValue))
        {
            score += 0.4;
        }

        // Difficulty is always an integer
        if (HeuristicUtilities.IsIntegerValue(value.CurrentValue))
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

            if (Math.Abs(currVal.Value - prevVal.Value) > 0.001)
            {
                changes++;
            }
        }

        // Difficulty should be very static (rarely changes)
        if (history.Count > 1)
        {
            var changeRatio = (double)changes / (history.Count - 1);
            if (changeRatio < 0.05)
            {
                score += 0.3;
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "byte";
    }

    private static bool IsInDifficultyRange(object? value)
    {
        if (value == null) return false;

        var doubleValue = HeuristicUtilities.ConvertToDouble(value);
        if (!doubleValue.HasValue) return false;

        var val = doubleValue.Value;
        return val >= 0 && val <= 10; // 0-4 typical, allow some flexibility
    }
}