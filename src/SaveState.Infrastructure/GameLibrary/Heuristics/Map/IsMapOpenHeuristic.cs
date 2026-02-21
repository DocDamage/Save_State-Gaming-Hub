using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting if map UI is currently open.
/// Is Map Open values typically:
/// - Are booleans (0/1) or small integers
/// - Toggle rapidly when opening/closing map
/// - Binary state (open/closed)
/// </summary>
public sealed class IsMapOpenHeuristic : IValueHeuristic
{
    public string Name => "Map Open State Detection";
    public string Category => "Map";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int toggleEvents = 0;
        int zeroCount = 0;
        int oneCount = 0;

        // Check value range (should be 0 or 1)
        if (IsInBinaryRange(value.CurrentValue))
        {
            score += 0.45;
        }

        // Must be integer
        if (!HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score -= 0.3;
        }
        else
        {
            score += 0.2;
        }

        // Analyze observation history
        for (int i = 0; i < history.Count; i++)
        {
            var curr = history[i];

            if (curr.Value == null)
                continue;

            double? currVal = HeuristicUtilities.ConvertToDouble(curr.Value);
            if (!currVal.HasValue)
                continue;

            // Count 0s and 1s
            if (currVal.Value == 0)
            {
                zeroCount++;
            }
            else if (currVal.Value == 1)
            {
                oneCount++;
            }
            else
            {
                // Should only be 0 or 1
                score -= 0.5;
            }

            // Check for toggles
            if (i > 0)
            {
                var prev = history[i - 1];
                if (prev.Value != null)
                {
                    double? prevVal = HeuristicUtilities.ConvertToDouble(prev.Value);
                    if (prevVal.HasValue && prevVal.Value != currVal.Value)
                    {
                        toggleEvents++;
                        // Toggle should be between 0 and 1
                        if ((prevVal.Value == 0 && currVal.Value == 1) ||
                            (prevVal.Value == 1 && currVal.Value == 0))
                        {
                            score += 0.15;
                        }
                    }
                }
            }
        }

        // Should have both 0s and 1s
        if (zeroCount > 0 && oneCount > 0)
        {
            score += 0.15;
        }

        // Bonus for toggle patterns
        if (toggleEvents >= 2)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "byte" or "bool" or "boolean";
    }

    private static bool IsInBinaryRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val == 0 || val == 1;
        }
        catch
        {
            return false;
        }
    }
}