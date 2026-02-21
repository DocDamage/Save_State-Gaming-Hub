using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting loading state values in game memory.
/// Loading state values typically:
/// - Are binary flags (0 or 1) or small integers
/// - Briefly 1 during loading screens
/// - Mostly 0 during gameplay
/// - Have short durations when active
/// </summary>
public sealed class IsLoadingHeuristic : IValueHeuristic
{
    public string Name => "Loading State Detection";
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
        if (history.Count >= 3)
        {
            int zeros = 0;
            int ones = 0;
            int shortOnes = 0;
            int currentRun = 0;
            bool lastWasOne = false;

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
                    if (!lastWasOne)
                    {
                        currentRun = 1;
                    }
                    else
                    {
                        currentRun++;
                    }
                }
                else
                {
                    zeros++;
                    if (lastWasOne && currentRun <= 3)
                    {
                        shortOnes++;
                    }
                    currentRun = 0;
                }

                lastWasOne = isOne;
            }

            // Check final run
            if (lastWasOne && currentRun <= 3)
            {
                shortOnes++;
            }

            // Loading should be mostly 0 (not loading)
            var total = zeros + ones;
            if (total > 0)
            {
                var zeroRatio = (double)zeros / total;
                if (zeroRatio > 0.8)
                {
                    score += 0.25;
                }
                else if (zeroRatio > 0.6)
                {
                    score += 0.1;
                }
            }

            // Loading periods should be brief
            if (ones > 0 && shortOnes >= 1)
            {
                score += 0.15;
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