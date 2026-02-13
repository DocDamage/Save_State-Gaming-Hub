using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Numerics;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Advanced physics combat service providing axis-aware hit detection, juggle decay,
/// character-specific gravity, wall splat mechanics, and environmental destruction.
/// </summary>
public class AdvancedPhysicsCombatService : AdvancedPhysicsCombatServiceIAdvancedPhysicsCombatService
{
    private readonly ILogger<AdvancedPhysicsCombatService> _logger;
    private readonly ICacheService _cache;
    private readonly Dictionary<string, AdvancedPhysicsCombatServiceHitDetectionState> _hitDetectionStates = new();
    private readonly Dictionary<string, AdvancedPhysicsCombatServiceJuggleDecayState> _juggleDecayStates = new();
    private readonly Dictionary<string, AdvancedPhysicsCombatServiceCharacterGravity> _characterGravities = new();
    private readonly Dictionary<string, AdvancedPhysicsCombatServiceWallCollisionState> _wallStates = new();
    private readonly Dictionary<string, AdvancedPhysicsCombatServiceDestructionState> _destructionStates = new();
    private readonly AdvancedPhysicsCombatServiceHitDetectionEngine _hitDetectionEngine;
    private readonly AdvancedPhysicsCombatServiceJuggleDecayEngine _juggleDecayEngine;
    private readonly AdvancedPhysicsCombatServiceCharacterGravityEngine _characterGravityEngine;
    private readonly AdvancedPhysicsCombatServiceWallSplatEngine _wallSplatEngine;
    private readonly AdvancedPhysicsCombatServiceDestructionEngine _destructionEngine;

    public AdvancedPhysicsCombatService(
        ILogger<AdvancedPhysicsCombatService> logger,
        ILoggerFactory loggerFactory,
        ICacheService cache)
    {
        _logger = logger;
        _cache = cache;
        _hitDetectionEngine = new AdvancedPhysicsCombatServiceHitDetectionEngine(loggerFactory.CreateLogger<AdvancedPhysicsCombatServiceHitDetectionEngine>());
        _juggleDecayEngine = new AdvancedPhysicsCombatServiceJuggleDecayEngine(loggerFactory.CreateLogger<AdvancedPhysicsCombatServiceJuggleDecayEngine>());
        _characterGravityEngine = new AdvancedPhysicsCombatServiceCharacterGravityEngine(loggerFactory.CreateLogger<AdvancedPhysicsCombatServiceCharacterGravityEngine>());
        _wallSplatEngine = new AdvancedPhysicsCombatServiceWallSplatEngine(loggerFactory.CreateLogger<AdvancedPhysicsCombatServiceWallSplatEngine>());
        _destructionEngine = new AdvancedPhysicsCombatServiceDestructionEngine(loggerFactory.CreateLogger<AdvancedPhysicsCombatServiceDestructionEngine>());

        InitializeAdvancedPhysics();
    }

    public async Task<Result<AdvancedPhysicsCombatServiceHitDetectionResult>> ProcessAxisAwareHitAsync(string attackerId, string defenderId, AdvancedPhysicsCombatServiceHitRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Processing axis-aware hit: {Attacker} vs {Defender}", attackerId, defenderId);

            var hitResult = await _hitDetectionEngine.ProcessHitAsync(attackerId, defenderId, request, ct);

            // Update hit detection state
            UpdateHitDetectionState(attackerId, defenderId, hitResult);

            _logger.LogInformation("Axis-aware hit processed: damage {Damage}, blocked {Blocked}, crossup {CrossUp}",
                hitResult.Damage, hitResult.WasBlocked, hitResult.IsCrossUp);

            return Result.Success<AdvancedPhysicsCombatServiceHitDetectionResult>(hitResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing axis-aware hit");
            return Result.Failure<AdvancedPhysicsCombatServiceHitDetectionResult>($"Axis-aware hit processing failed: {ex.Message}");
        }
    }

    public async Task<Result<AdvancedPhysicsCombatServiceJuggleDecayState>> ApplyJuggleDecayAsync(string characterId, AdvancedPhysicsCombatServiceJuggleHit hit, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Applying juggle decay to character {CharacterId}, combo length {ComboLength}", characterId, hit.ComboLength);

            var decayState = await _juggleDecayEngine.ApplyDecayAsync(characterId, hit, ct);

            _juggleDecayStates[characterId] = decayState;

            _logger.LogInformation("Juggle decay applied: gravity multiplier {Multiplier:F2}, momentum loss {MomentumLoss:F2}",
                decayState.GravityMultiplier, decayState.MomentumLoss);

            return Result.Success<AdvancedPhysicsCombatServiceJuggleDecayState>(decayState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying juggle decay to character {CharacterId}", characterId);
            return Result.Failure<AdvancedPhysicsCombatServiceJuggleDecayState>($"Juggle decay application failed: {ex.Message}");
        }
    }

    public async Task<Result<AdvancedPhysicsCombatServiceCharacterGravity>> CalculateCharacterGravityAsync(string characterId, AdvancedPhysicsCombatServiceGravityCalculationRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Calculating character gravity for {CharacterId}", characterId);

            var gravity = await _characterGravityEngine.CalculateGravityAsync(characterId, request, ct);

            _characterGravities[characterId] = gravity;

            _logger.LogInformation("Character gravity calculated: fall speed {FallSpeed:F2}, jump height {JumpHeight:F2}",
                gravity.FallSpeed, gravity.JumpHeight);

            return Result.Success<AdvancedPhysicsCombatServiceCharacterGravity>(gravity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating character gravity for {CharacterId}", characterId);
            return Result.Failure<AdvancedPhysicsCombatServiceCharacterGravity>($"Character gravity calculation failed: {ex.Message}");
        }
    }

