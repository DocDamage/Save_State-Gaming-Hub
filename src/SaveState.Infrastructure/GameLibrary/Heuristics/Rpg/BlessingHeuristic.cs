using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting blessing/divine favor in RPG games.
/// Blessing values typically:
/// - Are integers (0-100) representing favor level
/// - Decrease when using divine powers
/// - Increase through prayer/temple visits
/// </summary>
public sealed class BlessingHeuristic : IValueHeuristic
{
    public string Name => "Blessing/Divine Favor Detection";
    public string Category => "RPG";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int depletionEvents = 0;
        int gainEvents = 0;

        // Check value range (blessing typically 0-100)
        if (IsInBlessingRange(value.CurrentValue))
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

            // Check for depletion (using divine powers)
            if (currVal < prevVal)
            {
                depletionEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Divine powers cost significant blessing
                if (delta >= 5 && delta <= 50)
                {
                    score += 0.12;
                }
            }

            // Check for gain (prayer/temple)
            if (currVal > prevVal)
            {
                gainEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Prayer gives steady blessing
                if (delta > 0 && delta < 30)
                {
                    score += 0.1;
                }
            }

            // Should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Typically caps at 100
            if (currVal > 200)
            {
                score -= 0.3;
            }
        }

        // Bonus for depletion/gain patterns
        if (depletionEvents >= 1)
            score += 0.15;
        if (gainEvents >= 1)
            score += 0.1;

        // Check for max of 100
        var maxValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Max();

        if (Math.Abs(maxValue - 100) < 5)
        {
            score += 0.1;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "float" or "single";
    }

    private static bool IsInBlessingRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 200;
        }
        catch
        {
            return false;
        }
    }
}