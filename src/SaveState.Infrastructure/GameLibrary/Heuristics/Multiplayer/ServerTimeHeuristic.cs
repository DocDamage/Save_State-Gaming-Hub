using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting server time/match elapsed time in multiplayer games.
/// Server time values typically:
/// - Are integers (seconds or milliseconds)
/// - Start from 0 at match start
/// - Only increase
/// - Count up or down depending on game mode
/// </summary>
public sealed class ServerTimeHeuristic : IValueHeuristic
{
    public string Name => "Server Time Detection";
    public string Category => "Multiplayer";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool monotonic = true;
        int changeEvents = 0;
        bool reasonableRate = true;

        // Check value range (server time typically 0 to hours in seconds)
        if (IsInServerTimeRange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Must be integer
        if (!HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score -= 0.2;
        }
        else
        {
            score += 0.1;
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

            var delta = currVal.Value - prevVal.Value;

            // Check for monotonic behavior (either always up or always down)
            if (delta != 0)
            {
                changeEvents++;
            }

            // Reasonable rate of change (seconds per observation)
            if (Math.Abs(delta) > 100 && Math.Abs(delta) < 10000)
            {
                reasonableRate = false;
            }

            // Should not jump backwards and forwards
            if (i > 1)
            {
                var prevPrev = history[i - 2];
                if (prevPrev.Value != null)
                {
                    double? prevPrevVal = HeuristicUtilities.ConvertToDouble(prevPrev.Value);
                    if (prevPrevVal.HasValue)
                    {
                        var prevDelta = prevVal.Value - prevPrevVal.Value;
                        if (delta > 0 && prevDelta < 0 || delta < 0 && prevDelta > 0)
                        {
                            monotonic = false;
                        }
                    }
                }
            }

            // Should not be negative (if counting up)
            if (currVal < 0 && delta > 0)
            {
                score -= 0.3;
            }
        }

        // Bonus for change events (time should change)
        if (changeEvents >= 1)
            score += 0.15;

        // Bonus for monotonic behavior
        if (monotonic && history.Count > 2)
            score += 0.2;

        // Bonus for reasonable rate
        if (reasonableRate)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long" or "float" or "single";
    }

    private static bool IsInServerTimeRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            // Server time can be 0 to several hours (in seconds)
            return val >= 0 && val <= 86400;
        }
        catch
        {
            return false;
        }
    }
}