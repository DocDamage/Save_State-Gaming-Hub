using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting reserved slots in multiplayer servers.
/// Reserved slots values typically:
/// - Are small integers (0-32)
/// - Stay constant or change rarely
/// - Used for admins, VIPs, or specific roles
/// - Less than or equal to max players
/// </summary>
public sealed class ReservedSlotsHeuristic : IValueHeuristic
{
    public string Name => "Reserved Slots Detection";
    public string Category => "Multiplayer";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool smallValues = true;
        bool relativelyConstant = true;
        int changeCount = 0;

        // Check value range (reserved slots typically 0-32)
        if (IsInReservedSlotsRange(value.CurrentValue))
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

            // Check for small values
            if (currVal > 50)
            {
                smallValues = false;
            }

            // Track changes (should be rare)
            if (currVal != prevVal)
            {
                changeCount++;
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Reasonable max for reserved slots
            if (currVal > 100)
            {
                score -= 0.4;
            }
        }

        // Bonus for small values
        if (smallValues && history.Count > 1)
            score += 0.15;

        // Check constancy
        if (changeCount == 0 && history.Count > 2)
        {
            relativelyConstant = true;
            score += 0.2;
        }
        else if (changeCount <= 2)
        {
            score += 0.1;
        }

        // Check for common reserved slot values
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue)
        {
            var commonValues = new[] { 0.0, 1.0, 2.0, 4.0, 6.0, 8.0, 10.0, 12.0, 16.0, 20.0, 24.0, 32.0 };
            foreach (var common in commonValues)
            {
                if (Math.Abs(currentVal.Value - common) < 0.5)
                {
                    score += 0.1;
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

    private static bool IsInReservedSlotsRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}