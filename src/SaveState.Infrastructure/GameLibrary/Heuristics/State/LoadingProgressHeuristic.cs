using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting loading screen progress.
/// Loading values typically:
/// - Are floats (0.0-100.0) or integers (0-100)
/// - Only increase
/// - Reset to 0 on new load
/// </summary>
public sealed class LoadingProgressHeuristic : IValueHeuristic
{
    public string Name => "Loading Progress Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool onlyIncreases = true;
        int resetEvents = 0;

        // Check value range (loading 0-100)
        if (IsInLoadingRange(value.CurrentValue))
        {
            score += 0.4;
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

            // Check for steady increase
            if (currVal > prevVal)
            {
                var delta = currVal.Value - prevVal.Value;
                // Loading increases steadily
                if (delta > 0 && delta < 20)
                {
                    score += 0.1;
                }
            }
            // Check for reset (new load)
            else if (currVal == 0 && prevVal > 50)
            {
                resetEvents++;
                score += 0.2;
            }
            // Should not decrease otherwise
            else if (currVal < prevVal)
            {
                onlyIncreases = false;
                score -= 0.3;
            }

            // Should not exceed 100
            if (currVal > 100)
            {
                score -= 0.5;
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }
        }

        // Bonus for only increasing
        if (onlyIncreases && history.Count > 2)
            score += 0.2;

        // Bonus for reset events
        if (resetEvents >= 1)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsInLoadingRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}