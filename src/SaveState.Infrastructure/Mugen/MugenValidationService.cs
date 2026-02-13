using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ValueObjects;
using SaveState.Core.Mugen.Services;
using ValidationResult = SaveState.Core.Mugen.ValueObjects.ValidationResult;
using ValidationError = SaveState.Core.Mugen.ValueObjects.ValidationError;
using ValidationWarning = SaveState.Core.Mugen.ValueObjects.ValidationWarning;

namespace SaveState.Infrastructure.Mugen;

/// <summary>
/// Service for validating MUGEN move definitions.
/// Ensures moves are properly constructed and follow MUGEN rules.
/// </summary>
public class MugenValidationService : IMugenValidationService
{
    private readonly ILogger<MugenValidationService> _logger;

    public MugenValidationService(ILogger<MugenValidationService> logger)
    {
        _logger = logger;
    }

    public Task<Result<ValidationResult>> ValidateMoveAsync(
        MugenMoveDefinition move,
        ValidationOptions options,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Validating move '{MoveName}' with options: FrameData={FrameData}, Hitboxes={Hitboxes}, Balance={Balance}, Commands={Commands}, Strict={Strict}",
                move.Name, options.CheckFrameData, options.CheckHitboxes, options.CheckBalance, options.CheckCommands, options.StrictMode);

            var errors = new List<ValidationError>();
            var warnings = new List<ValidationWarning>();

            // Basic validation
            ValidateBasicProperties(move, errors, warnings);

            if (options.CheckFrameData)
            {
                ValidateFrameData(move, errors, warnings);
            }

            if (options.CheckHitboxes)
            {
                ValidateHitboxes(move, errors, warnings);
            }

            if (options.CheckBalance)
            {
                ValidateBalance(move, errors, warnings, options.StrictMode);
            }

            if (options.CheckCommands)
            {
                ValidateCommands(move, errors, warnings);
            }

            // Custom validation rules
            if (options.CustomRules != null)
            {
                ValidateCustomRules(move, options.CustomRules, errors, warnings);
            }

            var result = new ValidationResult(
                IsValid: !errors.Any(),
                Errors: errors,
                Warnings: warnings,
                Suggestions: GenerateSuggestions(move, errors, warnings));

            _logger.LogInformation("Move '{MoveName}' validation completed: {IsValid}, {ErrorCount} errors, {WarningCount} warnings",
                move.Name, result.IsValid, result.Errors.Count, result.Warnings.Count);

