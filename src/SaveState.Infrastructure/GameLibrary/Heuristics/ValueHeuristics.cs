using System.Globalization;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting health values in game memory.
/// Health values typically:
/// - Are integers 1-10000 or floats 1.0-1000.0
/// - Decrease when "TookDamage" action reported
/// - Increase when "Healed" action reported
/// - Often have a nearby "max health" value
/// - Rarely go above a certain threshold
/// </summary>
public sealed class HealthHeuristic : IValueHeuristic
{
    public string Name => "Health Detection";
    public string Category => "Health";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int healthIndicators = 0;
        int damageEvents = 0;
        int healEvents = 0;

        // Check value range
        if (IsInHealthRange(value.CurrentValue))
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

            double? prevVal = ConvertToDouble(prev.Value);
            double? currVal = ConvertToDouble(curr.Value);

            if (!prevVal.HasValue || !currVal.HasValue)
                continue;

            // Check for damage patterns (decrease after TookDamage)
            if (curr.RelatedAction == PlayerAction.TookDamage && currVal < prevVal)
            {
                damageEvents++;
                healthIndicators++;

                // Health typically decreases by reasonable amounts
                var delta = prevVal.Value - currVal.Value;
                if (delta > 0 && delta < 1000)
                {
                    score += 0.1;
                }
            }

            // Check for healing patterns (increase after Healed)
            if (curr.RelatedAction == PlayerAction.Healed && currVal > prevVal)
            {
                healEvents++;
                healthIndicators++;
            }

            // Health values rarely go negative
            if (currVal < 0)
            {
                score -= 0.3;
            }

