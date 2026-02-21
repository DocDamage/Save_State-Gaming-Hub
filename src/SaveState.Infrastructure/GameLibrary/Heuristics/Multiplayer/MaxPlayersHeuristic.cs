using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting maximum players allowed in a multiplayer lobby or match.
/// Max players values typically:
/// - Are small integers (2-256)
/// - Stay constant during a session
/// - Are common game mode values (4, 8, 16, 32, 64, 100)
/// - Never change during gameplay
/// </summary>
public sealed class MaxPlayersHeuristic : IValueHeuristic
{
    public string Name => "Max Players Detection";
    public string Category => "Multiplayer";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool isConstant = true;

        // Check value range (max players typically 2-256)
        if (IsInMaxPlayersRange(value.CurrentValue))
        {
            score += 0.4;
        }

        // Must be integer type
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

            // Check for constancy (max players rarely changes)
            if (currVal != prevVal)
            {
                isConstant = false;
                score -= 0.2;
            }

            // Should not be zero or negative
            if (currVal < 1)
            {
                score -= 0.5;
            }

            // Should have reasonable max
            if (currVal > 1000)
            {
                score -= 0.4;
            }
        }

        // Strong bonus for being constant (defining characteristic)
        if (isConstant && history.Count > 2)
            score += 0.3;

        // Check for common max player values
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue)
        {
            var commonMaxPlayers = new[] { 2.0, 4.0, 6.0, 8.0, 10.0, 12.0, 16.0, 20.0, 24.0, 32.0, 40.0, 50.0, 64.0, 100.0, 128.0, 256.0 };
            foreach (var common in commonMaxPlayers)
            {
                if (Math.Abs(currentVal.Value - common) < 0.5)
                {
                    score += 0.2;
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

    private static bool IsInMaxPlayersRange(object? value)
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