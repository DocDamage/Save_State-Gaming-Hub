using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting game speed values in game memory.
/// Game speed values typically:
/// - Are floats representing speed multipliers
/// - Default to 1.0 (normal speed)
/// - Range from 0.0 (paused) to higher values
/// - Change during slow-motion or fast-forward
/// </summary>
public sealed class GameSpeedHeuristic : IValueHeuristic
{
    public string Name => "Game Speed Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check value range for game speed
        if (IsInSpeedRange(value.CurrentValue))
        {
            score += 0.4;
        }

        // Non-negative
        if (HeuristicUtilities.IsNonNegative(value.CurrentValue))
        {
            score += 0.1;
        }

        // Check if close to 1.0 (normal speed)
        if (IsNearNormalSpeed(value.CurrentValue))
        {
            score += 0.15;
        }

        // Analyze observation history
        if (history.Count >= 2)
        {
            int normalSpeeds = 0;
            int changes = 0;

            for (int i = 0; i < history.Count; i++)
            {
                var obs = history[i];
                if (obs.Value == null)
                    continue;

                var val = HeuristicUtilities.ConvertToDouble(obs.Value);
                if (!val.HasValue)
                    continue;

                if (Math.Abs(val.Value - 1.0) < 0.1)
                {
                    normalSpeeds++;
                }
            }

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

                if (Math.Abs(currVal.Value - prevVal.Value) > 0.01)
                {
                    changes++;
                }
            }

            // Game speed should mostly be at normal speed (1.0)
            var total = history.Count;
            if (total > 0)
            {
                var normalRatio = (double)normalSpeeds / total;
                if (normalRatio > 0.7)
                {
                    score += 0.25;
                }
            }

            // Occasional changes are expected (slow-mo, pause)
            if (changes >= 1 && changes <= 5)
            {
                score += 0.1;
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double";
    }

    private static bool IsInSpeedRange(object? value)
    {
        if (value == null) return false;

        var doubleValue = HeuristicUtilities.ConvertToDouble(value);
        if (!doubleValue.HasValue) return false;

        var val = doubleValue.Value;
        // Game speed typically 0.0 to 10.0
        return val >= 0 && val <= 10.0;
    }

    private static bool IsNearNormalSpeed(object? value)
    {
        if (value == null) return false;

        var doubleValue = HeuristicUtilities.ConvertToDouble(value);
        if (!doubleValue.HasValue) return false;

        var val = doubleValue.Value;
        return Math.Abs(val - 1.0) < 0.5;
    }
}