using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting lives/continues in arcade/platformer games.
/// Lives values typically:
/// - Are small integers (0-99)
/// - Decrease when player dies
/// - Can be increased by collecting items or reaching checkpoints
/// - Game over when reaching 0
/// </summary>
public sealed class LivesHeuristic : IValueHeuristic
{
    public string Name => "Lives/Continues Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int deathEvents = 0;
        int gainEvents = 0;
        int smallIntegerCount = 0;

        // Check value range (lives typically 0-99)
        if (IsInLivesRange(value.CurrentValue))
        {
            score += 0.4;
        }

        // Must be integer type
        if (!HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score -= 0.3;
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

            // Check for small integer values
            if (currVal >= 0 && currVal <= 20)
            {
                smallIntegerCount++;
            }

            // Check for life loss (decrease by 1 after death)
            if (currVal == prevVal - 1 && curr.RelatedAction == PlayerAction.Died)
            {
                deathEvents++;
                score += 0.2;
            }

            // Check for life gain (increase, typically by 1)
            if (currVal == prevVal + 1)
            {
                gainEvents++;
                score += 0.15;
            }

            // Lives should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Lives rarely exceed 99
            if (currVal > 99)
            {
                score -= 0.4;
            }
        }

        // Bonus for death events pattern
        if (deathEvents >= 1)
            score += 0.15;

        // Bonus for gain events
        if (gainEvents >= 1)
            score += 0.1;

        // Strong bonus for consistent small integer values
        if (smallIntegerCount >= 3)
            score += 0.2;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "int64" or "long" or "byte";
    }

    private static bool IsInLivesRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Lives typically in range 0-99
            var val = doubleValue.Value;
            return val >= 0 && val <= 99;
        }
        catch
        {
            return false;
        }
    }
}