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

#region Movement & Physics Heuristics

/// <summary>
/// Heuristic for detecting movement speed values in game memory.
/// Speed values typically:
/// - Are floats in range 0.0-1000.0
/// - Fluctuate when moving, 0 when stationary
/// - Often near position coordinates
/// </summary>
public sealed class SpeedHeuristic : IValueHeuristic
{
    public string Name => "Speed Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int zeroWhenStationary = 0;
        int fluctuatingCount = 0;
        double prevVal = 0;
        bool hasBeenNonZero = false;

        // Check value range
        if (IsInSpeedRange(value.CurrentValue))
        {
            score += 0.3;
        }

        // Analyze observation history
        for (int i = 0; i < history.Count; i++)
        {
            if (history[i].Value == null)
                continue;

            double? currVal = ConvertToDouble(history[i].Value);
            if (!currVal.HasValue)
                continue;

            var val = currVal.Value;

            // Track if value has been non-zero
            if (val > 0.01)
                hasBeenNonZero = true;

            // Speed is 0 when stationary (after PositionChanged action ends)
            if (i > 0 && history[i].RelatedAction == null && val < 0.01)
            {
                zeroWhenStationary++;
            }

            // Speed fluctuates when moving
            if (i > 0 && Math.Abs(val - prevVal) > 0.1 && val > 0.01)
            {
                fluctuatingCount++;
            }

            prevVal = val;

            // Speed should never be negative
            if (val < 0)
            {
                score -= 0.3;
            }
        }

        // Bonus for speed that goes to zero when stationary
        if (zeroWhenStationary >= 2)
        {
            score += 0.2;
        }

        // Bonus for fluctuating values when moving
        if (fluctuatingCount >= 3 && hasBeenNonZero)
        {
            score += 0.25;
        }

        // Bonus for correlation with position changes
        int positionChangeEvents = history.Count(h => h.RelatedAction == PlayerAction.PositionChanged);
        if (positionChangeEvents >= 2 && hasBeenNonZero)
        {
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double";
    }

    private static bool IsInSpeedRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0.0 && val <= 1000.0;
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
/// Heuristic for detecting velocity components (X/Y/Z) in game memory.
/// Velocity values typically:
/// - Are floats in range -500.0 to 500.0
/// - Fluctuate continuously
/// - Can be negative (indicating direction)
/// - Often three consecutive values (VX, VY, VZ)
/// </summary>
public sealed class VelocityHeuristic : IValueHeuristic
{
    public string Name => "Velocity Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int signChanges = 0;
        int fluctuations = 0;
        double prevVal = 0;
        bool hasNegative = false;
        bool hasPositive = false;

