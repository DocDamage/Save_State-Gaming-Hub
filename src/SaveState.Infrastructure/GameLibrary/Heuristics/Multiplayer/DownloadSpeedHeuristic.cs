using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting network download speed in multiplayer games.
/// Download speed values typically:
/// - Are integers (KB/s or bytes/s)
/// - Higher than upload speed
/// - Fluctuate based on server updates
/// - Range from 10-5000 KB/s typically
/// </summary>
public sealed class DownloadSpeedHeuristic : IValueHeuristic
{
    public string Name => "Download Speed Detection";
    public string Category => "Multiplayer";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasFluctuation = false;
        bool alwaysPositive = true;

        // Check value range (download typically 0-20000 KB/s)
        if (IsInDownloadRange(value.CurrentValue))
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
            if (Math.Abs(currVal.Value - prevVal.Value) > 20)
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
            if (currVal > 200000)
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

        // Check for typical download speeds
        var avgValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Average();

        // Common ranges: 50-500 KB/s (normal), 500-2000 (busy)
        if (avgValue >= 10 && avgValue <= 5000)
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

    private static bool IsInDownloadRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 100000;
        }
        catch
        {
            return false;
        }
    }
}