            return Task.FromResult(Result.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating move '{MoveName}'", move.Name);
            return Task.FromResult(Result.Failure<ValidationResult>($"Failed to validate move: {ex.Message}"));
        }
    }

    private void ValidateBasicProperties(MugenMoveDefinition move, List<ValidationError> errors, List<ValidationWarning> warnings)
    {
        // Required fields
        if (string.IsNullOrWhiteSpace(move.Name))
            errors.Add(new ValidationError("MissingName", "Move name is required"));

            if (string.IsNullOrWhiteSpace(move.Command))
                errors.Add(new ValidationError("MissingCommand", "Move command is required"));

            if (move.States.Count == 0)
                errors.Add(new ValidationError("NoStates", "Move must have at least one state"));

        // State validation
        var stateNumbers = new HashSet<int>();
        foreach (var state in move.States)
        {
            if (!stateNumbers.Add(state.StateNumber))
                errors.Add(new ValidationError("DuplicateStateNumber", $"Duplicate state number: {state.StateNumber}"));

            if (state.Duration <= 0)
                errors.Add(new ValidationError("InvalidStateDuration", $"State {state.StateNumber} has invalid duration: {state.Duration}"));

            if (state.Duration > 999)
                warnings.Add(new ValidationWarning("LongStateDuration", $"State {state.StateNumber} has very long duration: {state.Duration}"));
        }

        // Properties validation
        if (move.Properties.Damage < 0)
            errors.Add(new ValidationError("NegativeDamage", "Move damage cannot be negative"));

        if (move.Properties.Damage > 1000)
            warnings.Add(new ValidationWarning("HighDamage", $"Move damage is very high: {move.Properties.Damage}"));

        if (move.Properties.StartupFrames < 0)
            errors.Add(new ValidationError("NegativeStartup", "Startup frames cannot be negative"));

        if (move.Properties.ActiveFrames < 0)
            errors.Add(new ValidationError("NegativeActive", "Active frames cannot be negative"));

        if (move.Properties.RecoveryFrames < 0)
            errors.Add(new ValidationError("NegativeRecovery", "Recovery frames cannot be negative"));

        // Meter validation
        if (move.Properties.MeterCost < 0)
            errors.Add(new ValidationError("NegativeMeterCost", "Meter cost cannot be negative"));

        if (move.Properties.MeterGain < 0)
            warnings.Add(new ValidationWarning("NegativeMeterGain", $"Meter gain is negative: {move.Properties.MeterGain}"));
    }

    private void ValidateFrameData(MugenMoveDefinition move, List<ValidationError> errors, List<ValidationWarning> warnings)
    {
        var totalStateDuration = move.States.Sum(s => s.Duration);
        var expectedTotal = move.Properties.StartupFrames + move.Properties.ActiveFrames + move.Properties.RecoveryFrames;

        // Check if state durations match frame data
        if (Math.Abs(totalStateDuration - expectedTotal) > 2) // Allow small tolerance
        {
            warnings.Add(new ValidationWarning("FrameDataMismatch",
                $"Total state duration ({totalStateDuration}) doesn't match frame data total ({expectedTotal})"));
        }

        // Validate frame advantage consistency
        var hitAdv = move.Properties.FrameAdvantageOnHit;
        var blockAdv = move.Properties.FrameAdvantageOnBlock;

        if (hitAdv < blockAdv)
        {
            warnings.Add(new ValidationWarning("InconsistentAdvantages",
                $"Hit advantage ({hitAdv}) should not be less than block advantage ({blockAdv})"));
        }

        // Check for impossible frame data
        if (move.Properties.StartupFrames == 0 && move.Properties.ActiveFrames > 0)
        {
            errors.Add(new ValidationError("InstantActiveFrames", "Moves cannot have active frames with 0 startup"));
        }

        // Validate hit/block stun
        if (move.Properties.HitStun <= 0 && move.Properties.Damage > 0)
        {
            errors.Add(new ValidationError("MissingHitStun", "Moves with damage must have hit stun"));
        }

        if (move.Properties.BlockStun <= 0 && move.Properties.Damage > 0)
        {
            warnings.Add(new ValidationWarning("MissingBlockStun", "Moves with damage should have block stun"));
        }
    }

    private void ValidateHitboxes(MugenMoveDefinition move, List<ValidationError> errors, List<ValidationWarning> warnings)
    {
        var activeStates = move.States.Where(s => s.HasHitboxes).ToList();

        // Check if move should have hitboxes
        if (move.Properties.Damage > 0 && activeStates.Count == 0 && move.Properties.ActiveFrames > 0)
        {
            errors.Add(new ValidationError("MissingHitboxes", "Damaging moves must have hitboxes during active frames"));
        }

        // Validate hitbox properties
        foreach (var state in move.States)
        {
            foreach (var hitbox in state.Hitboxes)
            {
                // Hitbox bounds validation
                if (hitbox.Bounds.Width <= 0 || hitbox.Bounds.Height <= 0)
                {
                    errors.Add(new ValidationError("InvalidHitboxBounds",
                        $"Hitbox in state {state.StateNumber} has invalid bounds: {hitbox.Bounds.Width}x{hitbox.Bounds.Height}"));
                }

                // Damage validation
                if (hitbox.Damage < 0)
                {
                    errors.Add(new ValidationError("NegativeHitboxDamage",
                        $"Hitbox in state {state.StateNumber} has negative damage: {hitbox.Damage}"));
                }

                // Hit ID validation
                if (hitbox.HitId < 0)
                {
                    errors.Add(new ValidationError("InvalidHitId",
                        $"Hitbox in state {state.StateNumber} has invalid hit ID: {hitbox.HitId}"));
                }

                // Hit flags validation
                if (!hitbox.HitFlags.Any())
                {
                    warnings.Add(new ValidationWarning("MissingHitFlags",
                        $"Hitbox in state {state.StateNumber} has no hit flags"));
                }
            }

            // Hurtbox validation
            foreach (var hurtbox in state.Hurtboxes)
            {
                if (hurtbox.Bounds.Width <= 0 || hurtbox.Bounds.Height <= 0)
                {
                    errors.Add(new ValidationError("InvalidHurtboxBounds",
                        $"Hurtbox in state {state.StateNumber} has invalid bounds: {hurtbox.Bounds.Width}x{hurtbox.Bounds.Height}"));
                }
            }

            // Projectile validation
            foreach (var projectile in state.Projectiles)
            {
                if (projectile.Damage < 0)
                {
                    errors.Add(new ValidationError("NegativeProjectileDamage",
                        $"Projectile in state {state.StateNumber} has negative damage: {projectile.Damage}"));
                }

                if (projectile.Time <= 0)
                {
                    errors.Add(new ValidationError("InvalidProjectileTime",
                        $"Projectile in state {state.StateNumber} has invalid time: {projectile.Time}"));
                }
            }
        }

        // Check for overlapping hitboxes (warning)
        var allHitboxes = move.States.SelectMany(s => s.Hitboxes).ToList();
        for (int i = 0; i < allHitboxes.Count; i++)
        {
            for (int j = i + 1; j < allHitboxes.Count; j++)
            {
                if (HitboxesOverlap(allHitboxes[i], allHitboxes[j]))
                {
                    warnings.Add(new ValidationWarning("OverlappingHitboxes",
                        $"Hitboxes {i} and {j} overlap, which may cause issues"));
                }
            }
        }
    }

    private void ValidateBalance(MugenMoveDefinition move, List<ValidationError> errors, List<ValidationWarning> warnings, bool strictMode)
    {
        // Damage scaling checks
        var damage = move.Properties.Damage;
        var startup = move.Properties.StartupFrames;
        var active = move.Properties.ActiveFrames;

        // Basic damage balance (rough guidelines)
        var expectedMaxDamage = CalculateExpectedMaxDamage(move.MoveType, move.Category, startup, active);

        if (damage > expectedMaxDamage * 1.5)
        {
            var severity = strictMode ? "error" : "warning";
            var message = $"Move damage ({damage}) significantly exceeds expected maximum ({expectedMaxDamage})";

            if (strictMode)
                errors.Add(new ValidationError("ExcessiveDamage", message));
            else
                warnings.Add(new ValidationWarning("HighDamage", message));
        }

        // Frame advantage balance
        if (move.Properties.FrameAdvantageOnHit > 20)
        {
            warnings.Add(new ValidationWarning("HighHitAdvantage",
                $"Hit advantage ({move.Properties.FrameAdvantageOnHit}) is very high"));
        }

        if (move.Properties.FrameAdvantageOnBlock < -30)
        {
            warnings.Add(new ValidationWarning("PunishingOnBlock",
                $"Block disadvantage ({move.Properties.FrameAdvantageOnBlock}) is very punishing"));
        }

        // Meter balance
        if (move.Properties.MeterCost > 2000)
        {
            errors.Add(new ValidationError("ExcessiveMeterCost",
                $"Meter cost ({move.Properties.MeterCost}) is too high (max 2000)"));
        }

        if (move.Properties.MeterGain > 100)
        {
            warnings.Add(new ValidationWarning("HighMeterGain",
                $"Meter gain ({move.Properties.MeterGain}) is very high"));
        }

        // Special move checks
        if (move.MoveType == MoveType.Super && move.Properties.MeterCost < 500)
        {
            warnings.Add(new ValidationWarning("LowSuperCost",
                $"Super move has low meter cost ({move.Properties.MeterCost}), consider increasing"));
        }

        // Projectile balance
        var projectiles = move.States.SelectMany(s => s.Projectiles).ToList();
        if (projectiles.Any(p => p.Hits > 10))
        {
            warnings.Add(new ValidationWarning("MultiHitProjectile",
                "Projectiles with many hits may be overpowered"));
        }

        // Meter validation
        if (move.Properties.MeterCost > 2000)
        {
            errors.Add(new ValidationError("ExcessiveMeterCost",
                $"Meter cost ({move.Properties.MeterCost}) is too high (max 2000)"));
        }
    }

    private void ValidateCommands(MugenMoveDefinition move, List<ValidationError> errors, List<ValidationWarning> warnings)
    {
        var command = move.Command;

        if (string.IsNullOrWhiteSpace(command))
            return;

        // Basic command format validation
        if (!IsValidCommandFormat(command))
        {
            errors.Add(new ValidationError("InvalidCommandFormat",
                $"Command '{command}' has invalid format"));
        }

        // Motion command validation
        if (command.Contains("QCF") || command.Contains("QCB") || command.Contains("DP") || command.Contains("RDP"))
        {
            if (move.MoveType == MoveType.Normal)
            {
                warnings.Add(new ValidationWarning("MotionNormalMove",
                    "Normal moves typically don't use motion commands"));
            }
        }

        // Charge command validation
        if (command.Contains("[") && command.Contains("]"))
        {
            if (!IsValidChargeCommand(command))
            {
                errors.Add(new ValidationError("InvalidChargeCommand",
                    $"Charge command '{command}' is malformed"));
            }
        }

        // Command complexity check
        var complexity = CalculateCommandComplexity(command);
        if (complexity > 20)
        {
            warnings.Add(new ValidationWarning("ComplexCommand",
                $"Command complexity ({complexity}) is very high, may be hard to execute"));
        }
    }

    private void ValidateCustomRules(MugenMoveDefinition move, IReadOnlyList<string> rules, List<ValidationError> errors, List<ValidationWarning> warnings)
    {
        foreach (var rule in rules)
        {
            // Parse and apply custom validation rules
            // This is a simplified implementation - real version would have a rule engine
            if (rule.Contains("max_damage"))
            {
                var parts = rule.Split('=');
                if (parts.Length == 2 && int.TryParse(parts[1], out var maxDamage))
                {
                    if (move.Properties.Damage > maxDamage)
                    {
                        errors.Add(new ValidationError("CustomRuleViolation",
                            $"Move violates custom rule '{rule}': damage {move.Properties.Damage} > {maxDamage}"));
                    }
                }
            }
        }
    }

    private IReadOnlyList<string> GenerateSuggestions(MugenMoveDefinition move, List<ValidationError> errors, List<ValidationWarning> warnings)
    {
        var suggestions = new List<string>();

        // Generate suggestions based on errors and warnings
        if (errors.Any(e => e.Code.Contains("Damage")))
        {
            suggestions.Add("Consider adjusting move damage to better fit its speed and range");
        }

        if (warnings.Any(w => w.Code.Contains("Frame")))
        {
            suggestions.Add("Review frame data to ensure consistent timing");
        }

        if (errors.Any(e => e.Code.Contains("Hitbox")))
        {
            suggestions.Add("Verify hitbox placement and ensure they cover intended areas");
        }

        if (warnings.Any(w => w.Code.Contains("Balance")))
        {
            suggestions.Add("Test move in practice to ensure it feels balanced");
        }

        return suggestions;
    }

    private bool HitboxesOverlap(Hitbox a, Hitbox b)
    {
        return !(a.Bounds.X > b.Bounds.X + b.Bounds.Width ||
                 b.Bounds.X > a.Bounds.X + a.Bounds.Width ||
                 a.Bounds.Y > b.Bounds.Y + b.Bounds.Height ||
                 b.Bounds.Y > a.Bounds.Y + a.Bounds.Height);
    }

    private int CalculateExpectedMaxDamage(MoveType moveType, MoveCategory category, int startup, int active)
    {
        // Simplified damage calculation based on MUGEN balance guidelines
        var baseDamage = moveType switch
        {
            MoveType.Normal => 60,
            MoveType.Special => 100,
            MoveType.Super => 300,
            MoveType.Hyper => 400,
            _ => 50
        };

        // Faster moves can have higher damage
        var speedMultiplier = Math.Max(0.5, 1.0 - (startup / 30.0));

        // Active frames bonus
        var activeBonus = Math.Min(2.0, active / 5.0);

        return (int)(baseDamage * speedMultiplier * activeBonus);
    }

    private bool IsValidCommandFormat(string command)
    {
        // Basic validation - real implementation would be more comprehensive
        if (string.IsNullOrWhiteSpace(command))
            return false;

        // Check for balanced brackets
        var bracketCount = 0;
        foreach (var c in command)
        {
            if (c == '[') bracketCount++;
            if (c == ']') bracketCount--;
            if (bracketCount < 0) return false;
        }

        return bracketCount == 0;
    }

    private bool IsValidChargeCommand(string command)
    {
        // Check for proper charge command format: [direction] [time]
        var chargePattern = @"\[([UDLRFB]+)\]\s*\d+";
        return System.Text.RegularExpressions.Regex.IsMatch(command, chargePattern);
    }

    private int CalculateCommandComplexity(string command)
    {
        // Simple complexity calculation
        var complexity = 0;
        complexity += command.Length;
        complexity += command.Count(c => c == '[' || c == ']') * 2; // Charge commands are more complex
        complexity += command.Count(c => c == '+' || c == ',') * 1; // Button combinations
        return complexity;
    }
    public async Task<Result<ValidationResult>> ValidateCharacterAsync(Guid characterId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating character {CharacterId}", characterId);
        return Result.Success(new ValidationResult(true, new List<ValidationError>(), new List<ValidationWarning>(), new List<string>()));
    }

    public async Task<Result<ValidationResult>> ValidateFrameDataAsync(FrameData frameData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating frame data");
        return Result.Success(new ValidationResult(true, new List<ValidationError>(), new List<ValidationWarning>(), new List<string>()));
    }

    public async Task<Result<ValidationResult>> ValidateHitboxesAsync(IReadOnlyList<Hitbox> hitboxes, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating {Count} hitboxes", hitboxes.Count);
        return Result.Success(new ValidationResult(true, new List<ValidationError>(), new List<ValidationWarning>(), new List<string>()));
    }

    public async Task<Result<bool>> IsMoveBalancedAsync(MugenMoveDefinition move, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking if move '{MoveName}' is balanced", move.Name);
        return Result.Success(true);
    }

    public async Task<Result<IReadOnlyList<string>>> GetSuggestedFixesAsync(ValidationResult validationResult, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting suggested fixes for validation result");
        return Result.Success<IReadOnlyList<string>>(new List<string>());
    }
}
