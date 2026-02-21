using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting ready state values in game memory.
/// Ready state values typically:
/// - Are binary flags (0 or 1)
/// - Toggle when player readies/unreadies
/// - Often 0 initially, 1 when ready
/// - Reset between matches
/// </summary>
public sealed class IsReadyHeuristic : IValueHeuristic
{
    public string Name => "Ready State Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check for binary flag
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
        if (history.Count >= 2)
        {
            int zeros = 0;
            int ones = 0;
            int zeroToOne = 0;
            int oneToZero = 0;
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

                if (lastValue.HasValue)
                {
                    if (!lastValue.Value && isOne)
                    {
                        zeroToOne++;
                    }
                    else if (lastValue.Value && !isOne)
                    {
                        oneToZero++;
                    }
                }

                lastValue = isOne;
            }

            // Ready state should start as 0
            if (history.Count > 0 && history[0].Value != null)
            {
                var firstVal = HeuristicUtilities.ConvertToDouble(history[0].Value);
                if (firstVal.HasValue && firstVal.Value == 0)
                {
                    score += 0.15;
                }
            }

            // Should have toggled at least once (readied up)
            if (zeroToOne >= 1)
            {
                score += 0.15;
            }

            // Unready should be less common
            if (oneToZero <= 1)
            {
                score += 0.1;
            }

            // Should have both states
            if (zeros > 0 && ones > 0)
            {
                score += 0.1;
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