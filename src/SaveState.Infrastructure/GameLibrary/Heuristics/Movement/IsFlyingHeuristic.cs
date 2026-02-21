using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting flying state values in game memory.
/// IsFlying values typically:
/// - Are boolean values (0 or 1)
/// - 1 when flying/gliding, 0 when grounded
/// - Used in games with flight mechanics
/// - Changes when taking off or landing
/// </summary>
public sealed class IsFlyingHeuristic : IValueHeuristic
{
    public string Name => "Flying State Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int zeroCount = 0;
        int oneCount = 0;
        int transitions = 0;
        double prevVal = -1;
        int continuousOnes = 0;
        int maxContinuousOnes = 0;

        // Analyze observation history
        for (int i = 0; i < history.Count; i++)
        {
            if (history[i].Value == null)
                continue;

            double? currVal = HeuristicUtilities.ConvertToDouble(history[i].Value);
            if (!currVal.HasValue)
                continue;

            var val = currVal.Value;

            // Count 0s and 1s
            if (Math.Abs(val) < 0.01)
            {
                zeroCount++;
                if (continuousOnes > 0)
                {
                    maxContinuousOnes = Math.Max(maxContinuousOnes, continuousOnes);
                    continuousOnes = 0;
                }
            }
            else if (Math.Abs(val - 1.0) < 0.01)
            {
                oneCount++;
                continuousOnes++;
            }
            else
            {
                // Values between 0 and 1 reduce confidence
                score -= 0.1;
                if (continuousOnes > 0)
                {
                    maxContinuousOnes = Math.Max(maxContinuousOnes, continuousOnes);
                    continuousOnes = 0;
                }
            }

            // Count transitions
            if (i > 0 && prevVal >= 0)
            {
                if ((prevVal < 0.5 && val >= 0.5) || (prevVal >= 0.5 && val < 0.5))
                {
                    transitions++;
                }
            }

            prevVal = val;
        }

        // Update max continuous if ended on ones
        maxContinuousOnes = Math.Max(maxContinuousOnes, continuousOnes);

        // Should have both 0 and 1 values
        if (zeroCount > 0 && oneCount > 0)
        {
            score += 0.35;
        }

        // Flying typically has fewer transitions than other states
        if (transitions >= 1 && transitions <= 3)
        {
            score += 0.25;
        }

        // Flying can have extended durations
        if (maxContinuousOnes >= 5 || oneCount > history.Count * 0.3)
        {
            score += 0.2;
        }

        // Most values should be 0 or 1
        var totalValid = zeroCount + oneCount;
        if (history.Count > 0 && totalValid >= history.Count * 0.95)
        {
            score += 0.2;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int" or "bool" or "boolean";
    }
}