using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting quest/mission progress in RPGs.
/// Quest progress values typically:
/// - Are integers (0-100) representing percentage
/// - Increase as objectives are completed
/// - Reset to 0 when quest is complete or new quest starts
/// </summary>
public sealed class QuestProgressHeuristic : IValueHeuristic
{
    public string Name => "Quest Progress Detection";
    public string Category => "RPG";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int progressEvents = 0;
        int resetEvents = 0;

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

            // Check for progress increase
            if (currVal > prevVal)
            {
                progressEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Quest progress increases by reasonable amounts
                if (delta > 0 && delta <= 50)
                {
                    score += 0.1;
                }
            }

            // Check for reset (quest complete/new quest)
            if (currVal == 0 && prevVal > 50)
            {
                resetEvents++;
                score += 0.2;
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

        // Bonus for quest completion pattern
        if (resetEvents >= 1)
            score += 0.2;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "float" or "single";
    }

    private static bool IsInProgressRange(object? value)
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