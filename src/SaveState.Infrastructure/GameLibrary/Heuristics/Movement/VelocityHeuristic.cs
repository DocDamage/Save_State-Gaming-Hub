using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting velocity components (X/Y/Z) in game memory.
/// Velocity values typically:
/// - Are floats in range -500.0 to 500.0
/// - Fluctuate continuously
/// - Can be negative (indicating direction)
/// - Often three consecutive values (VX, VY, VZ)
/// </summary>
public sealed class VelocityHeuristic : IValueHeuristic
{
    public string Name => "Velocity Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int signChanges = 0;
        int fluctuations = 0;
        double prevVal = 0;
        bool hasNegative = false;
        bool hasPositive = false;

        // Check value range
        if (IsInVelocityRange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Analyze observation history
        for (int i = 0; i < history.Count; i++)
        {
            if (history[i].Value == null)
                continue;

            double? currVal = HeuristicUtilities.ConvertToDouble(history[i].Value);
            if (!currVal.HasValue)
                continue;

            var val = currVal.Value;

            // Track positive/negative values
            if (val > 0.01) hasPositive = true;
            if (val < -0.01) hasNegative = true;

            // Track sign changes (velocity changes direction)
            if (i > 0 && prevVal != 0 && val != 0 && Math.Sign(val) != Math.Sign(prevVal))
            {
                signChanges++;
            }

            // Track fluctuations
            if (i > 0 && Math.Abs(val - prevVal) > 0.5)
            {
                fluctuations++;
            }

            prevVal = val;
        }

        // Bonus for having both positive and negative values (indicates direction changes)
        if (hasNegative && hasPositive)
        {
            score += 0.2;
        }

        // Bonus for sign changes (direction changes)
        if (signChanges >= 2)
        {
            score += 0.15;
        }

        // Bonus for fluctuating values
        if (fluctuations >= 3)
        {
            score += 0.15;
        }

        // Correlation with position changes
        int positionEvents = history.Count(h => h.RelatedAction == PlayerAction.PositionChanged);
        if (positionEvents >= 2)
        {
            score += 0.1;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double";
    }

    private static bool IsInVelocityRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= -500.0 && val <= 500.0;
        }
        catch
        {
            return false;
        }
    }
}