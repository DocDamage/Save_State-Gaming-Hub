using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting chunk/grid Y coordinate (vertical chunks) in chunk-based games.
/// Chunk Y values typically:
/// - Are integers representing chunk row/height index
/// - Change when moving between vertical chunk layers
/// - Used in games with vertical chunk subdivision
/// </summary>
public sealed class ChunkYHeuristic : IValueHeuristic
{
    public string Name => "Chunk Y Coordinate Detection";
    public string Category => "Map";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int chunkTransitions = 0;

        // Check value range (chunk Y typically -100 to 100)
        if (IsInChunkYRange(value.CurrentValue))
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

            // Vertical chunk range is smaller
            if (Math.Abs(delta) > 5 && Math.Abs(delta) < 50)
            {
                score += 0.05;
            }

            // Extreme values suspicious for Y chunks
            if (Math.Abs(currVal.Value) > 1000)
            {
                score -= 0.3;
            }
        }

        // Bonus for chunk transition patterns
        if (chunkTransitions >= 1)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "byte";
    }

    private static bool IsInChunkYRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= -1000 && val <= 1000;
        }
        catch
        {
            return false;
        }
    }
}