using System.Diagnostics;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Templates;

/// <summary>
/// Base class for memory pattern templates providing common functionality.
/// </summary>
public abstract class MemoryPatternTemplateBase : IMemoryPatternTemplate
{
    public abstract string Name { get; }
    public abstract string Category { get; }
    public abstract string Description { get; }
    public abstract ValueRange IntRange { get; }
    public abstract ValueRange FloatRange { get; }
    public abstract ValueChangePattern ChangePattern { get; }
    public abstract string DetectionInstruction { get; }

    public virtual Task<Result<List<PotentialMatch>>> ScanForMatchesAsync(
        IGameMemoryReader reader,
        int processId,
        ScanContext context,
        CancellationToken ct = default)
    {
        // Base implementation - subclasses can override for specialized scanning
        return Task.FromResult(Result.Success(new List<PotentialMatch>()));
    }

    public virtual bool ValidateChangePattern(object oldValue, object newValue, string valueType)
    {
        if (oldValue == null || newValue == null) return false;

        try
        {
            var change = CalculateValueChange(oldValue, newValue, valueType);
            return ChangePattern switch
            {
                ValueChangePattern.Static => Math.Abs(change) < 0.001,
                ValueChangePattern.Decreasing => change < 0,
                ValueChangePattern.Increasing => change > 0,
                ValueChangePattern.Fluctuating => true,
                ValueChangePattern.DecreasingThenJump => change != 0,
                ValueChangePattern.Countdown => change < 0,
                _ => true
            };
        }
        catch
        {
            return false;
        }
    }

    protected static double CalculateValueChange(object oldValue, object newValue, string valueType)
    {
        return valueType.ToLowerInvariant() switch
        {
            "int32" or "int" => Convert.ToInt32(newValue) - Convert.ToInt32(oldValue),
            "int64" or "long" => Convert.ToInt64(newValue) - Convert.ToInt64(oldValue),
            "float" => Convert.ToSingle(newValue) - Convert.ToSingle(oldValue),
            "double" => Convert.ToDouble(newValue) - Convert.ToDouble(oldValue),
            _ => 0
        };
    }

    public virtual double CalculateConfidence(PotentialMatch match)
    {
        double confidence = 0.5;

        // Check value range
        var value = match.AsDouble() ?? 0;
        bool inIntRange = IntRange.Contains(value);
        bool inFloatRange = FloatRange.Contains(value);

        if (inIntRange || inFloatRange)
        {
            confidence += 0.2;
        }

        // Check value history for pattern consistency
        if (match.ValueHistory.Count >= 2)
        {
            int patternMatches = 0;
            for (int i = 1; i < match.ValueHistory.Count; i++)
            {
                if (ValidateChangePattern(match.ValueHistory[i - 1], match.ValueHistory[i], match.ValueType))
                {
                    patternMatches++;
                }
            }

            double patternRatio = (double)patternMatches / (match.ValueHistory.Count - 1);
            confidence += patternRatio * 0.2;
        }

        // Bonus for related values nearby
        if (match.NearbyAddresses.Count > 0)
        {
            confidence += 0.1;
        }

        return Math.Min(confidence, 1.0);
    }

    protected static bool IsValueInRange(object value, ValueRange intRange, ValueRange floatRange)
    {
        return value switch
        {
            int i => intRange.Contains(i),
            long l => intRange.Contains((int)l),
            float f => floatRange.Contains(f),
            double d => floatRange.Contains(d),
            _ => false
        };
    }
}

/// <summary>
/// Template for detecting health values in games.
/// Common range: 1-10,000 (int) or 1.0-10,000.0 (float for decimal health).
/// </summary>
public sealed class HealthTemplate : MemoryPatternTemplateBase
{
    public override string Name => "Health";
    public override string Category => "Combat";
    public override string Description => "Player health points or hit points (HP). Decreases when taking damage.";
    public override ValueRange IntRange => new(1, 10000, true);
    public override ValueRange FloatRange => new(1.0, 10000.0, false);
    public override ValueChangePattern ChangePattern => ValueChangePattern.Decreasing;
    public override string DetectionInstruction => "Take damage from an enemy or hazard to help detect your health value.";

    public override Task<Result<List<PotentialMatch>>> ScanForMatchesAsync(
        IGameMemoryReader reader,
        int processId,
        ScanContext context,
        CancellationToken ct = default)
    {
        // Health-specific detection: look for values that decrease when damaged
        // and often have a corresponding max health value nearby
        return base.ScanForMatchesAsync(reader, processId, context, ct);
    }

