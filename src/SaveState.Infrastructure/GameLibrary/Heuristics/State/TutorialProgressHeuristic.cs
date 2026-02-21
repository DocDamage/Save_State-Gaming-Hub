using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting tutorial/onboarding progress.
/// Tutorial values typically:
/// - Are integers (0-100) representing percentage
/// - Only increase as steps completed
/// - Stay at 100 when complete
/// </summary>
public sealed class TutorialProgressHeuristic : IValueHeuristic
{
    public string Name => "Tutorial Progress Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool onlyIncreases = true;
        int stepEvents = 0;

        // Check value range (tutorial 0-100)
        if (IsInTutorialRange(value.CurrentValue))
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

            // Check for step completion
            if (currVal > prevVal)
            {
                stepEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Steps usually increment by fixed amounts
                if (delta >= 5 && delta <= 50)
                {
                    score += 0.15;
                }
            }
            // Should not decrease
            else if (currVal < prevVal)
            {
                onlyIncreases = false;
                score -= 0.3;
            }

            // Should not exceed 100
            if (currVal > 100)
            {
                score -= 0.5;
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }
        }

        // Bonus for step events
        if (stepEvents >= 1)
            score += 0.15;

        // Bonus for only increasing
        if (onlyIncreases && history.Count > 2)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsInTutorialRange(object? value)
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