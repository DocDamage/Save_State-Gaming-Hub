using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting reputation/faction standing in RPG games.
/// Reputation values typically:
/// - Are integers, often with both positive and negative ranges
/// - Change based on player actions with factions
/// - Have named thresholds (Hated, Hostile, Neutral, Friendly, Exalted)
/// - Often range from -10000 to +10000
/// </summary>
public sealed class ReputationHeuristic : IValueHeuristic
{
    public string Name => "Reputation/Faction Standing Detection";
    public string Category => "Resource";

#pragma warning disable CA1502 // Cyclomatic complexity acceptable for heuristic pattern matching
    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int gainEvents = 0;
        int lossEvents = 0;
        bool hasNegativeValues = false;
        bool hasPositiveValues = false;

        // Check value range (reputation often has both positive and negative)
        if (IsInReputationRange(value.CurrentValue))
        {
            score += 0.25;
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

            // Track positive and negative ranges
            if (currVal < 0)
                hasNegativeValues = true;
            if (currVal > 0)
                hasPositiveValues = true;

            // Check for reputation gain
            if (currVal > prevVal)
            {
                gainEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Reputation gains are typically small to moderate
                if (delta > 0 && delta <= 500)
                {
                    score += 0.1;
                }
            }

            // Check for reputation loss
            if (currVal < prevVal)
            {
                lossEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Reputation losses
                if (delta > 0 && delta <= 500)
                {
                    score += 0.1;
                }
            }

            // Check for crossing zero (Unfriendly to Friendly or vice versa)
            if ((prevVal < 0 && currVal >= 0) || (prevVal >= 0 && currVal < 0))
            {
                score += 0.15;
            }
        }

        // Bonus for bidirectional range (can be positive and negative)
        if (hasNegativeValues && hasPositiveValues)
            score += 0.2;

        // Bonus for gain/loss events
        if (gainEvents >= 1 || lossEvents >= 1)
            score += 0.15;

        // Check for common reputation thresholds
        var values = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToList();

        if (values.Any())
        {
            // Common WoW-style reputation thresholds
            var thresholds = new[] { -42000.0, -6000.0, -3000.0, 0.0, 3000.0, 6000.0, 9000.0, 21000.0, 42000.0 };
            var maxVal = values.Max();
            var minVal = values.Min();

            foreach (var threshold in thresholds)
            {
                if (Math.Abs(maxVal - threshold) < 100 || Math.Abs(minVal - threshold) < 100)
                {
                    score += 0.1;
                    break;
                }
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }
#pragma warning restore CA1502

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long" or "float" or "single" or "double";
    }

    private static bool IsInReputationRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Reputation typically in range -50000 to +50000
            var val = doubleValue.Value;
            return val >= -50000 && val <= 50000;
        }
        catch
        {
            return false;
        }
    }
}