    public override double CalculateConfidence(PotentialMatch match)
    {
        double confidence = base.CalculateConfidence(match);

        // Health often has a max value nearby (within 4-16 bytes)
        if (match.NearbyAddresses.Count >= 1)
        {
            confidence += 0.15;
        }

        // Health values commonly end in 0 or 5 (100, 50, 25, etc.)
        var intValue = match.AsInt();
        if (intValue.HasValue && (intValue.Value % 5 == 0))
        {
            confidence += 0.05;
        }

        // Common health values (100, 50, 25, 200, 1000, etc.)
        int[] commonHealthValues = { 100, 50, 25, 200, 150, 75, 30, 10, 1000, 500, 250 };
        if (intValue.HasValue && commonHealthValues.Contains(intValue.Value))
        {
            confidence += 0.1;
        }

        return Math.Min(confidence, 1.0);
    }
}

/// <summary>
/// Template for detecting currency/gold/credits in games.
/// Common range: 0-999,999,999 (often large values).
/// </summary>
public sealed class CurrencyTemplate : MemoryPatternTemplateBase
{
    public override string Name => "Currency";
    public override string Category => "Economy";
    public override string Description => "In-game currency, gold, credits, or money. Increases when earned, decreases when spent.";
    public override ValueRange IntRange => new(0, 999_999_999, true);
    public override ValueRange FloatRange => new(0.0, 999_999_999.0, false);
    public override ValueChangePattern ChangePattern => ValueChangePattern.Fluctuating;
    public override string DetectionInstruction => "Earn or spend currency to help detect your money value.";

    public override bool ValidateChangePattern(object oldValue, object newValue, string valueType)
    {
        // Currency can both increase (earn) and decrease (spend)
        var change = CalculateValueChange(oldValue, newValue, valueType);
        return change != 0; // Any change is valid for currency
    }

    public override double CalculateConfidence(PotentialMatch match)
    {
        double confidence = base.CalculateConfidence(match);

        var value = match.AsInt();
        if (value.HasValue)
        {
            // Currency often ends in 0 (10, 100, 1000)
            if (value.Value % 10 == 0)
            {
                confidence += 0.05;
            }

            // Very large values are likely currency (not health/ammo)
            if (value.Value > 10000)
            {
                confidence += 0.1;
            }

            // Values that match common currency patterns
            if (value.Value is >= 0 and <= 2_147_483_647)
            {
                confidence += 0.05;
            }
        }

        return Math.Min(confidence, 1.0);
    }
}

/// <summary>
/// Template for detecting experience points (XP) in games.
/// Common range: 0-999,999,999 (always increasing).
/// </summary>
public sealed class ExperienceTemplate : MemoryPatternTemplateBase
{
    public override string Name => "Experience";
    public override string Category => "Progress";
    public override string Description => "Experience points (XP) that accumulate to level up. Always increases.";
    public override ValueRange IntRange => new(0, 999_999_999, true);
    public override ValueRange FloatRange => new(0.0, 999_999_999.0, false);
    public override ValueChangePattern ChangePattern => ValueChangePattern.Increasing;
    public override string DetectionInstruction => "Defeat enemies or complete objectives to gain experience points.";

    public override double CalculateConfidence(PotentialMatch match)
    {
        double confidence = base.CalculateConfidence(match);

        // XP should have a level value nearby
        if (match.NearbyAddresses.Count >= 1)
        {
            confidence += 0.15;
        }

        var value = match.AsInt();
        if (value.HasValue)
        {
            // XP values often follow progression curves
            // Common XP thresholds: 100, 200, 500, 1000, 2000, 5000
            int[] commonXpValues = { 100, 200, 500, 1000, 2000, 5000, 10000 };
            if (commonXpValues.Any(x => Math.Abs(value.Value - x) < 100))
            {
                confidence += 0.1;
            }
        }

        return Math.Min(confidence, 1.0);
    }
}

/// <summary>
/// Template for detecting ammo count in games.
/// Common range: 0-999 (decreases when firing, jumps on reload).
/// </summary>
public sealed class AmmoTemplate : MemoryPatternTemplateBase
{
    public override string Name => "Ammo";
    public override string Category => "Combat";
    public override string Description => "Current ammunition count for a weapon. Decreases when firing, reloads to max.";
    public override ValueRange IntRange => new(0, 999, true);
    public override ValueRange FloatRange => new(0.0, 999.0, false);
    public override ValueChangePattern ChangePattern => ValueChangePattern.DecreasingThenJump;
    public override string DetectionInstruction => "Fire your weapon to consume ammo, then reload to refill.";

