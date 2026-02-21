using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting social/companionship values in survival games.
/// Social values typically:
/// - Are floats or integers (0.0-100.0)
/// - Decrease when isolated for long periods
/// - Increase when interacting with NPCs or companions
/// - Affects sanity, motivation, and mental health
/// </summary>
public sealed class SocialHeuristic : IValueHeuristic
{
    public string Name => "Social Needs Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int interactionEvents = 0;
        int isolationDecay = 0;
        bool gradualDecayPattern = false;

        // Check value range (social typically 0-100)
        if (IsInSocialRange(value.CurrentValue))
        {
            score += 0.3;
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

            // Check for social boost from interactions
            if (currVal > prevVal && (curr.RelatedAction == PlayerAction.UsedItem || 
                                       curr.RelatedAction == PlayerAction.Healed))
            {
                var delta = currVal.Value - prevVal.Value;
                // Social interactions provide significant boosts
                if (delta > 5 && delta < 40)
                {
                    interactionEvents++;
                    score += 0.15;
                }
            }

            // Check for gradual decay during isolation (idle)
            if (currVal < prevVal && curr.RelatedAction == PlayerAction.Idle)
            {
                var delta = prevVal.Value - currVal.Value;
                // Loneliness creeps in slowly
                if (delta > 0 && delta < 2)
                {
                    isolationDecay++;
                    gradualDecayPattern = true;
                    score += 0.08;
                }
            }

            // Social should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Social typically caps at 100
            if (currVal > 100)
            {
                score -= 0.3;
            }

            // Check for typical social ranges (people are rarely at extremes)
            if (currVal >= 20 && currVal <= 90)
            {
                score += 0.05;
            }
        }

        // Bonus for interaction events
        if (interactionEvents >= 1)
            score += 0.15;

        // Strong bonus for gradual decay pattern (distinctive)
        if (gradualDecayPattern && isolationDecay >= 3)
            score += 0.2;

        // Check for max value near 100
        var maxValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Max();

        if (Math.Abs(maxValue - 100) < 5)
        {
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "int32" or "int" or "double" or "int16" or "short";
    }

    private static bool IsInSocialRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Social typically in range 0-100
            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}