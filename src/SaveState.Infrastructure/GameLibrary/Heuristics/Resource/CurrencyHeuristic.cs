using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting currency/money values in game memory.
/// Currency values typically:
/// - Are integers (currencies are rarely floats)
/// - Only decrease on "SpentMoney" and increase on "EarnedMoney"
/// - Never go negative
/// - Change by reasonable amounts
/// - Often end in 0 or 5 (game design tendency)
/// </summary>
public sealed class CurrencyHeuristic : IValueHeuristic
{
    public string Name => "Currency Detection";
    public string Category => "Currency";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int spentEvents = 0;
        int earnedEvents = 0;
        bool hasGoneNegative = false;

        // Check value range for currency (typically 0 to millions)
        if (IsInCurrencyRange(value.CurrentValue))
        {
            score += 0.2;
        }

        // Prefer integers for currency
        if (HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score += 0.15;
        }

        // Check for "ends in 0 or 5" pattern
        if (EndsInZeroOrFive(value.CurrentValue))
        {
            score += 0.1;
        }

        // Analyze observation history
        for (int i = 1; i < history.Count; i++)
        {
            var curr = history[i];

            if (curr.Value == null)
                continue;

            double? currVal = HeuristicUtilities.ConvertToDouble(curr.Value);
            if (!currVal.HasValue)
                continue;

            // Currency should never be negative
            if (currVal < 0)
            {
                hasGoneNegative = true;
                score -= 0.5;
            }

            // Check action correlations
            if (curr.RelatedAction == PlayerAction.SpentMoney)
            {
                spentEvents++;
                score += 0.1;
            }

            if (curr.RelatedAction == PlayerAction.EarnedMoney)
            {
                earnedEvents++;
                score += 0.1;
            }

            // Check for reasonable change amounts
            if (curr.Delta.HasValue)
            {
                var absDelta = Math.Abs(curr.Delta.Value);
                // Suspicious if change is too large (unless it's a big reward)
                if (absDelta > 1000000 && absDelta < 100000000)
                {
                    score -= 0.1;
                }
            }
        }

        // Bonus for consistent currency behavior
        if (spentEvents >= 1 && earnedEvents >= 1)
            score += 0.2;

        // Heavy penalty for going negative
        if (hasGoneNegative)
        {
            score = Math.Max(0, score - 0.5);
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long" or "int16" or "short";
    }

    private static bool IsInCurrencyRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 999999999; // 0 to ~1 billion
        }
        catch
        {
            return false;
        }
    }

    private static bool EndsInZeroOrFive(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = (long)doubleValue.Value;
            var lastDigit = val % 10;
            return lastDigit == 0 || lastDigit == 5;
        }
        catch
        {
            return false;
        }
    }
}