    public override bool ValidateChangePattern(object oldValue, object newValue, string valueType)
    {
        var change = CalculateValueChange(oldValue, newValue, valueType);

        // Ammo can decrease (firing) or jump up (reload)
        // Decrease should be small (-1 for most games)
        if (change < 0 && change >= -10)
        {
            return true;
        }

        // Reload jumps should be significant (+10 or more)
        if (change > 10)
        {
            return true;
        }

        return false;
    }

    public override double CalculateConfidence(PotentialMatch match)
    {
        double confidence = base.CalculateConfidence(match);

        var value = match.AsInt();
        if (value.HasValue)
        {
            // Common clip sizes
            int[] commonClipSizes = { 30, 20, 15, 10, 8, 6, 50, 100, 200, 25, 12 };
            if (commonClipSizes.Contains(value.Value))
            {
                confidence += 0.15;
            }

            // Values typically 0-999 for ammo
            if (value.Value is >= 0 and <= 999)
            {
                confidence += 0.1;
            }
        }

        // Ammo often has reserve ammo nearby
        if (match.NearbyAddresses.Count >= 1)
        {
            confidence += 0.1;
        }

        return Math.Min(confidence, 1.0);
    }
}

/// <summary>
/// Template for detecting stamina/energy/mana in games.
/// Common range: 0.0-100.0 or 0.0-1000.0 (float).
/// </summary>
public sealed class StaminaTemplate : MemoryPatternTemplateBase
{
    public override string Name => "Stamina";
    public override string Category => "Combat";
    public override string Description => "Stamina, energy, or mana that drains when used and regenerates over time.";
    public override ValueRange IntRange => new(0, 1000, true);
    public override ValueRange FloatRange => new(0.0, 1000.0, false);
    public override ValueChangePattern ChangePattern => ValueChangePattern.Fluctuating;
    public override string DetectionInstruction => "Sprint, dodge, or use abilities to drain stamina, then wait to regenerate.";

    public override double CalculateConfidence(PotentialMatch match)
    {
        double confidence = base.CalculateConfidence(match);

        // Stamina is commonly a float between 0-100
        var floatValue = match.AsFloat();
        if (floatValue.HasValue)
        {
            if (floatValue.Value is >= 0 and <= 100)
            {
                confidence += 0.15;
            }

            // Decimal values suggest stamina/energy (not currency/health)
            if (floatValue.Value % 1 != 0)
            {
                confidence += 0.1;
            }
        }

        // Stamina often has a max value nearby (same as current or very close)
        if (match.NearbyAddresses.Count >= 1)
        {
            confidence += 0.1;
        }

        return Math.Min(confidence, 1.0);
    }
}

/// <summary>
/// Template for detecting score in games.
/// Common range: 0-9,999,999,999 (always increasing).
/// </summary>
public sealed class ScoreTemplate : MemoryPatternTemplateBase
{
    public override string Name => "Score";
    public override string Category => "Progress";
    public override string Description => "Game score or points accumulated during gameplay. Always increases.";
    public override ValueRange IntRange => new(0, 9_999_999_999, true);
    public override ValueRange FloatRange => new(0.0, 9_999_999_999.0, false);
    public override ValueChangePattern ChangePattern => ValueChangePattern.Increasing;
    public override string DetectionInstruction => "Collect items or defeat enemies to increase your score.";

    public override double CalculateConfidence(PotentialMatch match)
    {
        double confidence = base.CalculateConfidence(match);

        var value = match.AsInt();
        if (value.HasValue)
        {
            // Score typically increases by specific amounts
            // Common score increments: 100, 200, 500, 1000, 50, 25, 10
            if (value.Value % 10 == 0 || value.Value % 5 == 0)
            {
                confidence += 0.1;
            }

            // Very high values likely indicate score
            if (value.Value > 100000)
            {
                confidence += 0.1;
            }

            // Score is typically non-zero during gameplay
            if (value.Value > 0)
            {
                confidence += 0.05;
            }
        }

        return Math.Min(confidence, 1.0);
    }
}

/// <summary>
/// Template for detecting timers in games.
/// Common range: 0.0-3600.0 seconds (float).
/// </summary>
public sealed class TimerTemplate : MemoryPatternTemplateBase
{
    public override string Name => "Timer";
    public override string Category => "Gameplay";
    public override string Description => "Countdown timer (decreasing) or speedrun timer (increasing).";
    public override ValueRange IntRange => new(0, 3600, true);
    public override ValueRange FloatRange => new(0.0, 3600.0, false);
    public override ValueChangePattern ChangePattern => ValueChangePattern.Countdown;
    public override string DetectionInstruction => "Wait for the timer to count down (or up for speedrun timers).";

