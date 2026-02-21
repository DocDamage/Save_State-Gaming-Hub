using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting regional server ping in multiplayer games.
/// Region ping values typically:
/// - Are integers (milliseconds)
/// - Vary by geographic region (NA, EU, ASIA, etc.)
/// - Stay relatively constant for a region
/// - Higher than local ping due to distance
/// </summary>
public sealed class RegionPingHeuristic : IValueHeuristic
{
    public string Name => "Region Ping Detection";
    public string Category => "Multiplayer";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool relativelyConstant = true;
        bool inNormalRange = true;

        // Check value range (region ping typically 20-300ms)
        if (IsInRegionPingRange(value.CurrentValue))
        {
            score += 0.4;
        }

        // Must be integer type
        if (!HeuristicUtilities.IsIntegerValue(value.CurrentValue))
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

            // Check for relative constancy (region ping is stable)
            if (Math.Abs(currVal.Value - prevVal.Value) > 20)
            {
                relativelyConstant = false;
            }

            // Check for normal range
            if (currVal > 300)
            {
                inNormalRange = false;
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Extremely high ping is concerning
            if (currVal > 500)
            {
                score -= 0.3;
            }
        }

        // Bonus for being relatively constant
        if (relativelyConstant && history.Count > 2)
            score += 0.25;

        // Bonus for normal range
        if (inNormalRange && history.Count > 1)
            score += 0.15;

        // Check for common region ping ranges
        var avgValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Average();

        // Regional ping ranges: 20-50 (local), 50-100 (nearby), 100-200 (distant), 200-300 (far)
        if (avgValue >= 20 && avgValue <= 300)
        {
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short";
    }

    private static bool IsInRegionPingRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 500;
        }
        catch
        {
            return false;
        }
    }
}