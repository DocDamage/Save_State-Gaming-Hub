using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting combat rating/item level in RPG games.
/// Combat rating values typically:
/// - Are integers in range 1-1000+
/// - Increase with better gear
/// - Relatively stable between equipment changes
/// </summary>
public sealed class CombatRatingHeuristic : IValueHeuristic
{
    public string Name => "Combat Rating Detection";
    public string Category => "RPG";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool stableValue = true;
        int significantChanges = 0;

        // Check value range
        if (IsInRatingRange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Rating is typically integer
        if (HeuristicUtilities.IsIntegerValue(value.CurrentValue))
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

            var delta = Math.Abs(currVal.Value - prevVal.Value);

            // Check for stability - rating changes with gear
            if (delta > 50)
            {
                stableValue = false;
                significantChanges++;
            }

            // Should be positive
            if (currVal.Value <= 0)
            {
                score -= 0.5;
            }

            // Common rating milestones
            var commonRatings = new[] { 100.0, 200.0, 300.0, 400.0, 500.0, 600.0, 800.0, 1000.0 };
            foreach (var rating in commonRatings)
            {
                if (Math.Abs(currVal.Value - rating) < 5)
                {
                    score += 0.1;
                    break;
                }
            }
        }

        // Rating should be relatively stable (changes with gear upgrades)
        if (stableValue && history.Count > 2)
        {
            score += 0.15;
        }

        // Some gear changes expected during play
        if (significantChanges >= 1)
        {
            score += 0.1;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long";
    }

    private static bool IsInRatingRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 1 && val <= 10000;
        }
        catch
        {
            return false;
        }
    }
}