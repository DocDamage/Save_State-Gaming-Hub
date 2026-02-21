using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting nitro/boost charges in racing games.
/// Nitro values typically:
/// - Are integers (0-3 charges) or percentage (0-100)
/// - Decrease when boosting
/// - Increase by driving well or pickups
/// </summary>
public sealed class NitroHeuristic : IValueHeuristic
{
    public string Name => "Nitro/Boost Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int depletionEvents = 0;
        int gainEvents = 0;
        bool smallValues = true;

        // Check value range (nitro typically 0-100 or 0-3)
        if (IsInNitroRange(value.CurrentValue))
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

            // Check for small values (distinctive of nitro)
            if (currVal > 10 && currVal <= 100)
            {
                // Percentage mode
            }
            else if (currVal > 3)
            {
                smallValues = false;
            }

            // Check for depletion (boosting)
            if (currVal < prevVal)
            {
                depletionEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Boost uses nitro quickly
                if (delta > 0)
                {
                    score += 0.12;
                }
            }

            // Check for gain (pickups or driving)
            if (currVal > prevVal)
            {
                gainEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Pickups give +1 charge or significant percentage
                if (delta == 1 || delta >= 20)
                {
                    score += 0.15;
                }
            }

            // Should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }
        }

        // Bonus for patterns
        if (depletionEvents >= 1)
            score += 0.1;
        if (gainEvents >= 1)
            score += 0.1;

        // Bonus for small values
        if (smallValues)
            score += 0.15;

        // Check for common max values
        var maxValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .Max();

        if (Math.Abs(maxValue - 3) < 0.5 || Math.Abs(maxValue - 100) < 5)
        {
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "float" or "single";
    }

    private static bool IsInNitroRange(object? value)
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