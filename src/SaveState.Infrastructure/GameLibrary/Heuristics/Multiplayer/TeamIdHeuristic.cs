using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting team ID/number in multiplayer games.
/// Team ID values typically:
/// - Are small integers (0, 1, 2, 3)
/// - 0 or 1 for two-team games
/// - Stay constant during match
/// - Change between matches
/// </summary>
public sealed class TeamIdHeuristic : IValueHeuristic
{
    public string Name => "Team ID Detection";
    public string Category => "Multiplayer";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool isConstant = true;
        bool smallValues = true;

        // Check value range (team ID typically 0-10)
        if (IsInTeamIdRange(value.CurrentValue))
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

            // Check for constancy (team shouldn't change during match)
            if (currVal != prevVal)
            {
                isConstant = false;
            }

            // Check for small values
            if (currVal > 20)
            {
                smallValues = false;
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Reasonable max for team IDs
            if (currVal > 100)
            {
                score -= 0.4;
            }
        }

        // Bonus for being constant during match
        if (isConstant && history.Count > 2)
            score += 0.25;

        // Bonus for small values
        if (smallValues && history.Count > 1)
            score += 0.2;

        // Check for common team ID values
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue)
        {
            var commonTeams = new[] { 0.0, 1.0, 2.0, 3.0 };
            foreach (var common in commonTeams)
            {
                if (Math.Abs(currentVal.Value - common) < 0.5)
                {
                    score += 0.15;
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

    private static bool IsInTeamIdRange(object? value)
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