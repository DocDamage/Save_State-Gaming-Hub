using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting overtime state values in game memory.
/// Overtime values typically:
/// - Are binary flags (0 or 1) or small integers
/// - Remain 0 during normal play
/// - Switch to 1 during overtime periods
/// - May reset after overtime ends
/// </summary>
public sealed class OvertimeHeuristic : IValueHeuristic
{
    public string Name => "Overtime Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check for binary flag pattern (0 or 1)
        if (IsBinaryFlag(value.CurrentValue))
        {
            score += 0.3;
        }

        // Should be integer
        if (HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score += 0.2;
        }

        // Analyze observation history
        if (history.Count >= 3)
        {
            int zeros = 0;
            int ones = 0;
            int toggles = 0;
            bool? lastValue = null;

            for (int i = 0; i < history.Count; i++)
            {
                var obs = history[i];
                if (obs.Value == null)
                    continue;

                var val = HeuristicUtilities.ConvertToDouble(obs.Value);
                if (!val.HasValue)
                    continue;

                bool isOne = val.Value >= 1;

                if (isOne)
                {
                    ones++;
                }
                else
                {
                    zeros++;
                }

                if (lastValue.HasValue && lastValue.Value != isOne)
                {
                    toggles++;
                }

                lastValue = isOne;
            }

            // Most of the time should be 0 (not in overtime)
            var total = zeros + ones;
            if (total > 0)
            {
                var zeroRatio = (double)zeros / total;
                if (zeroRatio > 0.7 && zeroRatio < 1.0)
                {
                    score += 0.25;
                }
                else if (zeroRatio >= 0.5 && zeroRatio < 1.0)
                {
                    score += 0.1;
                }
            }

            // Should have toggled at least once (entered overtime)
            if (toggles >= 1 && toggles <= 4)
            {
                score += 0.25;
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long" or "byte" or "uint8" or "bool" or "boolean";
    }

    private static bool IsBinaryFlag(object? value)
    {
        if (value == null) return false;

        var doubleValue = HeuristicUtilities.ConvertToDouble(value);
        if (!doubleValue.HasValue) return false;

        var val = doubleValue.Value;
        return val == 0 || val == 1;
    }
}