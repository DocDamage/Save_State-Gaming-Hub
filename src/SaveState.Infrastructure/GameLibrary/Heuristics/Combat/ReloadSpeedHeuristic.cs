using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting reload speed in shooter games.
/// Reload speed values typically:
/// - Are floats (seconds)
/// - Relatively stable per weapon
/// - Range from 0.5 to 5+ seconds
/// </summary>
public sealed class ReloadSpeedHeuristic : IValueHeuristic
{
    public string Name => "Reload Speed Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool stableValue = true;

        // Check value range (reload typically 0.5-10 seconds)
        if (IsInReloadRange(value.CurrentValue))
        {
            score += 0.4;
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

            // Check for stability (reload speed changes rarely)
            if (Math.Abs(currVal.Value - prevVal.Value) > 0.5)
            {
                stableValue = false;
            }

            // Common reload values
            var commonReloads = new[] { 0.8, 1.0, 1.5, 2.0, 2.5, 3.0, 4.0, 5.0 };
            foreach (var reload in commonReloads)
            {
                if (Math.Abs(currVal.Value - reload) < 0.1)
                {
                    score += 0.2;
                    break;
                }
            }

            // Should be positive
            if (currVal <= 0)
            {
                score -= 0.5;
            }

            // Unreasonably high
            if (currVal > 20)
            {
                score -= 0.3;
            }
        }

        // Bonus for stability
        if (stableValue && history.Count > 2)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsInReloadRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0.1 && val <= 20.0;
        }
        catch
        {
            return false;
        }
    }
}