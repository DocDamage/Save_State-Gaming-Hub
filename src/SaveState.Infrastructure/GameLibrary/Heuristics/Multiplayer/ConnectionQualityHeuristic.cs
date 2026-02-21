using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting connection quality index in multiplayer games.
/// Connection quality values typically:
/// - Are integers (0-5, 0-100, or quality levels)
/// - Represent connection health (Excellent/Good/Fair/Poor)
/// - Change based on ping and packet loss
/// - Stay stable during good connection
/// </summary>
public sealed class ConnectionQualityHeuristic : IValueHeuristic
{
    public string Name => "Connection Quality Detection";
    public string Category => "Multiplayer";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool validRange = true;
        int changeEvents = 0;

        // Check value range (quality typically 0-5 or 0-100)
        if (IsInQualityRange(value.CurrentValue))
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

            // Check for valid quality ranges
            if (currVal > 100 || (currVal > 5 && currVal < 10))
            {
                validRange = false;
            }

            // Track changes
            if (currVal != prevVal)
            {
                changeEvents++;
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }
        }

        // Bonus for valid ranges
        if (validRange && history.Count > 1)
            score += 0.2;

        // Bonus for occasional changes (quality fluctuates with network)
        if (changeEvents >= 1 && changeEvents <= history.Count / 3)
            score += 0.15;

        // Check for common quality values
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue)
        {
            var commonValues = new[] { 0.0, 1.0, 2.0, 3.0, 4.0, 5.0, 25.0, 50.0, 75.0, 100.0 };
            foreach (var common in commonValues)
            {
                if (Math.Abs(currentVal.Value - common) < 0.1)
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

    private static bool IsInQualityRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            // Common ranges: 0-5 stars/bars or 0-100 percentage
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}