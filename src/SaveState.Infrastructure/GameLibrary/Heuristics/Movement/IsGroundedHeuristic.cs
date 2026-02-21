using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting grounded state values in game memory.
/// IsGrounded values typically:
/// - Are boolean values (0 or 1, sometimes represented as floats)
/// - 1 when on ground, 0 when in air (jumping, falling)
/// - Changes rapidly during jumps
/// - Used for jump availability checks
/// </summary>
public sealed class IsGroundedHeuristic : IValueHeuristic
{
    public string Name => "Grounded State Detection";
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
            }
            else if (Math.Abs(val - 1.0) < 0.01)
            {
                oneCount++;
            }
            else if (val > 0.01 && val < 0.99)
            {
                // Values between 0 and 1 reduce confidence
                score -= 0.1;
            }

            // Count transitions (0 to 1 or 1 to 0)
            if (i > 0 && prevVal >= 0)
            {
                if ((prevVal < 0.5 && val >= 0.5) || (prevVal >= 0.5 && val < 0.5))
                {
                    transitions++;
                }
            }

            prevVal = val;
        }

        // Should have both 0 and 1 values
        if (zeroCount > 0 && oneCount > 0)
        {
            score += 0.4;
        }

        // Should have transitions (jumping/falling)
        if (transitions >= 2)
        {
            score += 0.3;
        }

        // Most values should be 0 or 1
        var totalValid = zeroCount + oneCount;
        if (history.Count > 0 && totalValid >= history.Count * 0.9)
        {
            score += 0.2;
        }

        // Correlation with jump events
        int jumpEvents = history.Count(h => h.RelatedAction == PlayerAction.Jumped);
        if (jumpEvents >= 1 && transitions >= 1)
        {
            score += 0.1;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int" or "bool" or "boolean";
    }
}