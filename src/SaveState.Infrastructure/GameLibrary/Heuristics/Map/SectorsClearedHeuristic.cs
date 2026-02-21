using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting cleared sector/zone count.
/// Sectors Cleared values typically:
/// - Are integers (0-50)
/// - Increase when completing sector objectives
/// - Often used in Ubisoft-style open world games
/// </summary>
public sealed class SectorsClearedHeuristic : IValueHeuristic
{
    public string Name => "Sectors Cleared Count Detection";
    public string Category => "Map";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool onlyIncreases = true;

        // Check value range (sectors typically 0-50)
        if (IsInSectorsRange(value.CurrentValue))
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
                // Sectors usually cleared one at a time
                if (delta == 1)
                {
                    score += 0.18;
                }
                else if (delta > 1 && delta <= 3)
                {
                    score += 0.08;
                }
            }
            else
            {
                onlyIncreases = false;
                score -= 0.4;
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Reasonable maximum
            if (currVal > 200)
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

    private static bool IsInSectorsRange(object? value)
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