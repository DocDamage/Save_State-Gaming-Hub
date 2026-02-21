using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting item durability values in game memory.
/// Durability values typically:
/// - Are integers in range 0-100 or 0-1000
/// - Slowly decrease with use
/// - Jump up on repair
/// </summary>
public sealed class DurabilityHeuristic : IValueHeuristic
{
    public string Name => "Durability Detection";
    public string Category => "Resource";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int decreases = 0;
        int repairJumps = 0;

        // Check value range
        if (IsInDurabilityRange(value.CurrentValue))
        {
            score += 0.3;
        }

        // Durability is always an integer
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

            var delta = currVal.Value - prevVal.Value;

            // Small decreases indicate wear and tear
            if (delta < 0 && delta >= -5)
            {
                decreases++;
            }

            // Large jumps indicate repair
            if (delta > 50)
            {
                repairJumps++;
            }

            // Durability should never be negative
            if (currVal.Value < 0)
            {
                score -= 0.3;
            }
        }

        // Slow decrease is characteristic of durability
        if (decreases >= 2)
        {
            score += 0.25;
        }

        // Repair jumps
        if (repairJumps >= 1)
        {
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "byte";
    }

    private static bool IsInDurabilityRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 1000;
        }
        catch
        {
            return false;
        }
    }
}
