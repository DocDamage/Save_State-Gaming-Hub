using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting current map type/view mode.
/// Map Type values typically:
/// - Are integers (0-5) representing different map views
/// - Change when switching between world/local/dungeon maps
/// - Values often small enum-like integers
/// </summary>
public sealed class MapTypeHeuristic : IValueHeuristic
{
    public string Name => "Map Type/Mode Detection";
    public string Category => "Map";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int typeChanges = 0;

        // Check value range (map types typically 0-10)
        if (IsInMapTypeRange(value.CurrentValue))
        {
            score += 0.45;
        }

        // Must be integer (enum-like)
        if (!HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score -= 0.4;
        }
        else
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

            // Map type changes should be distinct values
            if (currVal != prevVal)
            {
                typeChanges++;
                var delta = Math.Abs(currVal.Value - prevVal.Value);
                // Usually changes by 1 or to specific values
                if (delta <= 3)
                {
                    score += 0.15;
                }
            }

            // Should be non-negative for enums
            if (currVal < 0)
            {
                score -= 0.4;
            }

            // Reasonable maximum for enum
            if (currVal > 20)
            {
                score -= 0.3;
            }

            // Common map type values (0-5)
            if (currVal >= 0 && currVal <= 5)
            {
                score += 0.1;
            }
        }

        // Type changes should be relatively infrequent
        if (typeChanges >= 1 && typeChanges <= history.Count / 5)
        {
            score += 0.1;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "byte";
    }

    private static bool IsInMapTypeRange(object? value)
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