using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting disease severity values in survival games.
/// Disease values typically:
/// - Are floats or integers (0.0-100.0 or 0-5 stages)
/// - Represent severity or progression of illness
/// - Increase with exposure and decrease with treatment/time
/// - Affect multiple stats (health, stamina, nutrition)
/// </summary>
public sealed class DiseaseHeuristic : IValueHeuristic
{
    public string Name => "Disease Detection";
    public string Category => "Survival";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int symptomEvents = 0;
        int remissionEvents = 0;
        bool stageProgressionPattern = false;

        // Check value range (disease typically 0-100 or 0-5)
        if (IsInDiseaseRange(value.CurrentValue))
        {
            score += 0.3;
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

            // Check for disease progression
            if (currVal > prevVal)
            {
                var delta = currVal.Value - prevVal.Value;
                // Disease stages or gradual worsening
                if (delta > 0 && delta <= 25)
                {
                    symptomEvents++;
                    score += 0.1;
                }
                // Check for stage progression (exact increments)
                if (HeuristicUtilities.IsIntegerValue(currVal.Value) && 
                    HeuristicUtilities.IsIntegerValue(prevVal.Value))
                {
                    stageProgressionPattern = true;
                }
            }

            // Check for remission/recovery
            if (currVal < prevVal)
            {
                var delta = prevVal.Value - currVal.Value;
                // Recovery can be sudden (treatment) or gradual
                if (delta > 0)
                {
                    remissionEvents++;
                    if (delta > 10)
                    {
                        score += 0.15;
                    }
                }
            }

            // Disease should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Check for common disease caps
            if (currVal > 100 && currVal != 5)
            {
                score -= 0.2;
            }
        }

        // Bonus for symptom events
        if (symptomEvents >= 2)
            score += 0.12;

        // Bonus for stage progression pattern (distinctive)
        if (stageProgressionPattern)
            score += 0.2;

        // Bonus for remission events
        if (remissionEvents >= 1)
            score += 0.1;

        // Check for common max values (100 or 5 stages)
        var maxValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Max();

        if (Math.Abs(maxValue - 100) < 5 || Math.Abs(maxValue - 5) < 0.5)
        {
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "int32" or "int" or "double" or "int16" or "short";
    }

    private static bool IsInDiseaseRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Disease typically in range 0-100 or 0-5 (stages)
            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}