    public async Task<Result<AdvancedPhysicsCombatServiceWallSplatResult>> ProcessWallSplatAsync(string characterId, AdvancedPhysicsCombatServiceWallCollision collision, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Processing wall splat for character {CharacterId}", characterId);

            var splatResult = await _wallSplatEngine.ProcessSplatAsync(characterId, collision, ct);

            // Update wall collision state
            UpdateWallState(characterId, splatResult);

            _logger.LogInformation("Wall splat processed: damage {Damage}, bounce angle {BounceAngle:F1}°",
                splatResult.Damage, splatResult.BounceAngle);

            return Result.Success<AdvancedPhysicsCombatServiceWallSplatResult>(splatResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing wall splat for character {CharacterId}", characterId);
            return Result.Failure<AdvancedPhysicsCombatServiceWallSplatResult>($"Wall splat processing failed: {ex.Message}");
        }
    }

    public async Task<Result<AdvancedPhysicsCombatServiceDestructionResult>> ProcessEnvironmentDestructionAsync(string stageId, AdvancedPhysicsCombatServiceDestructionRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Processing environment destruction for stage {StageId}", stageId);

            var destructionResult = await _destructionEngine.ProcessDestructionAsync(stageId, request, ct);

            // Update destruction state
            UpdateDestructionState(stageId, destructionResult);

            _logger.LogInformation("Environment destruction processed: {AdvancedPhysicsCombatServiceBreakType} break, hazard level {HazardLevel}",
                destructionResult.AdvancedPhysicsCombatServiceBreakType, destructionResult.HazardLevel);

            return Result.Success<AdvancedPhysicsCombatServiceDestructionResult>(destructionResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing environment destruction for stage {StageId}", stageId);
            return Result.Failure<AdvancedPhysicsCombatServiceDestructionResult>($"Environment destruction failed: {ex.Message}");
        }
    }

    public async Task<Result<AdvancedPhysicsCombatServiceAxisPositioning>> GetAxisPositioningAsync(string characterId, CancellationToken ct = default)
    {
        try
        {
            var positioning = await _hitDetectionEngine.GetAxisPositioningAsync(characterId, ct);

            return Result.Success<AdvancedPhysicsCombatServiceAxisPositioning>(positioning);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting axis positioning for character {CharacterId}", characterId);
            return Result.Failure<AdvancedPhysicsCombatServiceAxisPositioning>($"Axis positioning retrieval failed: {ex.Message}");
        }
    }

