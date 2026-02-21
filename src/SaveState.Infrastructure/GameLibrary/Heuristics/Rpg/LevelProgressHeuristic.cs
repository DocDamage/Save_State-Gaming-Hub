using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting level progress/percentage in RPG games.
/// Level progress values typically:
/// - Are floats (0.0-100.0) or integers (0-100) representing percentage
/// - Increase when gaining XP toward next level
/// - Reset to 0 when level up occurs
/// - Often paired with current level number
/// </summary>
public sealed class LevelProgressHeuristic : IValueHeuristic
{
    public string Name => "Level Progress Detection";
    public string Category => "RPG";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int progressEvents = 0;
        int resetEvents = 0;
        bool hasConsistentIncrease = true;
        double? lastValue = null;

        // Check value range (progress typically 0-100)
        if (IsInProgressRange(value.CurrentValue))
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

            // Check for progress increase (gaining XP)
            if (currVal > prevVal && curr.RelatedAction == PlayerAction.GainedXp)
            {
                progressEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Progress increases by reasonable amounts
                if (delta > 0 && delta < 25)
                {
                    score += 0.1;
                }

                // Check for consistent direction (should only increase until reset)
                if (lastValue.HasValue && currVal < lastValue)
                {
                    hasConsistentIncrease = false;
                }
                lastValue = currVal;
            }

            // Check for reset (level up - goes to 0)
            if (currVal == 0 && prevVal > 50)
            {
                resetEvents++;
                score += 0.2;
                lastValue = null;
            }

            // Progress should not decrease (except reset)
            if (currVal < prevVal && currVal != 0)
            {
                score -= 0.3;
            }

            // Progress should not exceed 100
            if (currVal > 100)
            {
                score -= 0.5;
            }
        }

        // Bonus for progress events
        if (progressEvents >= 2)
            score += 0.15;

        // Bonus for level up pattern
        if (resetEvents >= 1)
            score += 0.2;

        // Bonus for consistent increase pattern
        if (hasConsistentIncrease && progressEvents >= 2)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "int32" or "int" or "double" or "int16" or "short";
    }

    private static bool IsInProgressRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Progress typically in range 0-100
            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}