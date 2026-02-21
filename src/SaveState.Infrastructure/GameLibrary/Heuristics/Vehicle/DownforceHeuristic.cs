using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting aerodynamic downforce in racing games.
/// Downforce values typically:
/// - Are floats (Newtons or percentage)
/// - Increase with speed
/// - Affected by aerodynamic settings
/// </summary>
public sealed class DownforceHeuristic : IValueHeuristic
{
    public string Name => "Aerodynamic Downforce Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool speedCorrelation = false;

        // Check value range (downforce varies widely)
        if (IsInDownforceRange(value.CurrentValue))
        {
            score += 0.3;
        }

        // Float type preferred
        if (value.ValueType.ToLowerInvariant() is "float" or "single" or "double")
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

            // Check for speed correlation (higher speed = more downforce)
            if (curr.RelatedAction == PlayerAction.Moved && currVal > prevVal)
            {
                speedCorrelation = true;
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.4;
            }
        }

        // Bonus for speed correlation
        if (speedCorrelation)
            score += 0.25;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsInDownforceRange(object? value)
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