            // Health values should stay within reasonable bounds
            if (currVal > 100000)
            {
                score -= 0.2;
            }
        }

        // Bonus for multiple consistent health indicators
        if (damageEvents >= 2)
            score += 0.2;
        if (healEvents >= 1)
            score += 0.15;

        // Bonus for consistent range behavior
        if (healthIndicators >= 3)
            score += 0.15;

        // Check for max health proximity pattern (if we have enough observations)
        if (history.Count > 5 && HasMaxValuePattern(history))
        {
            score += 0.1;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "float" or "single" or "int16" or "short" or "int64" or "long";
    }

    private static bool IsInHealthRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            // Health typically in ranges: 1-100, 1-1000, 1-10000
            var val = doubleValue.Value;
            return (val >= 1 && val <= 10000) || (val >= 1.0 && val <= 1000.0);
        }
        catch
        {
            return false;
        }
    }

    private static double? ConvertToDouble(object value)
    {
        try
        {
            return Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }

    private static bool HasMaxValuePattern(List<ValueObservation> history)
    {
        // Look for values that frequently hit the same max value
        var values = history
            .Where(o => o.Value != null)
            .Select(o => ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToList();

        if (values.Count < 3) return false;

        var maxValue = values.Max();
        var timesAtMax = values.Count(v => Math.Abs(v - maxValue) < 0.01);

        // If value hits max frequently, might indicate health with max value
        return timesAtMax >= 2;
    }
}

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
        if (IsIntegerValue(value.CurrentValue))
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

            double? currVal = ConvertToDouble(curr.Value);
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
            var doubleValue = ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 999999999; // 0 to ~1 billion
        }
        catch
        {
            return false;
        }
    }

    private static bool IsIntegerValue(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = ConvertToDouble(value);
            if (!doubleValue.HasValue) return true; // Assume integer if can't parse

            return Math.Abs(doubleValue.Value % 1) < 0.0001;
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
            var doubleValue = ConvertToDouble(value);
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

    private static double? ConvertToDouble(object value)
    {
        try
        {
            return Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Heuristic for detecting position coordinates in game memory.
/// Position values typically:
/// - Are floats (X, Y, Z coordinates)
/// - Change smoothly (not jumping instantly)
/// - Are consecutive in memory (X at addr, Y at addr+4, Z at addr+8)
/// - Change on "PositionChanged" action
/// - Values typically in range -10000 to +10000
/// </summary>
public sealed class PositionHeuristic : IValueHeuristic
{
    public string Name => "Position Detection";
    public string Category => "Position";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int smoothChanges = 0;
        int positionChanges = 0;
        double totalDelta = 0;

        // Check value range for position
        if (IsInPositionRange(value.CurrentValue))
        {
            score += 0.25;
        }

        // Analyze observation history
        for (int i = 1; i < history.Count; i++)
        {
            var prev = history[i - 1];
            var curr = history[i];

            if (prev.Value == null || curr.Value == null)
                continue;

            double? prevVal = ConvertToDouble(prev.Value);
            double? currVal = ConvertToDouble(curr.Value);

            if (!prevVal.HasValue || !currVal.HasValue)
                continue;

            var delta = Math.Abs(currVal.Value - prevVal.Value);
            totalDelta += delta;

            // Position changes should be smooth, not instant jumps
            if (delta > 0 && delta < 1000)
            {
                smoothChanges++;
            }

            // Large instant jumps are suspicious
            if (delta > 5000)
            {
                score -= 0.1;
            }

            // Check for position changed action correlation
            if (curr.RelatedAction == PlayerAction.PositionChanged)
            {
                positionChanges++;
            }
        }

        // Bonus for smooth movement patterns
        if (history.Count > 1)
        {
            var smoothRatio = (double)smoothChanges / (history.Count - 1);
            score += smoothRatio * 0.2;
        }

        // Bonus for position action correlation
        if (positionChanges >= 2)
        {
            score += 0.2;
        }

        // Check for consistent change pattern (positions change gradually)
        if (history.Count > 2 && totalDelta > 0)
        {
            var avgDelta = totalDelta / (history.Count - 1);
            if (avgDelta > 0.1 && avgDelta < 100)
            {
                score += 0.15;
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double";
    }

    private static bool IsInPositionRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= -100000 && val <= 100000; // Wide range for various game scales
        }
        catch
        {
            return false;
        }
    }

    private static double? ConvertToDouble(object value)
    {
        try
        {
            return Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Heuristic for detecting ammo/magazine values in game memory.
/// Ammo values typically:
/// - Are small integers (0-999)
/// - Decrease by 1 on "UsedAmmo"
/// - Jump up on "Reloaded"
/// - Often paired with "max ammo" value
/// - Reset to max on weapon switch or respawn
/// </summary>
public sealed class AmmoHeuristic : IValueHeuristic
{
    public string Name => "Ammo Detection";
    public string Category => "Ammo";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int usedAmmoEvents = 0;
        int reloadEvents = 0;
        int decrementByOneCount = 0;

        // Check value range for ammo
        if (IsInAmmoRange(value.CurrentValue))
        {
            score += 0.3;
        }

        // Ammo is typically an integer
        if (IsIntegerValue(value.CurrentValue))
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

            double? prevVal = ConvertToDouble(prev.Value);
            double? currVal = ConvertToDouble(curr.Value);

            if (!prevVal.HasValue || !currVal.HasValue)
                continue;

            var delta = currVal.Value - prevVal.Value;

            // Check for decrease by 1 (typical ammo usage)
            if (Math.Abs(delta - (-1)) < 0.001)
            {
                decrementByOneCount++;
            }

            // Check for UsedAmmo action correlation
            if (curr.RelatedAction == PlayerAction.UsedAmmo)
            {
                usedAmmoEvents++;
                if (delta < 0)
                {
                    score += 0.15;
                }
            }

            // Check for Reloaded action correlation
            if (curr.RelatedAction == PlayerAction.Reloaded)
            {
                reloadEvents++;
                if (delta > 0)
                {
                    score += 0.15;
                }
            }

            // Ammo should never be negative
            if (currVal < 0)
            {
                score -= 0.3;
            }

            // Large jumps might indicate reloads or weapon switches
            if (delta > 10)
            {
                // Could be a reload - check if it's a common max ammo value
                if (IsCommonMaxAmmo(currVal.Value))
                {
                    score += 0.1;
                }
            }
        }

        // Strong bonus for consistent -1 decrements (shooting)
        if (decrementByOneCount >= 2)
        {
            score += 0.25;
        }

        // Bonus for having both used and reload events
        if (usedAmmoEvents >= 2 && reloadEvents >= 1)
        {
            score += 0.2;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "byte";
    }

    private static bool IsInAmmoRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 999;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsIntegerValue(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = ConvertToDouble(value);
            if (!doubleValue.HasValue) return true;

            return Math.Abs(doubleValue.Value % 1) < 0.0001;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsCommonMaxAmmo(double value)
    {
        // Common max ammo values in games
        var commonMaxValues = new[] { 30, 32, 60, 100, 200, 255, 999 };
        return commonMaxValues.Any(v => Math.Abs(value - v) < 0.001);
    }

    private static double? ConvertToDouble(object value)
    {
        try
        {
            return Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Heuristic for detecting experience points in game memory.
/// XP values typically:
/// - Are integers or floats
/// - Only increase (until level up, then may reset)
/// - Increase on "GainedXp" action
/// - Change by various amounts (not always 1)
/// - May have a "next level" threshold nearby
/// </summary>
public sealed class ExperienceHeuristic : IValueHeuristic
{
    public string Name => "Experience Detection";
    public string Category => "Experience";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int xpGainEvents = 0;
        int levelUpEvents = 0;
        int increases = 0;
        int decreases = 0;

        // Check value range for XP
        if (IsInXpRange(value.CurrentValue))
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

            double? prevVal = ConvertToDouble(prev.Value);
            double? currVal = ConvertToDouble(curr.Value);

            if (!prevVal.HasValue || !currVal.HasValue)
                continue;

            var delta = currVal.Value - prevVal.Value;

            // Track increases vs decreases
            if (delta > 0)
            {
                increases++;
            }
            else if (delta < 0)
            {
                decreases++;
            }

            // Check for GainedXp action correlation
            if (curr.RelatedAction == PlayerAction.GainedXp)
            {
                xpGainEvents++;
                if (delta > 0)
                {
                    score += 0.15;
                }
            }

            // Check for LeveledUp action correlation (might reset XP)
            if (curr.RelatedAction == PlayerAction.LeveledUp)
            {
                levelUpEvents++;
                if (delta < 0)
                {
                    // XP reset after level up is common
                    score += 0.1;
                }
            }

            // Check for reasonable XP gain amounts
            if (delta > 0 && delta < 100000)
            {
                score += 0.05;
            }
        }

        // XP should mostly increase
        if (history.Count > 1)
        {
            var increaseRatio = (double)increases / (history.Count - 1);
            if (increaseRatio > 0.7)
            {
                score += 0.2;
            }

            // Penalty for too many decreases (unlike health/ammo)
            if (decreases > increases)
            {
                score -= 0.3;
            }
        }

        // Bonus for XP gain events
        if (xpGainEvents >= 2)
        {
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long" or "float" or "single";
    }

    private static bool IsInXpRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 999999999; // XP can get very high
        }
        catch
        {
            return false;
        }
    }

    private static double? ConvertToDouble(object value)
    {
        try
        {
            return Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Heuristic for detecting score values in game memory.
/// Score values typically:
/// - Are integers
/// - Only increase (rarely decrease)
/// - Increase on "ScoreIncreased" action
/// - Often have specific patterns (multiples of 10, 100, etc.)
/// - Can get very large
/// </summary>
public sealed class ScoreHeuristic : IValueHeuristic
{
    public string Name => "Score Detection";
    public string Category => "Score";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int scoreEvents = 0;
        int increases = 0;
        int decreases = 0;

        // Check value range for score
        if (IsInScoreRange(value.CurrentValue))
        {
            score += 0.2;
        }

        // Scores are typically integers
        if (IsIntegerValue(value.CurrentValue))
        {
            score += 0.15;
        }

        // Check for common score patterns (multiples of 10, 100)
        if (HasScorePattern(value.CurrentValue))
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

            double? prevVal = ConvertToDouble(prev.Value);
            double? currVal = ConvertToDouble(curr.Value);

            if (!prevVal.HasValue || !currVal.HasValue)
                continue;

            var delta = currVal.Value - prevVal.Value;

            // Track increases vs decreases
            if (delta > 0)
            {
                increases++;
            }
            else if (delta < 0)
            {
                decreases++;
            }

            // Check for ScoreIncreased action correlation
            if (curr.RelatedAction == PlayerAction.ScoreIncreased)
            {
                scoreEvents++;
                if (delta > 0)
                {
                    score += 0.15;
                }
            }

            // Scores rarely decrease (unless penalty system)
            if (delta < 0)
            {
                score -= 0.1;
            }

            // Check for reasonable score gain amounts
            if (delta > 0 && delta < 10000)
            {
                score += 0.05;
            }
        }

        // Score should mostly increase
        if (history.Count > 1 && increases > 0)
        {
            var increaseRatio = (double)increases / (history.Count - 1);
            if (increaseRatio > 0.8)
            {
                score += 0.2;
            }
        }

        // Bonus for score events
        if (scoreEvents >= 2)
        {
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long";
    }

    private static bool IsInScoreRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 999999999999; // Scores can be very high
        }
        catch
        {
            return false;
        }
    }

    private static bool IsIntegerValue(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = ConvertToDouble(value);
            if (!doubleValue.HasValue) return true;

            return Math.Abs(doubleValue.Value % 1) < 0.0001;
        }
        catch
        {
            return false;
        }
    }

    private static bool HasScorePattern(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = (long)doubleValue.Value;
            // Scores often end in 0 (multiples of 10, 100, etc.)
            return val % 10 == 0;
        }
        catch
        {
            return false;
        }
    }

    private static double? ConvertToDouble(object value)
    {
        try
        {
            return Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Heuristic for detecting timer/countdown values in game memory.
/// Timer values typically:
/// - Are floats or integers
/// - Decrease steadily over time (for countdowns)
/// - Or increase steadily (for elapsed time)
/// - Change at consistent intervals
/// </summary>
public sealed class TimerHeuristic : IValueHeuristic
{
    public string Name => "Timer Detection";
    public string Category => "Timer";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check value range for timers (typically seconds)
        if (IsInTimerRange(value.CurrentValue))
        {
            score += 0.25;
        }

        // Check for steady change pattern
        if (history.Count >= 3)
        {
            var deltas = new List<double>();

            for (int i = 1; i < history.Count; i++)
            {
                if (history[i].Value == null || history[i - 1].Value == null)
                    continue;

                double? curr = ConvertToDouble(history[i].Value);
                double? prev = ConvertToDouble(history[i - 1].Value);

                if (!curr.HasValue || !prev.HasValue)
                    continue;

                var delta = curr.Value - prev.Value;
                var timeDelta = (history[i].Timestamp - history[i - 1].Timestamp).TotalMilliseconds;

                if (timeDelta > 0)
                {
                    deltas.Add(delta / timeDelta);
                }
            }

            if (deltas.Count >= 2)
            {
                // Check for consistent rate of change (timer characteristic)
                var avgDelta = deltas.Average();
                var variance = deltas.Average(d => Math.Pow(d - avgDelta, 2));
                var stdDev = Math.Sqrt(variance);

                // Low standard deviation means consistent change (timer-like)
                if (stdDev < 0.001)
                {
                    score += 0.35;
                }

                // Timers either consistently increase or decrease
                if (Math.Abs(avgDelta) > 0)
                {
                    score += 0.2;
                }
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "int32" or "int" or "double";
    }

    private static bool IsInTimerRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            // Timers typically in range of game sessions (0 to a few hours in seconds)
            return val >= 0 && val <= 86400; // 0 to 24 hours in seconds
        }
        catch
        {
            return false;
        }
    }

    private static double? ConvertToDouble(object value)
    {
        try
        {
            return Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }
}
