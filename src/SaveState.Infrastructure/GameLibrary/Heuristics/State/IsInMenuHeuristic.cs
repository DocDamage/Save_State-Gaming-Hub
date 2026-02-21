using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting menu state values in game memory.
/// Menu state values typically:
/// - Are binary flags (0 or 1) or small integers (0-5)
/// - Remain 1 while in menus
/// - Toggle frequently during menu navigation
/// - Often 0 during gameplay
/// </summary>
public sealed class IsInMenuHeuristic : IValueHeuristic
{
    public string Name => "Menu State Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check for binary or small integer
        if (IsValidMenuState(value.CurrentValue))
        {
            score += 0.35;
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
            int nonZeros = 0;
            int toggles = 0;
            bool? lastValue = null;

            for (int i = 0; i < history.Count; i++)
            {
                var obs = history[i];
                if (obs.Value == null)
                    continue;

                var val = HeuristicUtilities.ConvertToDouble(obs.Value);
                if (!val.HasValue)
                    continue;

                bool isNonZero = val.Value > 0;

                if (isNonZero)
                {
                    nonZeros++;
                }
                else
                {
                    zeros++;
                }

                if (lastValue.HasValue && lastValue.Value != isNonZero)
                {
                    toggles++;
                }

                lastValue = isNonZero;
            }

            // Menu state should toggle frequently
            if (toggles >= 2 && toggles <= 6)
            {
                score += 0.25;
            }
            else if (toggles >= 1)
            {
                score += 0.1;
            }

            // Should have both in-menu and in-game time
            var total = zeros + nonZeros;
            if (total > 0)
            {
                var nonZeroRatio = (double)nonZeros / total;
                if (nonZeroRatio > 0.2 && nonZeroRatio < 0.8)
                {
                    score += 0.2;
                }
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long" or "byte" or "uint8" or "bool" or "boolean";
    }

    private static bool IsValidMenuState(object? value)
    {
        if (value == null) return false;

        var doubleValue = HeuristicUtilities.ConvertToDouble(value);
        if (!doubleValue.HasValue) return false;

        var val = doubleValue.Value;
        // Binary or small menu state identifier
        return val >= 0 && val <= 5;
    }
}