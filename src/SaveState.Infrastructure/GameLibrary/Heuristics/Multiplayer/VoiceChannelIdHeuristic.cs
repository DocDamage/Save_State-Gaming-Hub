using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting voice channel ID in multiplayer games.
/// Voice channel ID values typically:
/// - Are integers (0, 1, 2, etc.)
/// - Represent team/party/squad channels
/// - Change when switching channels
/// - Stay constant while in channel
/// </summary>
public sealed class VoiceChannelIdHeuristic : IValueHeuristic
{
    public string Name => "Voice Channel ID Detection";
    public string Category => "Multiplayer";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool smallValues = true;
        int changeEvents = 0;

        // Check value range (channel ID typically 0-10)
        if (IsInChannelIdRange(value.CurrentValue))
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
            if (currVal > 100)
            {
                smallValues = false;
            }

            // Check for changes (channel switches)
            if (currVal != prevVal)
            {
                changeEvents++;
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Reasonable max for channel IDs
            if (currVal > 1000)
            {
                score -= 0.4;
            }
        }

        // Bonus for change events (occasional channel switches)
        if (changeEvents >= 1 && changeEvents <= 3)
            score += 0.15;

        // Bonus for small values
        if (smallValues && history.Count > 1)
            score += 0.2;

        // Check for common channel ID values
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue)
        {
            var commonChannels = new[] { 0.0, 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0, 10.0 };
            foreach (var common in commonChannels)
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

    private static bool IsInChannelIdRange(object? value)
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