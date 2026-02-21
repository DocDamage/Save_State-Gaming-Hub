using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting network upload speed in multiplayer games.
/// Upload speed values typically:
/// - Are integers (KB/s or bytes/s)
/// - Fluctuate based on game state
/// - Higher during action moments
/// - Range from 1-1000 KB/s typically
/// </summary>
public sealed class UploadSpeedHeuristic : IValueHeuristic
{
    public string Name => "Upload Speed Detection";
    public string Category => "Multiplayer";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasFluctuation = false;
        bool alwaysPositive = true;

        // Check value range (upload typically 0-10000 KB/s)
        if (IsInUploadRange(value.CurrentValue))
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

            // Check for fluctuation (bandwidth varies)
            if (Math.Abs(currVal.Value - prevVal.Value) > 10)
            {
                hasFluctuation = true;
            }

            // Should always be positive or zero
            if (currVal < 0)
            {
                alwaysPositive = false;
                score -= 0.5;
            }

            // Reasonable max check
            if (currVal > 100000)
            {
                score -= 0.3;
            }
        }

        // Bonus for fluctuation (realistic network behavior)
        if (hasFluctuation)
            score += 0.2;

        // Bonus for always positive
        if (alwaysPositive && history.Count > 2)
            score += 0.15;

        // Check for typical upload speeds
        var avgValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Average();

        // Common ranges: 10-100 KB/s (normal), 100-500 (busy)
        if (avgValue >= 1 && avgValue <= 1000)
        {
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "int64" or "long";
    }

    private static bool IsInUploadRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 50000;
        }
        catch
        {
            return false;
        }
    }
}