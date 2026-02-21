using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting terminal velocity values in game memory.
/// Terminal velocity values typically:
/// - Are floats in range 0.0-200.0 (typical terminal velocity for falling)
/// - Represent the maximum falling speed before air resistance balances gravity
/// - Often constant per character/game physics settings
/// </summary>
public sealed class TerminalVelocityHeuristic : IValueHeuristic
{
    public string Name => "Terminal Velocity Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check value range
        if (IsInTerminalVelocityRange(value.CurrentValue))
        {
            score += 0.4;
        }

        // Analyze observation history
        if (history.Count >= 3)
        {
            var values = history
                .Where(h => h.Value != null)
                .Select(h => HeuristicUtilities.ConvertToDouble(h.Value))
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToList();

            if (values.Count >= 3)
            {
                // Calculate variance - terminal velocity should be mostly static
                var avg = values.Average();
                var variance = values.Average(v => Math.Pow(v - avg, 2));

                // Low variance means mostly static (character property)
                if (variance < 0.1)
                {
                    score += 0.35;
                }

                // Check for rare changes (less than 20% of observations)
                var uniqueValues = values.Select(v => Math.Round(v, 1)).Distinct().Count();
                var changeRatio = (double)uniqueValues / values.Count;
                if (changeRatio < 0.2)
                {
                    score += 0.15;
                }

                // Most values should be non-negative
                var negativeCount = values.Count(v => v < 0);
                if (negativeCount == 0)
                {
                    score += 0.1;
                }
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double";
    }

    private static bool IsInTerminalVelocityRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0.0 && val <= 200.0;
        }
        catch
        {
            return false;
        }
    }
}