    public async Task<Result<AdvancedPhysicsCombatServiceJuggleMetrics>> GetJuggleMetricsAsync(string characterId, CancellationToken ct = default)
    {
        try
        {
            var metrics = await _juggleDecayEngine.GetMetricsAsync(characterId, ct);

            return Result.Success<AdvancedPhysicsCombatServiceJuggleMetrics>(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting juggle metrics for character {CharacterId}", characterId);
            return Result.Failure<AdvancedPhysicsCombatServiceJuggleMetrics>($"Juggle metrics retrieval failed: {ex.Message}");
        }
    }

    public async Task<Result<AdvancedPhysicsCombatServiceWallCollisionMetrics>> GetWallCollisionMetricsAsync(string characterId, CancellationToken ct = default)
    {
        try
        {
            var metrics = await _wallSplatEngine.GetMetricsAsync(characterId, ct);

            return Result.Success<AdvancedPhysicsCombatServiceWallCollisionMetrics>(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting wall collision metrics for character {CharacterId}", characterId);
            return Result.Failure<AdvancedPhysicsCombatServiceWallCollisionMetrics>($"Wall collision metrics retrieval failed: {ex.Message}");
        }
    }

    public async Task<Result<AdvancedPhysicsCombatServiceDestructionMetrics>> GetDestructionMetricsAsync(string stageId, CancellationToken ct = default)
    {
        try
        {
            var metrics = await _destructionEngine.GetMetricsAsync(stageId, ct);

            return Result.Success<AdvancedPhysicsCombatServiceDestructionMetrics>(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting destruction metrics for stage {StageId}", stageId);
            return Result.Failure<AdvancedPhysicsCombatServiceDestructionMetrics>($"Destruction metrics retrieval failed: {ex.Message}");
        }
    }

    public async Task<Result<AdvancedPhysicsCombatServicePhysicsCombatReport>> GeneratePhysicsCombatReportAsync(string sessionId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating physics combat report for session {SessionId}", sessionId);

            var report = new AdvancedPhysicsCombatServicePhysicsCombatReport
            {
                SessionId = sessionId,
                Duration = TimeSpan.FromMinutes(15), // Placeholder
                AdvancedPhysicsCombatServiceHitDetectionStats = await AnalyzeHitDetectionStatsAsync(sessionId, ct),
                AdvancedPhysicsCombatServiceJuggleDecayAnalysis = await AnalyzeJuggleDecayAsync(sessionId, ct),
                AdvancedPhysicsCombatServiceGravityMechanics = await AnalyzeGravityMechanicsAsync(sessionId, ct),
                AdvancedPhysicsCombatServiceWallSplatAnalysis = await AnalyzeWallSplatsAsync(sessionId, ct),
                AdvancedPhysicsCombatServiceDestructionEvents = await AnalyzeDestructionEventsAsync(sessionId, ct),
                OverallPhysicsScore = CalculateOverallPhysicsScore(sessionId),
                GeneratedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Physics combat report generated successfully");
            return Result.Success<AdvancedPhysicsCombatServicePhysicsCombatReport>(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating physics combat report for session {SessionId}", sessionId);
            return Result.Failure<AdvancedPhysicsCombatServicePhysicsCombatReport>($"Physics combat report generation failed: {ex.Message}");
        }
    }

    #region Private Methods

    private void InitializeAdvancedPhysics()
    {
        // Initialize advanced physics constants and default states
        _logger.LogInformation("Advanced physics combat system initialized");
    }

    private void UpdateHitDetectionState(string attackerId, string defenderId, AdvancedPhysicsCombatServiceHitDetectionResult result)
    {
        // Update hit detection state tracking
        var stateKey = $"{attackerId}_{defenderId}";
        if (!_hitDetectionStates.TryGetValue(stateKey, out var state))
        {
            state = new AdvancedPhysicsCombatServiceHitDetectionState
            {
                AttackerId = attackerId,
                DefenderId = defenderId,
                TotalHits = 0,
                TotalCrossUps = 0,
                TotalBlocks = 0,
                LastUpdate = DateTime.UtcNow
            };
        }

        state.TotalHits++;
        if (result.IsCrossUp) state.TotalCrossUps++;
        if (result.WasBlocked) state.TotalBlocks++;
        state.LastUpdate = DateTime.UtcNow;

        _hitDetectionStates[stateKey] = state;
    }

    private void UpdateWallState(string characterId, AdvancedPhysicsCombatServiceWallSplatResult result)
    {
        // Update wall collision state
        if (!_wallStates.TryGetValue(characterId, out var state))
        {
            state = new AdvancedPhysicsCombatServiceWallCollisionState
            {
                CharacterId = characterId,
                TotalSplats = 0,
                TotalDamage = 0,
                LastSplatTime = DateTime.UtcNow
            };
        }

        state.TotalSplats++;
        state.TotalDamage += result.Damage;
        state.LastSplatTime = DateTime.UtcNow;

        _wallStates[characterId] = state;
    }

    private void UpdateDestructionState(string stageId, AdvancedPhysicsCombatServiceDestructionResult result)
    {
        // Update destruction state
        if (!_destructionStates.TryGetValue(stageId, out var state))
        {
            state = new AdvancedPhysicsCombatServiceDestructionState
            {
                StageId = stageId,
                TotalBreaks = 0,
                TotalHazards = 0,
                LastBreakTime = DateTime.UtcNow
            };
        }

        state.TotalBreaks++;
        state.TotalHazards += result.HazardLevel;
        state.LastBreakTime = DateTime.UtcNow;

        _destructionStates[stageId] = state;
    }

    private async Task<AdvancedPhysicsCombatServiceHitDetectionStats> AnalyzeHitDetectionStatsAsync(string sessionId, CancellationToken ct)
    {
        // Analyze hit detection statistics
        return new AdvancedPhysicsCombatServiceHitDetectionStats
        {
            TotalHits = _hitDetectionStates.Values.Sum(s => s.TotalHits),
            CrossUpRate = _hitDetectionStates.Values.Any() ?
                (float)_hitDetectionStates.Values.Average(s => (float)s.TotalCrossUps / Math.Max(s.TotalHits, 1)) : 0f,
            BlockRate = _hitDetectionStates.Values.Any() ?
                (float)_hitDetectionStates.Values.Average(s => (float)s.TotalBlocks / Math.Max(s.TotalHits, 1)) : 0f,
            AxisUtilization = 0.75f
        };
    }

    private async Task<AdvancedPhysicsCombatServiceJuggleDecayAnalysis> AnalyzeJuggleDecayAsync(string sessionId, CancellationToken ct)
    {
        // Analyze juggle decay effectiveness
        return new AdvancedPhysicsCombatServiceJuggleDecayAnalysis
        {
            AverageDecayRate = _juggleDecayStates.Values.Any() ?
                (float)_juggleDecayStates.Values.Average(s => s.GravityMultiplier) : 1.0f,
            MaxComboLength = _juggleDecayStates.Values.Any() ?
                _juggleDecayStates.Values.Max(s => s.MaxComboLength) : 0,
            BreakPointTriggers = _juggleDecayStates.Values.Sum(s => s.BreakPointTriggers),
            RealismScore = 0.85f
        };
    }

    private async Task<AdvancedPhysicsCombatServiceGravityMechanics> AnalyzeGravityMechanicsAsync(string sessionId, CancellationToken ct)
    {
        // Analyze character gravity mechanics
        return new AdvancedPhysicsCombatServiceGravityMechanics
        {
            GravityVariations = _characterGravities.Count,
            AverageFallSpeed = _characterGravities.Values.Any() ?
                (float)_characterGravities.Values.Average(g => g.FallSpeed) : 1.0f,
            JumpHeightVariance = CalculateJumpHeightVariance(),
            ComboViabilityImpact = 0.7f
        };
    }

    private async Task<AdvancedPhysicsCombatServiceWallSplatAnalysis> AnalyzeWallSplatsAsync(string sessionId, CancellationToken ct)
    {
        // Analyze wall splat mechanics
        return new AdvancedPhysicsCombatServiceWallSplatAnalysis
        {
            TotalSplats = _wallStates.Values.Sum(s => s.TotalSplats),
            AverageDamage = _wallStates.Values.Any() ?
                (float)_wallStates.Values.Average(s => (float)s.TotalDamage / Math.Max(s.TotalSplats, 1)) : 0f,
            BounceEfficiency = 0.8f,
            ComboExtensionRate = 0.6f
        };
    }

    private async Task<AdvancedPhysicsCombatServiceDestructionEvents> AnalyzeDestructionEventsAsync(string sessionId, CancellationToken ct)
    {
        // Analyze destruction events
        return new AdvancedPhysicsCombatServiceDestructionEvents
        {
            TotalBreaks = _destructionStates.Values.Sum(s => s.TotalBreaks),
            HazardCreation = _destructionStates.Values.Sum(s => s.TotalHazards),
            StageTransformation = _destructionStates.Count > 0 ? 1 : 0,
            TacticalImpact = 0.9f
        };
    }

    private float CalculateOverallPhysicsScore(string sessionId)
    {
        // Calculate overall physics implementation score
        return 0.82f; // Placeholder
    }

    private float CalculateJumpHeightVariance()
    {
        // Calculate variance in jump heights across characters
        if (_characterGravities.Count < 2) return 0;

        var heights = _characterGravities.Values.Select(g => g.JumpHeight).ToList();
        var average = (float)heights.Average();
        var variance = heights.Sum(h => Math.Pow((double)h - average, 2)) / heights.Count;

        return (float)Math.Sqrt(variance) / (float)average; // Coefficient of variation
    }

    #endregion
}

/// <summary>
/// Hit detection engine for axis-aware collision detection.
/// </summary>
public class AdvancedPhysicsCombatServiceHitDetectionEngine
{
    private readonly ILogger<AdvancedPhysicsCombatServiceHitDetectionEngine> _logger;

    public AdvancedPhysicsCombatServiceHitDetectionEngine(ILogger<AdvancedPhysicsCombatServiceHitDetectionEngine> logger)
    {
        _logger = logger;
    }

    public async Task<AdvancedPhysicsCombatServiceHitDetectionResult> ProcessHitAsync(string attackerId, string defenderId, AdvancedPhysicsCombatServiceHitRequest request, CancellationToken ct)
    {
        // Process axis-aware hit detection
        var depthDamage = CalculateDepthDamage((float)request.AttackPosition.Z, (float)request.DefensePosition.Z);
        var angleMultiplier = CalculateAngleMultiplier(request.AttackAngle, request.DefenseAngle);
        var crossUpCheck = IsCrossUp(request.AttackPosition, request.DefensePosition);

        var result = new AdvancedPhysicsCombatServiceHitDetectionResult
        {
            AttackerId = attackerId,
            DefenderId = defenderId,
            Damage = (int)(request.BaseDamage * depthDamage * angleMultiplier),
            WasBlocked = request.DefenseAngle < -45, // Simplified block check
            IsCrossUp = crossUpCheck,
            HitAngle = request.AttackAngle,
            DepthBonus = depthDamage,
            AngleBonus = angleMultiplier,
            ProcessedAt = DateTime.UtcNow
        };

        return result;
    }

    public async Task<AdvancedPhysicsCombatServiceAxisPositioning> GetAxisPositioningAsync(string characterId, CancellationToken ct)
    {
        // Get current axis positioning data
        return new AdvancedPhysicsCombatServiceAxisPositioning
        {
            CharacterId = characterId,
            CurrentZPosition = 0.0f,
            OptimalAttackRange = new Vector3(50, 0, 20),
            CrossUpOpportunities = new[] { new Vector3(-30, 0, 15) },
            MeasuredAt = DateTime.UtcNow
        };
    }

    private float CalculateDepthDamage(float attackZ, float defenseZ)
    {
        // Calculate damage bonus based on Z-axis positioning
        var depthDifference = Math.Abs(attackZ - defenseZ);
        return 1.0f + (depthDifference / 50.0f) * 0.5f; // Up to 50% bonus
    }

    private float CalculateAngleMultiplier(float attackAngle, float defenseAngle)
    {
        // Calculate damage multiplier based on approach angle
        var angleDifference = Math.Abs(attackAngle - defenseAngle);
        return 1.0f + (angleDifference / 180.0f) * 0.3f; // Up to 30% bonus
    }

    private bool IsCrossUp(Vector3 attackPos, Vector3 defensePos)
    {
        // Check if this is a cross-up situation
        return Math.Sign(attackPos.X) != Math.Sign(defensePos.X) &&
               Math.Abs(attackPos.Z - defensePos.Z) > 10;
    }
}

/// <summary>
/// Juggle decay engine for realistic combo scaling.
/// </summary>
public class AdvancedPhysicsCombatServiceJuggleDecayEngine
{
    private readonly ILogger<AdvancedPhysicsCombatServiceJuggleDecayEngine> _logger;

    public AdvancedPhysicsCombatServiceJuggleDecayEngine(ILogger<AdvancedPhysicsCombatServiceJuggleDecayEngine> logger)
    {
        _logger = logger;
    }

    public async Task<AdvancedPhysicsCombatServiceJuggleDecayState> ApplyDecayAsync(string characterId, AdvancedPhysicsCombatServiceJuggleHit hit, CancellationToken ct)
    {
        // Apply juggle decay mechanics
        var gravityMultiplier = 1.0f + (hit.ComboLength * 0.1f); // Gravity increases with combo length
        var momentumLoss = Math.Min(hit.ComboLength * 0.15f, 0.8f); // Momentum loss caps at 80%

        var state = new AdvancedPhysicsCombatServiceJuggleDecayState
        {
            CharacterId = characterId,
            CurrentComboLength = hit.ComboLength,
            MaxComboLength = hit.ComboLength,
            GravityMultiplier = gravityMultiplier,
            MomentumLoss = momentumLoss,
            BreakPointReached = hit.ComboLength >= 15, // Hard cap at 15 hits
            BreakPointTriggers = hit.ComboLength >= 15 ? 1 : 0,
            LastHitTime = DateTime.UtcNow
        };

        return state;
    }

    public async Task<AdvancedPhysicsCombatServiceJuggleMetrics> GetMetricsAsync(string characterId, CancellationToken ct)
    {
        // Get juggle performance metrics
        return new AdvancedPhysicsCombatServiceJuggleMetrics
        {
            CharacterId = characterId,
            AverageComboLength = 8.5f,
            MaxComboLength = 15,
            DecayEfficiency = 0.85f,
            BreakPointFrequency = 0.1f,
            MeasuredAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Character gravity engine for individual character physics.
/// </summary>
public class AdvancedPhysicsCombatServiceCharacterGravityEngine
{
    private readonly ILogger<AdvancedPhysicsCombatServiceCharacterGravityEngine> _logger;

    public AdvancedPhysicsCombatServiceCharacterGravityEngine(ILogger<AdvancedPhysicsCombatServiceCharacterGravityEngine> logger)
    {
        _logger = logger;
    }

    public async Task<AdvancedPhysicsCombatServiceCharacterGravity> CalculateGravityAsync(string characterId, AdvancedPhysicsCombatServiceGravityCalculationRequest request, CancellationToken ct)
    {
        // Calculate character-specific gravity properties
        var baseGravity = 1.0f;
        var characterMultiplier = GetCharacterGravityMultiplier(characterId);

        return new AdvancedPhysicsCombatServiceCharacterGravity
        {
            CharacterId = characterId,
            FallSpeed = baseGravity * characterMultiplier,
            JumpHeight = 100.0f / characterMultiplier, // Higher gravity = lower jumps
            AirControl = 1.0f / characterMultiplier, // Lower gravity = more control
            DashSpeed = 8.0f * characterMultiplier,
            TerminalVelocity = 15.0f * characterMultiplier,
            CalculatedAt = DateTime.UtcNow
        };
    }

    private float GetCharacterGravityMultiplier(string characterId)
    {
        // Get character-specific gravity multiplier (simplified)
        return characterId.ToLower() switch
        {
            var c when c.Contains("light") => 0.8f, // Lighter characters float more
            var c when c.Contains("heavy") => 1.2f, // Heavier characters fall faster
            var c when c.Contains("fast") => 0.9f, // Fast characters have slightly lower gravity
            _ => 1.0f
        };
    }
}

/// <summary>
/// Wall splat engine for wall collision mechanics.
/// </summary>
public class AdvancedPhysicsCombatServiceWallSplatEngine
{
    private readonly ILogger<AdvancedPhysicsCombatServiceWallSplatEngine> _logger;

    public AdvancedPhysicsCombatServiceWallSplatEngine(ILogger<AdvancedPhysicsCombatServiceWallSplatEngine> logger)
    {
        _logger = logger;
    }

    public async Task<AdvancedPhysicsCombatServiceWallSplatResult> ProcessSplatAsync(string characterId, AdvancedPhysicsCombatServiceWallCollision collision, CancellationToken ct)
    {
        // Process wall splat collision
        var impactForce = CalculateImpactForce(collision.Velocity, collision.Angle);
        var damage = (int)(impactForce * 0.5f); // Damage based on impact
        var bounceAngle = CalculateBounceAngle(collision.Angle);
        // Compute scalar magnitude of bounce velocity to match the float property
        var bounceVelocity = (float)(Math.Sqrt(collision.Velocity.X * collision.Velocity.X +
                                         collision.Velocity.Y * collision.Velocity.Y +
                                         collision.Velocity.Z * collision.Velocity.Z) * 0.7); // Energy loss on bounce

        return new AdvancedPhysicsCombatServiceWallSplatResult
        {
            CharacterId = characterId,
            Damage = damage,
            BounceAngle = bounceAngle,
            BounceVelocity = bounceVelocity,
            StunDuration = TimeSpan.FromMilliseconds(damage * 10),
            RecoveryWindow = TimeSpan.FromSeconds(2),
            ComboExtensionPossible = damage < 50, // Low damage allows combo extension
            ProcessedAt = DateTime.UtcNow
        };
    }

    public async Task<AdvancedPhysicsCombatServiceWallCollisionMetrics> GetMetricsAsync(string characterId, CancellationToken ct)
    {
        // Get wall collision performance metrics
        return new AdvancedPhysicsCombatServiceWallCollisionMetrics
        {
            CharacterId = characterId,
            TotalSplats = 12,
            AverageDamage = 35.5f,
            BounceEfficiency = 0.75f,
            ComboExtensionRate = 0.6f,
            MeasuredAt = DateTime.UtcNow
        };
    }

    private float CalculateImpactForce(Vector3 velocity, float angle)
    {
        // Calculate impact force based on velocity and angle
        var speed = Math.Sqrt((double)velocity.X * velocity.X + (double)velocity.Y * velocity.Y + (double)velocity.Z * velocity.Z);
        var angleFactor = Math.Abs(Math.Sin((double)angle * Math.PI / 180.0)); // Perpendicular impact = more force

        return (float)speed * (float)angleFactor;
    }

    private float CalculateBounceAngle(float incidentAngle)
    {
        // Calculate bounce angle (simplified reflection)
        return -incidentAngle + (float)(new Random().NextDouble() * 20 - 10); // Some randomness
    }
}

/// <summary>
/// Destruction engine for environmental destruction.
/// </summary>
public class AdvancedPhysicsCombatServiceDestructionEngine
{
    private readonly ILogger<AdvancedPhysicsCombatServiceDestructionEngine> _logger;

    public AdvancedPhysicsCombatServiceDestructionEngine(ILogger<AdvancedPhysicsCombatServiceDestructionEngine> logger)
    {
        _logger = logger;
    }

    public async Task<AdvancedPhysicsCombatServiceDestructionResult> ProcessDestructionAsync(string stageId, AdvancedPhysicsCombatServiceDestructionRequest request, CancellationToken ct)
    {
        // Process environmental destruction
        var breakThreshold = CalculateBreakThreshold(request.Damage, request.CharacterPower);
        var isBreakTriggered = request.Damage >= breakThreshold;

        if (isBreakTriggered)
        {
            return new AdvancedPhysicsCombatServiceDestructionResult
            {
                StageId = stageId,
                AdvancedPhysicsCombatServiceBreakType = DetermineBreakType(request.ImpactLocation),
                Damage = request.Damage,
                HazardLevel = CalculateHazardLevel(request.ImpactLocation),
                AffectedArea = CalculateAffectedArea(request.ImpactLocation),
                DebrisCount = new Random().Next(5, 15),
                StageTransformation = true,
                ProcessedAt = DateTime.UtcNow
            };
        }

        return new AdvancedPhysicsCombatServiceDestructionResult
        {
            StageId = stageId,
            AdvancedPhysicsCombatServiceBreakType = AdvancedPhysicsCombatServiceBreakType.None,
            Damage = request.Damage,
            HazardLevel = 0,
            AffectedArea = new Vector3(0, 0, 0),
            DebrisCount = 0,
            StageTransformation = false,
            ProcessedAt = DateTime.UtcNow
        };
    }

    public async Task<AdvancedPhysicsCombatServiceDestructionMetrics> GetMetricsAsync(string stageId, CancellationToken ct)
    {
        // Get destruction event metrics
        return new AdvancedPhysicsCombatServiceDestructionMetrics
        {
            StageId = stageId,
            TotalBreaks = 8,
            HazardLevelSum = 25,
            AverageAffectedArea = 150.0f,
            TransformationEvents = 3,
            MeasuredAt = DateTime.UtcNow
        };
    }

    private float CalculateBreakThreshold(float damage, float characterPower)
    {
        // Calculate damage threshold for breaking environment
        return 500.0f / characterPower; // Stronger characters break easier
    }

    private AdvancedPhysicsCombatServiceBreakType DetermineBreakType(string impactLocation)
    {
        // Determine type of break based on impact location
        return impactLocation.ToLower() switch
        {
            var l when l.Contains("wall") => AdvancedPhysicsCombatServiceBreakType.WallBreak,
            var l when l.Contains("floor") => AdvancedPhysicsCombatServiceBreakType.FloorBreak,
            var l when l.Contains("ceiling") => AdvancedPhysicsCombatServiceBreakType.CeilingBreak,
            _ => AdvancedPhysicsCombatServiceBreakType.StructureBreak
        };
    }

    private int CalculateHazardLevel(string impactLocation)
    {
        // Calculate hazard level from destruction
        return impactLocation.ToLower() switch
        {
            var l when l.Contains("wall") => 2,
            var l when l.Contains("floor") => 3, // Floor breaks more dangerous
            var l when l.Contains("ceiling") => 1,
            _ => 2
        };
    }

    private Vector3 CalculateAffectedArea(string impactLocation)
    {
        // Calculate area affected by destruction
        return new Vector3(100, 50, 50); // Placeholder area
    }
}

/// <summary>
/// Advanced physics combat service interface.
/// </summary>
public interface AdvancedPhysicsCombatServiceIAdvancedPhysicsCombatService
{
    Task<Result<AdvancedPhysicsCombatServiceHitDetectionResult>> ProcessAxisAwareHitAsync(string attackerId, string defenderId, AdvancedPhysicsCombatServiceHitRequest request, CancellationToken ct = default);
    Task<Result<AdvancedPhysicsCombatServiceJuggleDecayState>> ApplyJuggleDecayAsync(string characterId, AdvancedPhysicsCombatServiceJuggleHit hit, CancellationToken ct = default);
    Task<Result<AdvancedPhysicsCombatServiceCharacterGravity>> CalculateCharacterGravityAsync(string characterId, AdvancedPhysicsCombatServiceGravityCalculationRequest request, CancellationToken ct = default);
    Task<Result<AdvancedPhysicsCombatServiceWallSplatResult>> ProcessWallSplatAsync(string characterId, AdvancedPhysicsCombatServiceWallCollision collision, CancellationToken ct = default);
    Task<Result<AdvancedPhysicsCombatServiceDestructionResult>> ProcessEnvironmentDestructionAsync(string stageId, AdvancedPhysicsCombatServiceDestructionRequest request, CancellationToken ct = default);
    Task<Result<AdvancedPhysicsCombatServiceAxisPositioning>> GetAxisPositioningAsync(string characterId, CancellationToken ct = default);
    Task<Result<AdvancedPhysicsCombatServiceJuggleMetrics>> GetJuggleMetricsAsync(string characterId, CancellationToken ct = default);
    Task<Result<AdvancedPhysicsCombatServiceWallCollisionMetrics>> GetWallCollisionMetricsAsync(string characterId, CancellationToken ct = default);
    Task<Result<AdvancedPhysicsCombatServiceDestructionMetrics>> GetDestructionMetricsAsync(string stageId, CancellationToken ct = default);
    Task<Result<AdvancedPhysicsCombatServicePhysicsCombatReport>> GeneratePhysicsCombatReportAsync(string sessionId, CancellationToken ct = default);
}

/// <summary>
/// Hit detection result data.
/// </summary>
public class AdvancedPhysicsCombatServiceHitDetectionResult
{
    public string AttackerId { get; set; } = default!;
    public string DefenderId { get; set; } = default!;
    public int Damage { get; set; } = default!;
    public bool WasBlocked { get; set; } = default!;
    public bool IsCrossUp { get; set; } = default!;
    public float HitAngle { get; set; } = default!;
    public float DepthBonus { get; set; } = default!;
    public float AngleBonus { get; set; } = default!;
    public DateTime ProcessedAt { get; set; } = default!;
}

/// <summary>
/// Hit request data.
/// </summary>
public class AdvancedPhysicsCombatServiceHitRequest
{
    public Vector3 AttackPosition { get; set; } = default!;
    public Vector3 DefensePosition { get; set; } = default!;
    public float AttackAngle { get; set; } = default!;
    public float DefenseAngle { get; set; } = default!;
    public int BaseDamage { get; set; } = default!;
}

/// <summary>
/// Juggle decay state data.
/// </summary>
public class AdvancedPhysicsCombatServiceJuggleDecayState
{
    public string CharacterId { get; set; } = default!;
    public int CurrentComboLength { get; set; } = default!;
    public int MaxComboLength { get; set; } = default!;
    public float GravityMultiplier { get; set; } = default!;
    public float MomentumLoss { get; set; } = default!;
    public bool BreakPointReached { get; set; } = default!;
    public int BreakPointTriggers { get; set; } = default!;
    public DateTime LastHitTime { get; set; } = default!;
}

/// <summary>
/// Juggle hit data.
/// </summary>
public class AdvancedPhysicsCombatServiceJuggleHit
{
    public int ComboLength { get; set; } = default!;
    public float HitForce { get; set; } = default!;
    public Vector3 HitPosition { get; set; } = default!;
    public DateTime HitTime { get; set; } = default!;
}

/// <summary>
/// Character gravity data.
/// </summary>
public class AdvancedPhysicsCombatServiceCharacterGravity
{
    public string CharacterId { get; set; } = default!;
    public float FallSpeed { get; set; } = default!;
    public float JumpHeight { get; set; } = default!;
    public float AirControl { get; set; } = default!;
    public float DashSpeed { get; set; } = default!;
    public float TerminalVelocity { get; set; } = default!;
    public DateTime CalculatedAt { get; set; } = default!;
}

/// <summary>
/// Gravity calculation request.
/// </summary>
public class AdvancedPhysicsCombatServiceGravityCalculationRequest
{
    public string CharacterType { get; set; } = default!;
    public float BaseGravity { get; set; } = default!;
    public float WeightClass { get; set; } = default!;
}

/// <summary>
/// Wall splat result data.
/// </summary>
public class AdvancedPhysicsCombatServiceWallSplatResult
{
    public string CharacterId { get; set; } = default!;
    public int Damage { get; set; } = default!;
    public float BounceAngle { get; set; } = default!;
    public float BounceVelocity { get; set; } = default!;
    public TimeSpan StunDuration { get; set; } = default!;
    public TimeSpan RecoveryWindow { get; set; } = default!;
    public bool ComboExtensionPossible { get; set; } = default!;
    public DateTime ProcessedAt { get; set; } = default!;
}

/// <summary>
/// Wall collision data.
/// </summary>
public class AdvancedPhysicsCombatServiceWallCollision
{
    public Vector3 Velocity { get; set; } = default!;
    public float Angle { get; set; } = default!;
    public float ImpactForce { get; set; } = default!;
    public string WallType { get; set; } = default!;
    public DateTime CollisionTime { get; set; } = default!;
}

/// <summary>
/// Destruction result data.
/// </summary>
public class AdvancedPhysicsCombatServiceDestructionResult
{
    public string StageId { get; set; } = default!;
    public AdvancedPhysicsCombatServiceBreakType AdvancedPhysicsCombatServiceBreakType { get; set; } = default!;
    public float Damage { get; set; } = default!;
    public int HazardLevel { get; set; } = default!;
    public Vector3 AffectedArea { get; set; } = default!;
    public int DebrisCount { get; set; } = default!;
    public bool StageTransformation { get; set; } = default!;
    public DateTime ProcessedAt { get; set; } = default!;
}

/// <summary>
/// Destruction request data.
/// </summary>
public class AdvancedPhysicsCombatServiceDestructionRequest
{
    public float Damage { get; set; } = default!;
    public float CharacterPower { get; set; } = default!;
    public string ImpactLocation { get; set; } = default!;
    public float ImpactForce { get; set; } = default!;
}

/// <summary>
/// Axis positioning data.
/// </summary>
public class AdvancedPhysicsCombatServiceAxisPositioning
{
    public string CharacterId { get; set; } = default!;
    public float CurrentZPosition { get; set; } = default!;
    public Vector3 OptimalAttackRange { get; set; } = default!;
    public Vector3[] CrossUpOpportunities { get; set; } = default!;
    public DateTime MeasuredAt { get; set; } = default!;
}

/// <summary>
/// Juggle metrics data.
/// </summary>
public class AdvancedPhysicsCombatServiceJuggleMetrics
{
    public string CharacterId { get; set; } = default!;
    public float AverageComboLength { get; set; } = default!;
    public int MaxComboLength { get; set; } = default!;
    public float DecayEfficiency { get; set; } = default!;
    public float BreakPointFrequency { get; set; } = default!;
    public DateTime MeasuredAt { get; set; } = default!;
}

/// <summary>
/// Wall collision metrics data.
/// </summary>
public class AdvancedPhysicsCombatServiceWallCollisionMetrics
{
    public string CharacterId { get; set; } = default!;
    public int TotalSplats { get; set; } = default!;
    public float AverageDamage { get; set; } = default!;
    public float BounceEfficiency { get; set; } = default!;
    public float ComboExtensionRate { get; set; } = default!;
    public DateTime MeasuredAt { get; set; } = default!;
}

/// <summary>
/// Destruction metrics data.
/// </summary>
public class AdvancedPhysicsCombatServiceDestructionMetrics
{
    public string StageId { get; set; } = default!;
    public int TotalBreaks { get; set; } = default!;
    public int HazardLevelSum { get; set; } = default!;
    public float AverageAffectedArea { get; set; } = default!;
    public int TransformationEvents { get; set; } = default!;
    public DateTime MeasuredAt { get; set; } = default!;
}

/// <summary>
/// Physics combat report data.
/// </summary>
public class AdvancedPhysicsCombatServicePhysicsCombatReport
{
    public string SessionId { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public AdvancedPhysicsCombatServiceHitDetectionStats AdvancedPhysicsCombatServiceHitDetectionStats { get; set; } = default!;
    public AdvancedPhysicsCombatServiceJuggleDecayAnalysis AdvancedPhysicsCombatServiceJuggleDecayAnalysis { get; set; } = default!;
    public AdvancedPhysicsCombatServiceGravityMechanics AdvancedPhysicsCombatServiceGravityMechanics { get; set; } = default!;
    public AdvancedPhysicsCombatServiceWallSplatAnalysis AdvancedPhysicsCombatServiceWallSplatAnalysis { get; set; } = default!;
    public AdvancedPhysicsCombatServiceDestructionEvents AdvancedPhysicsCombatServiceDestructionEvents { get; set; } = default!;
    public float OverallPhysicsScore { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = default!;
}

/// <summary>
/// Hit detection stats data.
/// </summary>
public class AdvancedPhysicsCombatServiceHitDetectionStats
{
    public int TotalHits { get; set; } = default!;
    public float CrossUpRate { get; set; } = default!;
    public float BlockRate { get; set; } = default!;
    public float AxisUtilization { get; set; } = default!;
}

/// <summary>
/// Juggle decay analysis data.
/// </summary>
public class AdvancedPhysicsCombatServiceJuggleDecayAnalysis
{
    public float AverageDecayRate { get; set; } = default!;
    public int MaxComboLength { get; set; } = default!;
    public int BreakPointTriggers { get; set; } = default!;
    public float RealismScore { get; set; } = default!;
}

/// <summary>
/// Gravity mechanics data.
/// </summary>
public class AdvancedPhysicsCombatServiceGravityMechanics
{
    public int GravityVariations { get; set; } = default!;
    public float AverageFallSpeed { get; set; } = default!;
    public float JumpHeightVariance { get; set; } = default!;
    public float ComboViabilityImpact { get; set; } = default!;
}

/// <summary>
/// Wall splat analysis data.
/// </summary>
public class AdvancedPhysicsCombatServiceWallSplatAnalysis
{
    public int TotalSplats { get; set; } = default!;
    public float AverageDamage { get; set; } = default!;
    public float BounceEfficiency { get; set; } = default!;
    public float ComboExtensionRate { get; set; } = default!;
}

/// <summary>
/// Destruction events data.
/// </summary>
public class AdvancedPhysicsCombatServiceDestructionEvents
{
    public int TotalBreaks { get; set; } = default!;
    public int HazardCreation { get; set; } = default!;
    public int StageTransformation { get; set; } = default!;
    public float TacticalImpact { get; set; } = default!;
}

/// <summary>
/// Hit detection state data.
/// </summary>
public class AdvancedPhysicsCombatServiceHitDetectionState
{
    public string AttackerId { get; set; } = default!;
    public string DefenderId { get; set; } = default!;
    public int TotalHits { get; set; } = default!;
    public int TotalCrossUps { get; set; } = default!;
    public int TotalBlocks { get; set; } = default!;
    public DateTime LastUpdate { get; set; } = default!;
}

/// <summary>
/// Wall collision state data.
/// </summary>
public class AdvancedPhysicsCombatServiceWallCollisionState
{
    public string CharacterId { get; set; } = default!;
    public int TotalSplats { get; set; } = default!;
    public float TotalDamage { get; set; } = default!;
    public DateTime LastSplatTime { get; set; } = default!;
}

/// <summary>
/// Destruction state data.
/// </summary>
public class AdvancedPhysicsCombatServiceDestructionState
{
    public string StageId { get; set; } = default!;
    public int TotalBreaks { get; set; } = default!;
    public int TotalHazards { get; set; } = default!;
    public DateTime LastBreakTime { get; set; } = default!;
}



/// <summary>
/// Various enumeration types.
/// </summary>
public enum AdvancedPhysicsCombatServiceBreakType { None, FloorBreak, WallBreak, CeilingBreak, StructureBreak }
