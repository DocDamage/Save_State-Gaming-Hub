using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting fire rate/attack speed in shooter/action games.
/// Fire rate values typically:
/// - Are floats (rounds per second or attacks per second)
/// - Relatively stable based on weapon/skill
/// - Range from 0.5 to 30+
/// </summary>
public sealed class FireRateHeuristic : IValueHeuristic
{
    public string Name => "Fire Rate/Attack Speed Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool stableValue = true;

        // Check value range (fire rate typically 0.1-50)
        if (IsInFireRateRange(value.CurrentValue))
        {
            score += 0.35;
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

            // Check for stability (fire rate changes rarely)
            if (Math.Abs(currVal.Value - prevVal.Value) > 1.0)
            {
                stableValue = false;
            }

            // Common fire rate values
            var commonRates = new[] { 0.5, 1.0, 2.0, 3.0, 5.0, 6.0, 10.0, 15.0, 30.0 };
            foreach (var rate in commonRates)
            {
                if (Math.Abs(currVal.Value - rate) < 0.2)
                {
                    score += 0.15;
                    break;
                }
            }

            // Should be positive
            if (currVal <= 0)
            {
                score -= 0.5;
            }

            // Unreasonably high
            if (currVal > 100)
            {
                score -= 0.3;
            }
        }

        // Bonus for stability
        if (stableValue && history.Count > 2)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsInFireRateRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0.1 && val <= 100.0;
        }
        catch
        {
            return false;
        }
    }
}