    public TimerMode Mode { get; set; } = TimerMode.Countdown;

    public override bool ValidateChangePattern(object oldValue, object newValue, string valueType)
    {
        var change = CalculateValueChange(oldValue, newValue, valueType);

        return Mode switch
        {
            TimerMode.Countdown => change < 0 && change > -5, // Decreasing by small amounts
            TimerMode.Speedrun => change > 0 && change < 5,   // Increasing by small amounts
            _ => Math.Abs(change) < 5
        };
    }

    public override double CalculateConfidence(PotentialMatch match)
    {
        double confidence = base.CalculateConfidence(match);

        var floatValue = match.AsFloat();
        if (floatValue.HasValue)
        {
            // Timers typically have decimal precision (seconds with milliseconds)
            if (floatValue.Value % 1 != 0)
            {
                confidence += 0.15;
            }

            // Common timer ranges
            if (floatValue.Value is >= 0 and <= 3600)
            {
                confidence += 0.1;
            }
        }

        // Timers usually don't have related values nearby
        if (match.NearbyAddresses.Count == 0)
        {
            confidence += 0.05;
        }

        return Math.Min(confidence, 1.0);
    }
}

/// <summary>
/// Timer mode: countdown or speedrun.
/// </summary>
public enum TimerMode
{
    Countdown,
    Speedrun
}

/// <summary>
/// Template for detecting player position coordinates.
/// Common range: -99999.0 to 99999.0 (float).
/// </summary>
public sealed class PositionTemplate : MemoryPatternTemplateBase
{
    public override string Name => "Position";
    public override string Category => "Movement";
    public override string Description => "Player position coordinates (X, Y, Z). Changes with movement.";
    public override ValueRange IntRange => new(-99999, 99999, true);
    public override ValueRange FloatRange => new(-99999.0, 99999.0, false);
    public override ValueChangePattern ChangePattern => ValueChangePattern.Fluctuating;
    public override string DetectionInstruction => "Move your character in different directions to detect position coordinates.";

    public override double CalculateConfidence(PotentialMatch match)
    {
        double confidence = base.CalculateConfidence(match);

        var floatValue = match.AsFloat();
        if (floatValue.HasValue)
        {
            // Position is almost always a float
            if (match.ValueType == "float" || match.ValueType == "double")
            {
                confidence += 0.2;
            }

            // Can be negative (coordinates)
            if (Math.Abs(floatValue.Value) > 0.001)
            {
                confidence += 0.1;
            }

            // Position often changes by small amounts
            if (match.ValueHistory.Count >= 2)
            {
                var changes = new List<double>();
                for (int i = 1; i < match.ValueHistory.Count; i++)
                {
                    if (match.ValueHistory[i] is float f2 && match.ValueHistory[i - 1] is float f1)
                    {
                        changes.Add(Math.Abs(f2 - f1));
                    }
                }

                // Position changes should be relatively smooth
                if (changes.Count > 0 && changes.Average() < 100)
                {
                    confidence += 0.15;
                }
            }
        }

        // Position values often appear in groups of 2 (X,Y) or 3 (X,Y,Z)
        if (match.NearbyAddresses.Count >= 2)
        {
            confidence += 0.2;
        }

        return Math.Min(confidence, 1.0);
    }
}

/// <summary>
/// Template for detecting lives/continues in games.
/// Common range: 0-99 (small integers).
/// </summary>
public sealed class LivesTemplate : MemoryPatternTemplateBase
{
    public override string Name => "Lives";
    public override string Category => "Progress";
    public override string Description => "Number of lives or continues remaining. Decreases on death.";
    public override ValueRange IntRange => new(0, 99, true);
    public override ValueRange FloatRange => new(0.0, 99.0, false);
    public override ValueChangePattern ChangePattern => ValueChangePattern.Decreasing;
    public override string DetectionInstruction => "Lose a life to help detect the lives counter.";

    public override double CalculateConfidence(PotentialMatch match)
    {
        double confidence = base.CalculateConfidence(match);

        var value = match.AsInt();
        if (value.HasValue)
        {
            // Lives are typically small values
            if (value.Value is >= 0 and <= 10)
            {
                confidence += 0.2;
            }

            // Common starting lives
            int[] commonLives = { 3, 5, 1, 9, 99 };
            if (commonLives.Contains(value.Value))
            {
                confidence += 0.15;
            }
        }

        return Math.Min(confidence, 1.0);
    }
}

