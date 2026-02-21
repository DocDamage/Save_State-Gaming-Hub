using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting paused state values in game memory.
/// Paused state values typically:
/// - Are binary flags (0 or 1)
/// - Toggle between states
/// - Remain constant during each state
/// - Often correlate with menu visibility
/// </summary>
public sealed class IsPausedHeuristic : IValueHeuristic
{
    public string Name => "Paused State Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check for binary flag pattern
        if (IsBinaryFlag(value.CurrentValue))
        {
            score += 0.4;
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
            int constantPeriods = 0;
            bool? lastValue = null;
            int currentRun = 0;

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

                if (lastValue.HasValue)
                {
                    if (lastValue.Value != isOne)
                    {
                        toggles++;
                        // End of a constant period
                        if (currentRun >= 2)
                        {
                            constantPeriods++;
                        }
                        currentRun = 0;
                    }
                }

                currentRun++;
                lastValue = isOne;
            }

            // Final constant period
            if (currentRun >= 2)
            {
                constantPeriods++;
            }

            // Paused state should have both 0 and 1 values
            var total = zeros + ones;
            if (total > 0)
            {
                var zeroRatio = (double)zeros / total;
                if (zeroRatio > 0.3 && zeroRatio < 0.9)
                {
                    score += 0.2;
                }
            }

            // Should toggle between states
            if (toggles >= 1 && toggles <= 5)
            {
                score += 0.15;
            }

            // Should have periods of constant state
            if (constantPeriods >= 1)
            {
                score += 0.05;
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