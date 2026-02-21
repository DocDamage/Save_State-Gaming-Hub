using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting game mode values in game memory.
/// Game mode values typically:
/// - Are small integers (0-20 range)
/// - Remain constant during gameplay in a specific mode
/// - Change only when switching game modes
/// - Often sequential (0, 1, 2, 3) or bit flags
/// </summary>
public sealed class GameModeHeuristic : IValueHeuristic
{
    public string Name => "Game Mode Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check value range for game modes (typically 0-20)
        if (IsInGameModeRange(value.CurrentValue))
        {
            score += 0.4;
        }

        // Game modes should be integers
        if (HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score += 0.2;
        }

        // Analyze observation history
        if (history.Count >= 2)
        {
            int constantPeriods = 0;
            int changes = 0;
            object? lastValue = null;

            for (int i = 0; i < history.Count; i++)
            {
                var current = history[i];
                if (current.Value == null)
                    continue;

                if (lastValue != null)
                {
                    var lastDouble = HeuristicUtilities.ConvertToDouble(lastValue);
                    var currDouble = HeuristicUtilities.ConvertToDouble(current.Value);
                    if (lastDouble.HasValue && currDouble.HasValue && HeuristicUtilities.AreValuesEqual(lastDouble.Value, currDouble.Value))
                    {
                        constantPeriods++;
                    }
                    else
                    {
                        changes++;
                    }
                }
                lastValue = current.Value;
            }

            // Game modes should mostly stay constant
            var totalComparisons = history.Count - 1;
            if (totalComparisons > 0)
            {
                var constancyRatio = (double)constantPeriods / totalComparisons;
                if (constancyRatio > 0.8)
                {
                    score += 0.3;
                }
            }

            // Changes should be infrequent
            if (changes <= 2)
            {
                score += 0.1;
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long" or "byte" or "uint8";
    }

    private static bool IsInGameModeRange(object? value)
    {
        if (value == null) return false;

        var doubleValue = HeuristicUtilities.ConvertToDouble(value);
        if (!doubleValue.HasValue) return false;

        var val = doubleValue.Value;
        return val >= 0 && val <= 20;
    }
}