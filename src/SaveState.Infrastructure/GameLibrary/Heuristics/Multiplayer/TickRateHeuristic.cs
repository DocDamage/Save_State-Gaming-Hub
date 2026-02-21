using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting server tick rate in multiplayer games.
/// Tick rate values typically:
/// - Are integers (20, 30, 60, 64, 128, 144)
/// - Stay constant during gameplay
/// - Common values: 20, 30, 60, 64, 128
/// - Never change without server restart
/// </summary>
public sealed class TickRateHeuristic : IValueHeuristic
{
    public string Name => "Tick Rate Detection";
    public string Category => "Multiplayer";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool isConstant = true;

        // Check value range (tick rate typically 10-144)
        if (IsInTickRateRange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Must be integer
        if (!HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score -= 0.3;
        }
        else
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

            // Check for constancy (tick rate should never change)
            if (currVal != prevVal)
            {
                isConstant = false;
                score -= 0.3;
            }

            // Should not be negative or zero
            if (currVal <= 0)
            {
                score -= 0.5;
            }

            // Should be reasonable
            if (currVal > 1000)
            {
                score -= 0.4;
            }
        }

        // Very strong bonus for being constant
        if (isConstant && history.Count > 2)
            score += 0.35;

        // Check for common tick rate values
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue)
        {
            var commonRates = new[] { 10.0, 20.0, 30.0, 32.0, 60.0, 64.0, 120.0, 128.0, 144.0 };
            foreach (var common in commonRates)
            {
                if (Math.Abs(currentVal.Value - common) < 0.5)
                {
                    score += 0.15;
                    break;
                }
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "byte";
    }

    private static bool IsInTickRateRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 1 && val <= 1000;
        }
        catch
        {
            return false;
        }
    }
}