        // Check value range
        if (IsInVelocityRange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Analyze observation history
        for (int i = 0; i < history.Count; i++)
        {
            if (history[i].Value == null)
                continue;

            double? currVal = ConvertToDouble(history[i].Value);
            if (!currVal.HasValue)
                continue;

            var val = currVal.Value;

            // Track positive/negative values
            if (val > 0.01) hasPositive = true;
            if (val < -0.01) hasNegative = true;

            // Track sign changes (velocity changes direction)
            if (i > 0 && prevVal != 0 && val != 0 && Math.Sign(val) != Math.Sign(prevVal))
            {
                signChanges++;
            }

            // Track fluctuations
            if (i > 0 && Math.Abs(val - prevVal) > 0.5)
            {
                fluctuations++;
            }

            prevVal = val;
        }

        // Bonus for having both positive and negative values (indicates direction changes)
        if (hasNegative && hasPositive)
        {
            score += 0.2;
        }

        // Bonus for sign changes (direction changes)
        if (signChanges >= 2)
        {
            score += 0.15;
        }

        // Bonus for fluctuating values
        if (fluctuations >= 3)
        {
            score += 0.15;
        }

        // Correlation with position changes
        int positionEvents = history.Count(h => h.RelatedAction == PlayerAction.PositionChanged);
        if (positionEvents >= 2)
        {
            score += 0.1;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double";
    }

    private static bool IsInVelocityRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= -500.0 && val <= 500.0;
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
/// Heuristic for detecting jump height/capability values in game memory.
/// Jump height values typically:
/// - Are floats in range 0.0-1000.0
/// - Have a static base value
/// - Show temporary changes during jump
/// </summary>
public sealed class JumpHeightHeuristic : IValueHeuristic
{
    public string Name => "Jump Height Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        double? baseValue = null;
        int returnsToBase = 0;
        int temporaryChanges = 0;

        // Check value range
        if (IsInJumpRange(value.CurrentValue))
        {
            score += 0.3;
        }

        // Analyze observation history
        if (history.Count >= 3)
        {
            var values = history
                .Where(h => h.Value != null)
                .Select(h => ConvertToDouble(h.Value))
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToList();

            if (values.Count >= 3)
            {
                // Find most common value (base value)
                baseValue = values.GroupBy(v => Math.Round(v, 2))
                    .OrderByDescending(g => g.Count())
                    .First()
                    .Key;

                // Check for values returning to base after changes
                for (int i = 1; i < values.Count; i++)
                {
                    if (Math.Abs(values[i] - baseValue.Value) < 0.01 &&
                        Math.Abs(values[i - 1] - baseValue.Value) > 0.01)
                    {
                        returnsToBase++;
                    }

                    if (Math.Abs(values[i] - values[i - 1]) > 10 &&
                        Math.Abs(values[i] - baseValue.Value) > 0.01)
                    {
                        temporaryChanges++;
                    }
                }
            }
        }

        // Bonus for returning to base value (static base with temporary changes)
        if (returnsToBase >= 2)
        {
            score += 0.25;
        }

        // Bonus for temporary changes during jumps
        if (temporaryChanges >= 1)
        {
            score += 0.2;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double";
    }

    private static bool IsInJumpRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0.0 && val <= 1000.0;
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
/// Heuristic for detecting gravity multiplier values in game memory.
/// Gravity values typically:
/// - Are floats in range 0.0-5.0 (1.0 = normal gravity)
/// - Mostly static
/// - Rarely change (usually during special effects)
/// </summary>
public sealed class GravityHeuristic : IValueHeuristic
{
    public string Name => "Gravity Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check value range
        if (IsInGravityRange(value.CurrentValue))
        {
            score += 0.4;
        }

        // Check if value is near 1.0 (normal gravity)
        if (IsNearNormalGravity(value.CurrentValue))
        {
            score += 0.2;
        }

        // Analyze observation history
        if (history.Count >= 3)
        {
            var values = history
                .Where(h => h.Value != null)
                .Select(h => ConvertToDouble(h.Value))
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToList();

            if (values.Count >= 3)
            {
                // Calculate variance - gravity should be mostly static
                var avg = values.Average();
                var variance = values.Average(v => Math.Pow(v - avg, 2));

                // Low variance means mostly static
                if (variance < 0.01)
                {
                    score += 0.3;
                }

                // Check for rare changes (less than 10% of observations)
                var uniqueValues = values.Select(v => Math.Round(v, 2)).Distinct().Count();
                var changeRatio = (double)uniqueValues / values.Count;
                if (changeRatio < 0.1)
                {
                    score += 0.1;
                }
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double";
    }

    private static bool IsInGravityRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0.0 && val <= 5.0;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsNearNormalGravity(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return Math.Abs(val - 1.0) < 0.1;
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

#endregion

#region Combat Mechanics Heuristics

/// <summary>
/// Heuristic for detecting ability/skill cooldown values in game memory.
/// Cooldown values typically:
/// - Are floats in range 0.0-300.0 seconds
/// - Count down from max to 0
/// - Jump back up when ability is used
/// </summary>
public sealed class CooldownHeuristic : IValueHeuristic
{
    public string Name => "Cooldown Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int countdownEvents = 0;
        int resetEvents = 0;
        double? maxValue = null;

        // Check value range
        if (IsInCooldownRange(value.CurrentValue))
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

            var delta = currVal.Value - prevVal.Value;

            // Check for countdown pattern (decreasing)
            if (delta < 0 && delta > -5 && currVal.Value >= 0)
            {
                countdownEvents++;
                // Track max value
                if (!maxValue.HasValue || prevVal.Value > maxValue.Value)
                    maxValue = prevVal.Value;
            }

            // Check for reset pattern (jump back up)
            if (delta > 1)
            {
                resetEvents++;
            }

            // Cooldown should never be negative
            if (currVal.Value < 0)
            {
                score -= 0.3;
            }
        }

        // Bonus for countdown pattern
        if (countdownEvents >= 3)
        {
            score += 0.3;
        }

        // Bonus for reset events
        if (resetEvents >= 1)
        {
            score += 0.2;
        }

        // Bonus for both countdown and reset
        if (countdownEvents >= 2 && resetEvents >= 1)
        {
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double";
    }

    private static bool IsInCooldownRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0.0 && val <= 300.0;
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
/// Heuristic for detecting attack damage values in game memory.
/// Damage values typically:
/// - Are integers in range 1-99999
/// - Static, change only on equipment/level up
/// - Often have min/max damage pair
/// </summary>
public sealed class DamageHeuristic : IValueHeuristic
{
    public string Name => "Damage Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int changes = 0;

        // Check value range
        if (IsInDamageRange(value.CurrentValue))
        {
            score += 0.3;
        }

        // Damage is typically an integer
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

            // Track changes
            if (Math.Abs(delta) > 0.001)
            {
                changes++;
            }

            // Large jumps might indicate equipment changes
            if (Math.Abs(delta) > 100)
            {
                score += 0.05;
            }
        }

        // Damage should be relatively static (rare changes)
        if (history.Count > 1)
        {
            var changeRatio = (double)changes / (history.Count - 1);
            if (changeRatio < 0.1)
            {
                score += 0.25;
            }
        }

        // Bonus for damage correlation with level up
        int levelUpEvents = history.Count(h => h.RelatedAction == PlayerAction.LeveledUp);
        if (levelUpEvents >= 1 && changes >= 1)
        {
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long" or "int16" or "short";
    }

    private static bool IsInDamageRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 1 && val <= 99999;
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
/// Heuristic for detecting critical hit chance values in game memory.
/// Critical chance values typically:
/// - Are floats in range 0.0-100.0 (percentage)
/// - Static or slowly increasing
/// - Change with gear/levels
/// </summary>
public sealed class CriticalChanceHeuristic : IValueHeuristic
{
    public string Name => "Critical Chance Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int increases = 0;
        int decreases = 0;

        // Check value range
        if (IsInCriticalRange(value.CurrentValue))
        {
            score += 0.35;
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

            if (delta > 0)
                increases++;
            else if (delta < 0)
                decreases++;

            // Small changes indicate gradual improvement
            if (Math.Abs(delta) > 0.001 && Math.Abs(delta) < 5)
            {
                score += 0.05;
            }
        }

        // Critical chance should be relatively static
        if (history.Count > 1)
        {
            var changeRatio = (double)(increases + decreases) / (history.Count - 1);
            if (changeRatio < 0.2)
            {
                score += 0.25;
            }
        }

        // Usually increases with gear/levels
        if (increases >= decreases)
        {
            score += 0.1;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double";
    }

    private static bool IsInCriticalRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0.0 && val <= 100.0;
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
/// Heuristic for detecting armor/defense rating values in game memory.
/// Armor values typically:
/// - Are integers in range 0-9999
/// - Static
/// - Change on equipment
/// </summary>
public sealed class ArmorRatingHeuristic : IValueHeuristic
{
    public string Name => "Armor Rating Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int changes = 0;

        // Check value range
        if (IsInArmorRange(value.CurrentValue))
        {
            score += 0.35;
        }

        // Armor is typically an integer
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

            if (Math.Abs(delta) > 0.001)
            {
                changes++;
            }
        }

        // Armor should be relatively static
        if (history.Count > 1)
        {
            var changeRatio = (double)changes / (history.Count - 1);
            if (changeRatio < 0.05)
            {
                score += 0.3;
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long" or "int16" or "short";
    }

    private static bool IsInArmorRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 9999;
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

#endregion

#region RPG Progression Heuristics

/// <summary>
/// Heuristic for detecting unspent skill points in game memory.
/// Skill points typically:
/// - Are integers in range 0-999
/// - Increase on level up
/// - Decrease when spent
/// </summary>
public sealed class SkillPointsHeuristic : IValueHeuristic
{
    public string Name => "Skill Points Detection";
    public string Category => "RPG";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int increases = 0;
        int decreases = 0;

        // Check value range
        if (IsInSkillPointsRange(value.CurrentValue))
        {
            score += 0.3;
        }

        // Skill points are always integers
        if (IsIntegerValue(value.CurrentValue))
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

            if (delta > 0)
            {
                increases++;
                // Increases typically on level up
                if (curr.RelatedAction == PlayerAction.LeveledUp)
                {
                    score += 0.2;
                }
            }
            else if (delta < 0)
            {
                decreases++;
                // Decreases when spent
                score += 0.1;
            }

            // Skill points should never be negative
            if (currVal.Value < 0)
            {
                score -= 0.5;
            }
        }

        // Should have both increases and decreases
        if (increases >= 1 && decreases >= 1)
        {
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long" or "int16" or "short";
    }

    private static bool IsInSkillPointsRange(object? value)
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
/// Heuristic for detecting faction reputation values in game memory.
/// Reputation values typically:
/// - Are integers in range -10000 to 10000
/// - Slowly change based on actions
/// - Can be negative (hostile) or positive (friendly)
/// </summary>
public sealed class ReputationHeuristic : IValueHeuristic
{
    public string Name => "Reputation Detection";
    public string Category => "RPG";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasNegative = false;
        bool hasPositive = false;
        int smallChanges = 0;

        // Check value range
        if (IsInReputationRange(value.CurrentValue))
        {
            score += 0.3;
        }

        // Reputation is typically an integer
        if (IsIntegerValue(value.CurrentValue))
        {
            score += 0.15;
        }

        // Analyze observation history
        for (int i = 0; i < history.Count; i++)
        {
            if (history[i].Value == null)
                continue;

            double? currVal = ConvertToDouble(history[i].Value);
            if (!currVal.HasValue)
                continue;

            if (currVal.Value < 0) hasNegative = true;
            if (currVal.Value > 0) hasPositive = true;

            // Track small changes (slow reputation changes)
            if (i > 0 && history[i - 1].Value != null)
            {
                double? prevVal = ConvertToDouble(history[i - 1].Value);
                if (prevVal.HasValue)
                {
                    var delta = Math.Abs(currVal.Value - prevVal.Value);
                    if (delta > 0 && delta <= 100)
                    {
                        smallChanges++;
                    }
                }
            }
        }

        // Bonus for having both negative and positive values
        if (hasNegative && hasPositive)
        {
            score += 0.2;
        }

        // Bonus for small, gradual changes
        if (smallChanges >= 2)
        {
            score += 0.2;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long" or "int16" or "short";
    }

    private static bool IsInReputationRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= -10000 && val <= 10000;
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
/// Heuristic for detecting inventory carry weight values in game memory.
/// Carry weight typically:
/// - Are floats in range 0.0-10000.0
/// - Current weight fluctuates
/// - Max weight is static
/// </summary>
public sealed class CarryWeightHeuristic : IValueHeuristic
{
    public string Name => "Carry Weight Detection";
    public string Category => "RPG";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int increases = 0;
        int decreases = 0;

        // Check value range
        if (IsInCarryWeightRange(value.CurrentValue))
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

            double? prevVal = ConvertToDouble(prev.Value);
            double? currVal = ConvertToDouble(curr.Value);

            if (!prevVal.HasValue || !currVal.HasValue)
                continue;

            var delta = currVal.Value - prevVal.Value;

            if (delta > 0)
            {
                increases++;
            }
            else if (delta < 0)
            {
                decreases++;
            }

            // Weight should never be negative
            if (currVal.Value < 0)
            {
                score -= 0.3;
            }
        }

        // Both increases (pick up items) and decreases (drop items) should occur
        if (increases >= 1 && decreases >= 1)
        {
            score += 0.25;
        }

        // Weight fluctuates with inventory changes
        if (increases + decreases >= 2)
        {
            score += 0.2;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double";
    }

    private static bool IsInCarryWeightRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0.0 && val <= 10000.0;
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

#endregion

#region Resource Management Heuristics

/// <summary>
/// Heuristic for detecting mana/magic/energy resource values in game memory.
/// Mana values typically:
/// - Are floats or integers in range 0-1000
/// - Fluctuate (use spell -> decreases, regen -> increases)
/// - Similar pattern to health but for magic
/// </summary>
public sealed class ManaHeuristic : IValueHeuristic
{
    public string Name => "Mana Detection";
    public string Category => "Resource";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int decreases = 0;
        int increases = 0;
        int spellUseEvents = 0;

        // Check value range
        if (IsInManaRange(value.CurrentValue))
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

            var delta = currVal.Value - prevVal.Value;

            // Track decreases (spell usage)
            if (delta < 0)
            {
                decreases++;
                spellUseEvents++;
            }

            // Track increases (regeneration)
            if (delta > 0)
            {
                increases++;
            }

            // Mana should never be negative
            if (currVal.Value < 0)
            {
                score -= 0.3;
            }
        }

        // Mana should have both decreases (usage) and increases (regen)
        if (decreases >= 1 && increases >= 1)
        {
            score += 0.25;
        }

        // Fluctuating pattern is key for mana
        if (decreases + increases >= 3)
        {
            score += 0.2;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "float" or "single" or "int16" or "short";
    }

    private static bool IsInManaRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 10000;
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
/// Heuristic for detecting item durability values in game memory.
/// Durability values typically:
/// - Are integers in range 0-100 or 0-1000
/// - Slowly decrease with use
/// - Jump up on repair
/// </summary>
public sealed class DurabilityHeuristic : IValueHeuristic
{
    public string Name => "Durability Detection";
    public string Category => "Resource";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int decreases = 0;
        int repairJumps = 0;

        // Check value range
        if (IsInDurabilityRange(value.CurrentValue))
        {
            score += 0.3;
        }

        // Durability is always an integer
        if (IsIntegerValue(value.CurrentValue))
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

            // Small decreases indicate wear and tear
            if (delta < 0 && delta >= -5)
            {
                decreases++;
            }

            // Large jumps indicate repair
            if (delta > 50)
            {
                repairJumps++;
            }

            // Durability should never be negative
            if (currVal.Value < 0)
            {
                score -= 0.3;
            }
        }

        // Slow decrease is characteristic of durability
        if (decreases >= 2)
        {
            score += 0.25;
        }

        // Repair jumps
        if (repairJumps >= 1)
        {
            score += 0.15;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "byte";
    }

    private static bool IsInDurabilityRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 1000;
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
/// Heuristic for detecting resource counts (building materials, crafting resources) in game memory.
/// Resource counts typically:
/// - Are integers in range 0-9999
/// - Increase when gathered
/// - Decrease when used
/// </summary>
public sealed class ResourceCountHeuristic : IValueHeuristic
{
    public string Name => "Resource Count Detection";
    public string Category => "Resource";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int increases = 0;
        int decreases = 0;

        // Check value range
        if (IsInResourceRange(value.CurrentValue))
        {
            score += 0.3;
        }

        // Resources are always integers
        if (IsIntegerValue(value.CurrentValue))
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

            if (delta > 0)
            {
                increases++;
            }
            else if (delta < 0)
            {
                decreases++;
            }

            // Resources should never be negative
            if (currVal.Value < 0)
            {
                score -= 0.5;
            }
        }

        // Both gathering (increases) and usage (decreases)
        if (increases >= 1 && decreases >= 1)
        {
            score += 0.25;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long" or "int16" or "short";
    }

    private static bool IsInResourceRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 9999;
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

#endregion

#region Game State Heuristics

/// <summary>
/// Heuristic for detecting game difficulty level values in game memory.
/// Difficulty values typically:
/// - Are integers in range 0-4 (Easy, Normal, Hard, Nightmare, etc.)
/// - Static
/// - Rarely change
/// </summary>
public sealed class DifficultyHeuristic : IValueHeuristic
{
    public string Name => "Difficulty Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int changes = 0;

        // Check value range
        if (IsInDifficultyRange(value.CurrentValue))
        {
            score += 0.4;
        }

        // Difficulty is always an integer
        if (IsIntegerValue(value.CurrentValue))
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

            if (Math.Abs(currVal.Value - prevVal.Value) > 0.001)
            {
                changes++;
            }
        }

        // Difficulty should be very static (rarely changes)
        if (history.Count > 1)
        {
            var changeRatio = (double)changes / (history.Count - 1);
            if (changeRatio < 0.05)
            {
                score += 0.3;
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "byte";
    }

    private static bool IsInDifficultyRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 10; // 0-4 typical, allow some flexibility
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
/// Heuristic for detecting in-game time/day counter values in game memory.
/// Game time values typically:
/// - Are floats in range 0.0-86400.0 (seconds in a day)
/// - Constantly increasing
/// - Reset at midnight (0)
/// </summary>
public sealed class GameTimeHeuristic : IValueHeuristic
{
    public string Name => "Game Time Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int increases = 0;
        int resets = 0;
        double totalDelta = 0;

        // Check value range
        if (IsInGameTimeRange(value.CurrentValue))
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

            double? prevVal = ConvertToDouble(prev.Value);
            double? currVal = ConvertToDouble(curr.Value);

            if (!prevVal.HasValue || !currVal.HasValue)
                continue;

            var delta = currVal.Value - prevVal.Value;
            totalDelta += delta;

            // Track increases
            if (delta > 0 && delta < 3600) // Less than an hour jump
            {
                increases++;
            }

            // Track resets (value went from high to low - midnight reset)
            if (delta < -80000) // Reset from near 86400 to near 0
            {
                resets++;
            }

            // Negative values are invalid for time
            if (currVal.Value < 0)
            {
                score -= 0.5;
            }
        }

        // Game time should mostly increase
        if (history.Count > 1)
        {
            var increaseRatio = (double)increases / (history.Count - 1);
            if (increaseRatio > 0.8)
            {
                score += 0.3;
            }
        }

        // Midnight resets are characteristic
        if (resets >= 1)
        {
            score += 0.25;
        }

        // Steady increase rate
        if (history.Count > 1 && totalDelta > 0)
        {
            var avgDelta = totalDelta / (history.Count - 1);
            if (avgDelta > 0 && avgDelta < 60) // Steady increase, not jumps
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

    private static bool IsInGameTimeRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0.0 && val <= 86400.0; // Seconds in a day
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
/// Heuristic for detecting game completion percentage values in game memory.
/// Completion values typically:
/// - Are floats in range 0.0-100.0
/// - Only increasing
/// - Change on achievements/progress
/// </summary>
public sealed class CompletionHeuristic : IValueHeuristic
{
    public string Name => "Completion Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int increases = 0;
        int decreases = 0;

        // Check value range
        if (IsInCompletionRange(value.CurrentValue))
        {
            score += 0.35;
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

            if (delta > 0)
            {
                increases++;
            }
            else if (delta < 0)
            {
                decreases++;
            }

            // Completion should never exceed 100%
            if (currVal.Value > 100)
            {
                score -= 0.3;
            }

            // Completion should never be negative
            if (currVal.Value < 0)
            {
                score -= 0.3;
            }
        }

        // Completion should only increase
        if (history.Count > 1)
        {
            if (decreases == 0 && increases > 0)
            {
                score += 0.35;
            }

            var increaseRatio = (double)increases / (history.Count - 1);
            if (increaseRatio > 0.9)
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

    private static bool IsInCompletionRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0.0 && val <= 100.0;
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

#endregion

#region Extension Methods

/// <summary>
/// Extension methods for working with value heuristics.
/// </summary>
public static class HeuristicExtensions
{
    /// <summary>
    /// Gets all available heuristics.
    /// </summary>
    public static IEnumerable<IValueHeuristic> GetAllHeuristics()
    {
        return new List<IValueHeuristic>
        {
            // Original 7 heuristics
            new HealthHeuristic(),
            new CurrencyHeuristic(),
            new PositionHeuristic(),
            new AmmoHeuristic(),
            new ExperienceHeuristic(),
            new ScoreHeuristic(),
            new TimerHeuristic(),

            // Movement & Physics (4)
            new SpeedHeuristic(),
            new VelocityHeuristic(),
            new JumpHeightHeuristic(),
            new GravityHeuristic(),

            // Combat Mechanics (4)
            new CooldownHeuristic(),
            new DamageHeuristic(),
            new CriticalChanceHeuristic(),
            new ArmorRatingHeuristic(),

            // RPG Progression (3)
            new SkillPointsHeuristic(),
            new ReputationHeuristic(),
            new CarryWeightHeuristic(),

            // Resource Management (3)
            new ManaHeuristic(),
            new DurabilityHeuristic(),
            new ResourceCountHeuristic(),

            // Game State (3)
            new DifficultyHeuristic(),
            new GameTimeHeuristic(),
            new CompletionHeuristic()
        };
    }

    /// <summary>
    /// Gets heuristics by category.
    /// </summary>
    public static IEnumerable<IValueHeuristic> GetHeuristicsByCategory(this IEnumerable<IValueHeuristic> heuristics, string category)
    {
        return heuristics.Where(h => h.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets all available heuristic categories.
    /// </summary>
    public static IEnumerable<string> GetAllCategories(this IEnumerable<IValueHeuristic> heuristics)
    {
        return heuristics.Select(h => h.Category).Distinct().OrderBy(c => c);
    }
}

#endregion
