using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting discovered area/region count.
/// Areas Discovered values typically:
/// - Are integers (0-100)
/// - Increase as new areas are explored
/// - Often tied to map completion percentage
/// </summary>
public sealed class AreasDiscoveredHeuristic : IValueHeuristic
{
    public string Name => "Areas Discovered Count Detection";
    public string Category => "Map";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool onlyIncreases = true;

        // Check value range (areas typically 0-100)
        if (IsInAreasRange(value.CurrentValue))
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

            // Should only increase
            if (currVal >= prevVal)
            {
                var delta = currVal.Value - prevVal.Value;
                // Areas typically discovered one at a time
                if (delta == 1)
                {
                    score += 0.15;
                }
                else if (delta > 0 && delta <= 3)
                {
                    score += 0.08;
                }
            }
            else
            {
                onlyIncreases = false;
                score -= 0.35;
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Reasonable maximum
            if (currVal > 500)
            {
                score -= 0.3;
            }
        }

        // Bonus for only increasing pattern
        if (onlyIncreases && history.Count > 2)
            score += 0.2;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "byte";
    }

    private static bool IsInAreasRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 300;
        }
        catch
        {
            return false;
        }
    }
}