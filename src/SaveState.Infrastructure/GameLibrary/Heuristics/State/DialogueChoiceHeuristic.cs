using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting dialogue choice index in story games.
/// Choice values typically:
/// - Are small integers (1-4)
/// - Change rapidly during conversations
/// - Reset between dialogues
/// </summary>
public sealed class DialogueChoiceHeuristic : IValueHeuristic
{
    public string Name => "Dialogue Choice Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool smallValues = true;
        int changes = 0;

        // Check value range (choices 0-10)
        if (IsInChoiceRange(value.CurrentValue))
        {
            score += 0.4;
        }

        // Must be integer
        if (!HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score -= 0.3;
        }
        else
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

            // Check for small values
            if (currVal > 10)
            {
                smallValues = false;
            }

            // Check for changes
            if (currVal != prevVal)
            {
                changes++;
                // Rapid changes typical for menu navigation
                score += 0.1;
            }

            // Reasonable range
            if (currVal < 0 || currVal > 20)
            {
                score -= 0.4;
            }
        }

        // Bonus for small values
        if (smallValues)
            score += 0.2;

        // Bonus for changes
        if (changes >= 2)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "byte";
    }

    private static bool IsInChoiceRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 20;
        }
        catch
        {
            return false;
        }
    }
}