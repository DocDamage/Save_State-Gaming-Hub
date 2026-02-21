using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting chunk/grid X coordinate in chunk-based games.
/// Chunk X values typically:
/// - Are integers representing chunk column index
/// - Change in steps when crossing chunk boundaries
/// - Used in Minecraft-style and open world games
/// </summary>
public sealed class ChunkXHeuristic : IValueHeuristic
{
    public string Name => "Chunk X Coordinate Detection";
    public string Category => "Map";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int chunkTransitions = 0;

        // Check value range (chunks typically -10000 to 10000)
        if (IsInChunkRange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Must be integer
        if (!HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score -= 0.3;
        }
        else
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

            // Check for chunk transitions (+1 or -1)
            var delta = currVal.Value - prevVal.Value;
            if (Math.Abs(delta) == 1)
            {
                chunkTransitions++;
                score += 0.15;
            }

            // Multiple chunk jumps at once (teleport/fast travel)
            if (Math.Abs(delta) > 10 && Math.Abs(delta) < 1000)
            {
                score += 0.05;
            }

            // Extreme jumps are suspicious
            if (Math.Abs(delta) > 10000)
            {
                score -= 0.3;
            }

            // Should not be extremely negative or positive
            if (Math.Abs(currVal.Value) > 1000000)
            {
                score -= 0.4;
            }
        }

        // Bonus for chunk transition patterns
        if (chunkTransitions >= 2)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "byte";
    }

    private static bool IsInChunkRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= -100000 && val <= 100000;
        }
        catch
        {
            return false;
        }
    }
}