/// <summary>
/// Template for detecting level/rank in games.
/// Common range: 1-999 (always increasing or static).
/// </summary>
public sealed class LevelTemplate : MemoryPatternTemplateBase
{
    public override string Name => "Level";
    public override string Category => "Progress";
    public override string Description => "Character level, rank, or stage number. Increases when leveling up.";
    public override ValueRange IntRange => new(1, 999, true);
    public override ValueRange FloatRange => new(1.0, 999.0, false);
    public override ValueChangePattern ChangePattern => ValueChangePattern.Static;
    public override string DetectionInstruction => "Level up your character or advance to the next stage.";

    public override double CalculateConfidence(PotentialMatch match)
    {
        double confidence = base.CalculateConfidence(match);

        var value = match.AsInt();
        if (value.HasValue)
        {
            // Levels typically start at 1
            if (value.Value >= 1)
            {
                confidence += 0.1;
            }

            // Common level ranges
            if (value.Value is >= 1 and <= 100)
            {
                confidence += 0.15;
            }

            // Levels often have XP nearby
            if (match.NearbyAddresses.Count >= 1)
            {
                confidence += 0.15;
            }
        }

        return Math.Min(confidence, 1.0);
    }
}

/// <summary>
/// Template for detecting shield/armor values in games.
/// Common range: 0-1000 (often lower than health).
/// </summary>
public sealed class ShieldTemplate : MemoryPatternTemplateBase
{
    public override string Name => "Shield";
    public override string Category => "Combat";
    public override string Description => "Shield, armor, or damage absorption value. Absorbs damage before health.";
    public override ValueRange IntRange => new(0, 1000, true);
    public override ValueRange FloatRange => new(0.0, 1000.0, false);
    public override ValueChangePattern ChangePattern => ValueChangePattern.Decreasing;
    public override string DetectionInstruction => "Take damage while shields are active to detect shield value.";

    public override double CalculateConfidence(PotentialMatch match)
    {
        double confidence = base.CalculateConfidence(match);

        var value = match.AsInt();
        if (value.HasValue)
        {
            // Shields are often smaller than health
            if (value.Value is >= 0 and <= 200)
            {
                confidence += 0.1;
            }

            // Common shield values
            int[] commonShields = { 100, 50, 25, 75, 150, 200 };
            if (commonShields.Contains(value.Value))
            {
                confidence += 0.1;
            }
        }

        // Shield often has health nearby
        if (match.NearbyAddresses.Count >= 1)
        {
            confidence += 0.15;
        }

        return Math.Min(confidence, 1.0);
    }
}

/// <summary>
/// Provides access to all universal pattern templates.
/// </summary>
public static class UniversalPatternTemplates
{
    /// <summary>
    /// Gets all available pattern templates.
    /// </summary>
    public static IReadOnlyList<IMemoryPatternTemplate> All => new List<IMemoryPatternTemplate>
    {
        new HealthTemplate(),
        new CurrencyTemplate(),
        new ExperienceTemplate(),
        new AmmoTemplate(),
        new StaminaTemplate(),
        new ScoreTemplate(),
        new TimerTemplate(),
        new PositionTemplate(),
        new LivesTemplate(),
        new LevelTemplate(),
        new ShieldTemplate()
    };

    /// <summary>
    /// Gets templates by category.
    /// </summary>
    public static IReadOnlyList<IMemoryPatternTemplate> GetByCategory(string category)
    {
        return All.Where(t => t.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>
    /// Gets a template by name.
    /// </summary>
    public static IMemoryPatternTemplate? GetByName(string name)
    {
        return All.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets all template categories.
    /// </summary>
    public static IReadOnlyList<string> GetCategories()
    {
        return All.Select(t => t.Category).Distinct().ToList();
    }

    /// <summary>
    /// Converts a template match to a game memory signature.
    /// </summary>
    public static GameMemorySignature ToSignature(
        IMemoryPatternTemplate template,
        PotentialMatch match,
        string gameTitle = "*")
    {
        return new GameMemorySignature
        {
            GameTitle = gameTitle,
            Name = template.Name,
            Description = template.Description,
            Pattern = $"{match.Address:X}",
            Offset = 0,
            ValueType = match.ValueType,
            MinValue = (long)template.IntRange.Min,
            MaxValue = (long)template.IntRange.Max,
            MinFloatValue = template.FloatRange.Min,
            MaxFloatValue = template.FloatRange.Max,
            ModuleName = match.ModuleName,
            Tags = new List<string> { template.Category, "universal", "auto-detected" },
            CreatedAt = DateTime.UtcNow
        };
    }
}
