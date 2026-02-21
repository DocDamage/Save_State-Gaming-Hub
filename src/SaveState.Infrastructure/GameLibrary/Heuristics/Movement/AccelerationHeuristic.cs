using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting acceleration in racing/physics games.
/// Acceleration values typically:
/// - Are floats (positive and negative)
/// - Change rapidly during speed changes
/// - Include both positive and negative values (deceleration)
/// </summary>
public sealed class AccelerationHeuristic : IValueHeuristic
{
    public string Name => "Acceleration Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasPositive = false;
        bool hasNegative = false;
        bool rapidChanges = false;

        // Check for float type
        if (IsFloatType(value.ValueType))
        {
            score += 0.2;
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

            // Check for positive acceleration
            if (currVal > 0)
                hasPositive = true;

            // Check for negative acceleration (braking)
            if (currVal < 0)
                hasNegative = true;

            // Check for rapid changes
            if (Math.Abs(currVal.Value - prevVal.Value) > 1.0)
            {
                rapidChanges = true;
            }

            // Reasonable range for acceleration
            if (Math.Abs(currVal.Value) > 100)
            {
                score -= 0.2;
            }
        }

        // Bonus for having both positive and negative
        if (hasPositive && hasNegative)
            score += 0.25;

        // Bonus for rapid changes
        if (rapidChanges)
            score += 0.2;

        // Bonus for zero being common (constant speed)
        var zeroCount = history.Count(o => 
        {
            var val = HeuristicUtilities.ConvertToDouble(o.Value);
            return val.HasValue && Math.Abs(val.Value) < 0.01;
        });
        if (zeroCount > 0)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsFloatType(string valueType)
    {
        var normalized = valueType.ToLowerInvariant();
        return normalized is "float" or "single" or "double";
    }
}