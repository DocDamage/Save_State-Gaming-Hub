using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting kill/death ratio in multiplayer games.
/// K/D ratio values typically:
/// - Are floats (0.0-50.0)
/// - Change as kills/deaths occur
/// - Common ranges: 0.5-5.0 for most players
/// - Can be very high for skilled players
/// </summary>
public sealed class KdRatioHeuristic : IValueHeuristic
{
    public string Name => "K/D Ratio Detection";
    public string Category => "Multiplayer";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool nonNegative = true;
        int changeEvents = 0;

        // Check value range (K/D typically 0-100)
        if (IsInKdrRange(value.CurrentValue))
        {
            score += 0.4;
        }

        // Float type is expected for ratios
        if (HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score -= 0.2;
        }
        else
        {
            score += 0.1;
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

            // Check for changes (K/D changes slowly)
            if (!HeuristicUtilities.AreValuesEqual(currVal.Value, prevVal.Value))
            {
                changeEvents++;
                var delta = Math.Abs(currVal.Value - prevVal.Value);
                // Usually changes by small amounts
                if (delta <= 1.0)
                {
                    score += 0.1;
                }
            }

            // Should not be negative
            if (currVal < 0)
            {
                nonNegative = false;
                score -= 0.5;
            }

            // Reasonable max check
            if (currVal > 1000)
            {
                score -= 0.4;
            }
        }

        // Bonus for change events
        if (changeEvents >= 1)
            score += 0.1;

        // Bonus for non-negative
        if (nonNegative && history.Count > 1)
            score += 0.15;

        // Check for common K/D ranges
        var avgValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Average();

        // Most players have K/D between 0.2 and 5.0
        if (avgValue >= 0.2 && avgValue <= 5.0)
        {
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int" or "int64" or "long";
    }

    private static bool IsInKdrRange(object? value)
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