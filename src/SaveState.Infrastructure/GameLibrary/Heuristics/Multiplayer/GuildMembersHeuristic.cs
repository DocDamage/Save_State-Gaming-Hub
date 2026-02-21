using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting guild/clan member count in multiplayer games.
/// Guild member values typically:
/// - Are integers (1-1000)
/// - Change when members join/leave
/// - Relatively stable during gameplay
/// - Often capped by game limits
/// </summary>
public sealed class GuildMembersHeuristic : IValueHeuristic
{
    public string Name => "Guild Members Detection";
    public string Category => "Multiplayer";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool moderateValues = true;
        int changeEvents = 0;

        // Check value range (guild members typically 1-1000)
        if (IsInGuildMembersRange(value.CurrentValue))
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

            // Check for reasonable values
            if (currVal > 5000)
            {
                moderateValues = false;
            }

            // Check for changes (members join/leave)
            if (currVal != prevVal)
            {
                changeEvents++;
                var delta = Math.Abs(currVal.Value - prevVal.Value);
                // Usually changes by small amounts
                if (delta <= 5)
                {
                    score += 0.1;
                }
            }

            // Should not be zero or negative
            if (currVal < 1)
            {
                score -= 0.4;
            }

            // Game limits (usually 100-1000)
            if (currVal > 10000)
            {
                score -= 0.4;
            }
        }

        // Bonus for change events
        if (changeEvents >= 1)
            score += 0.1;

        // Bonus for moderate values
        if (moderateValues && history.Count > 1)
            score += 0.15;

        // Check for common guild size values
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue)
        {
            var commonSizes = new[] { 1.0, 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 80.0, 100.0, 150.0, 200.0, 300.0, 500.0, 1000.0 };
            foreach (var common in commonSizes)
            {
                if (Math.Abs(currentVal.Value - common) < 1)
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

    private static bool IsInGuildMembersRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 1 && val <= 10000;
        }
        catch
        {
            return false;
        }
    }
}