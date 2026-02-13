using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.AdvancedCombat;
using CombatSessionRequest = SaveState.Application.Mugen.Models.AdvancedCombat.AdvancedCombatSessionRequest;
using SaveState.Application.Mugen.Services.AdvancedCombat.Engines;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Application.Mugen.Services.AdvancedCombat;

/// <summary>
/// Advanced combat mechanics service providing 3D movement, dynamic physics,
/// timing visualization, and forgiving input systems for professional gameplay.
/// Acts as a coordinator for specialized engines.
/// </summary>
public class AdvancedCombatMechanicsService : IAdvancedCombatMechanicsService
{
    private readonly ILogger<AdvancedCombatMechanicsService> _logger;
    private readonly ICacheService _cache;

    // Specialized engines
    private readonly CombatEngine _combatEngine;
    private readonly ZAxisEngine _zAxisEngine;
    private readonly JuggleEngine _juggleEngine;
    private readonly FrameDataEngine _frameDataEngine;
    private readonly InputBufferEngine _inputBufferEngine;
    private readonly ParryEngine _parryEngine;
    private readonly ComboEngine _comboEngine;

    public AdvancedCombatMechanicsService(
        ILogger<AdvancedCombatMechanicsService> logger,
        ILoggerFactory loggerFactory,
        ICacheService cache)
    {
        _logger = logger;
        _cache = cache;

        // Initialize engines
        _combatEngine = new CombatEngine(loggerFactory.CreateLogger<CombatEngine>());
        _zAxisEngine = new ZAxisEngine(loggerFactory.CreateLogger<ZAxisEngine>());
        _juggleEngine = new JuggleEngine(loggerFactory.CreateLogger<JuggleEngine>());
        _frameDataEngine = new FrameDataEngine(loggerFactory.CreateLogger<FrameDataEngine>());
        _inputBufferEngine = new InputBufferEngine(loggerFactory.CreateLogger<InputBufferEngine>());
        _parryEngine = new ParryEngine(loggerFactory.CreateLogger<ParryEngine>());
        _comboEngine = new ComboEngine(loggerFactory.CreateLogger<ComboEngine>());

        _logger.LogInformation("Advanced combat mechanics system initialized");
    }

    #region Session Management

    public async Task<Result<AdvancedCombatSession>> InitializeCombatSessionAsync(AdvancedCombatSessionRequest request, CancellationToken ct = default)
    {
        return await _combatEngine.InitializeSessionAsync(request, ct);
    }

    public async Task<Result<AdvancedCombatSession>> GetCombatSessionAsync(string sessionId, CancellationToken ct = default)
    {
        return await _combatEngine.GetSessionAsync(sessionId, ct);
    }

    public async Task<Result<bool>> EndCombatSessionAsync(string sessionId, CancellationToken ct = default)
    {
        return await _combatEngine.EndSessionAsync(sessionId, ct);
    }

    #endregion

    #region Z-Axis Movement

    public async Task<Result<ZAxisMovement>> ExecuteSidestepAsync(string sessionId, SidestepRequest request, CancellationToken ct = default)
    {
        var sessionResult = await _combatEngine.GetSessionAsync(sessionId, ct);
        if (!sessionResult.IsSuccess)
        {
            return Result.Failure<ZAxisMovement>("Combat session not found");
        }

        return await _zAxisEngine.ExecuteSidestepAsync(sessionResult.Value, request, ct);
    }

    public async Task<Result<ZAxisPositioning>> GetZAxisPositioningAsync(string sessionId, CancellationToken ct = default)
    {
        var sessionResult = await _combatEngine.GetSessionAsync(sessionId, ct);
        if (!sessionResult.IsSuccess)
        {
            return Result.Failure<ZAxisPositioning>("Combat session not found");
        }

        return await _zAxisEngine.GetPositioningAsync(sessionResult.Value, ct);
    }

    #endregion

    #region Juggle & Physics

    public async Task<Result<JuggleState>> ApplyJuggleGravityAsync(string sessionId, JuggleRequest request, CancellationToken ct = default)
    {
        var sessionResult = await _combatEngine.GetSessionAsync(sessionId, ct);
        if (!sessionResult.IsSuccess)
        {
            return Result.Failure<JuggleState>("Combat session not found");
        }

        return await _juggleEngine.ApplyJuggleGravityAsync(sessionResult.Value, request, ct);
    }

    public async Task<Result<PhysicsState>> GetPhysicsStateAsync(string sessionId, CancellationToken ct = default)
    {
        var sessionResult = await _combatEngine.GetSessionAsync(sessionId, ct);
        if (!sessionResult.IsSuccess)
        {
            return Result.Failure<PhysicsState>("Combat session not found");
        }

        return await _juggleEngine.GetPhysicsStateAsync(sessionResult.Value, ct);
    }

    #endregion

    #region Frame Data

    public async Task<Result<FrameDataDisplay>> DisplayFrameDataAsync(string sessionId, FrameDataRequest request, CancellationToken ct = default)
    {
        return await _frameDataEngine.DisplayFrameDataAsync(sessionId, request, ct);
    }

    public async Task<Result<MoveAnalysis>> AnalyzeMoveFramesAsync(MoveAnalysisRequest request, CancellationToken ct = default)
    {
        return await _frameDataEngine.AnalyzeMoveAsync(request, ct);
    }

    #endregion

    #region Input Buffering

    public async Task<Result<InputBufferResult>> ProcessInputBufferAsync(string sessionId, InputBufferRequest request, CancellationToken ct = default)
    {
        var sessionResult = await _combatEngine.GetSessionAsync(sessionId, ct);
        if (!sessionResult.IsSuccess)
        {
            return Result.Failure<InputBufferResult>("Combat session not found");
        }

        return await _inputBufferEngine.ProcessBufferAsync(sessionResult.Value, request, ct);
    }

    public async Task<Result<InputBufferStats>> GetInputBufferStatsAsync(string sessionId, CancellationToken ct = default)
    {
        var sessionResult = await _combatEngine.GetSessionAsync(sessionId, ct);
        if (!sessionResult.IsSuccess)
        {
            return Result.Failure<InputBufferStats>("Combat session not found");
        }

        return await _inputBufferEngine.GetBufferStatsAsync(sessionResult.Value, ct);
    }

    #endregion

    #region Parry & Counter

    public async Task<Result<ParryResult>> AttemptParryAsync(string sessionId, ParryRequest request, CancellationToken ct = default)
    {
        var sessionResult = await _combatEngine.GetSessionAsync(sessionId, ct);
        if (!sessionResult.IsSuccess)
        {
            return Result.Failure<ParryResult>("Combat session not found");
        }

        return await _parryEngine.AttemptParryAsync(sessionResult.Value, request, ct);
    }

    public async Task<Result<ParryWindow>> ActivateParryWindowAsync(string sessionId, ParryType type, CancellationToken ct = default)
    {
        return await _parryEngine.ActivateParryWindowAsync(sessionId, type, ct);
    }

    #endregion

    #region Combos

    public async Task<Result<ComboSequence>> CreateComboAsync(ComboInputRequest request, CancellationToken ct = default)
    {
        return await _comboEngine.CreateComboAsync(request, ct);
    }

    public async Task<Result<ComboValidation>> ValidateComboAsync(string comboId, CancellationToken ct = default)
    {
        var combo = _comboEngine.GetCombosForSession(comboId).FirstOrDefault();
        if (combo == null)
        {
            return Result.Failure<ComboValidation>("Combo not found");
        }

        return await _comboEngine.ValidateComboAsync(combo, ct);
    }

    public async Task<Result<ComboSequence>> AddMoveToComboAsync(string comboId, string moveName, CancellationToken ct = default)
    {
        return await _comboEngine.AddMoveToComboAsync(comboId, moveName, ct);
    }

    #endregion

    #region Reports

    public async Task<Result<AdvancedCombatReport>> GenerateCombatReportAsync(string sessionId, CancellationToken ct = default)
    {
        var sessionResult = await _combatEngine.GetSessionAsync(sessionId, ct);
        if (!sessionResult.IsSuccess)
        {
            return Result.Failure<AdvancedCombatReport>("Combat session not found");
        }

        var session = sessionResult.Value;

        _logger.LogInformation("Generating advanced combat report for session {SessionId}", sessionId);

        var report = new AdvancedCombatReport
        {
            SessionId = sessionId,
            Duration = DateTime.UtcNow - session.StartedAt,
            ZAxisUtilization = await AnalyzeZAxisUtilizationAsync(session, ct),
            JuggleMechanics = await AnalyzeJuggleMechanicsAsync(session, ct),
            FrameDataInsights = await AnalyzeFrameDataUsageAsync(session, ct),
            InputBufferEfficiency = await AnalyzeInputBufferEfficiencyAsync(session, ct),
            OverallMechanicsScore = CalculateOverallScore(session),
            GeneratedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Advanced combat report generated successfully");
        return Result.Success(report);
    }

    #endregion

    #region Private Analysis Methods

    private async Task<ZAxisUtilization> AnalyzeZAxisUtilizationAsync(AdvancedCombatSession session, CancellationToken ct)
    {
        var movements = _zAxisEngine.GetMovementsForSession(session.SessionId);

        return new ZAxisUtilization
        {
            TotalMovements = movements.Count,
            AverageDistance = movements.Any() ? (float)movements.Average(m => m.Distance) : 0f,
            SidestepFrequency = (float)(movements.Count / Math.Max(1, (DateTime.UtcNow - session.StartedAt).TotalMinutes)),
            PositioningEfficiency = _zAxisEngine.CalculatePositioningEfficiency(movements.ToList(), session),
            EvasionSuccessRate = _zAxisEngine.CalculateEvasionSuccess(movements.ToList())
        };
    }

    private async Task<JuggleMechanics> AnalyzeJuggleMechanicsAsync(AdvancedCombatSession session, CancellationToken ct)
    {
        var juggles = _juggleEngine.GetJugglesForSession(session.SessionId);

        return new JuggleMechanics
        {
            TotalJuggles = juggles.Count,
            AverageGravityScale = juggles.Any() ? (float)juggles.Average(j => j.GravityMultiplier) : 1.0f,
            MaxHeightAchieved = juggles.Any() ? juggles.Max(j => j.CurrentHeight) : 0,
            ComboExtensionRate = _juggleEngine.CalculateComboExtension(juggles.ToList()),
            PhysicsManipulation = juggles.Count > 0 ? 0.8f : 0.0f
        };
    }

    private async Task<FrameDataInsights> AnalyzeFrameDataUsageAsync(AdvancedCombatSession session, CancellationToken ct)
    {
        var displays = _frameDataEngine.GetDisplaysForSession(session.SessionId);

        return new FrameDataInsights
        {
            DisplaysAccessed = displays.Count,
            MovesAnalyzed = displays.SelectMany(d => d.FrameData.FrameBreakdown.Keys).Distinct().Count(),
            TrainingEfficiency = displays.Count > 0 ? 0.85f : 0.0f,
            TimingImprovement = _frameDataEngine.CalculateTimingImprovement(displays.ToList()),
            AnalysisDepth = displays.Any(d => d.ShowAdvanced) ? 0.9f : 0.5f
        };
    }

    private async Task<InputBufferEfficiency> AnalyzeInputBufferEfficiencyAsync(AdvancedCombatSession session, CancellationToken ct)
    {
        var buffers = _inputBufferEngine.GetBuffersForSession(session.SessionId);

        return new InputBufferEfficiency
        {
            BufferSizeUsed = session.BufferWindow,
            InputsBuffered = buffers.Count,
            SuccessfulBuffers = buffers.Count(b => b.Success),
            ForgivenessRate = buffers.Any() ? buffers.Count(b => b.Success) / (float)buffers.Count : 0,
            InputAccuracy = _inputBufferEngine.CalculateInputAccuracy(buffers.ToList())
        };
    }

    private float CalculateOverallScore(AdvancedCombatSession session)
    {
        var zAxisScore = session.EnableZAxisMovement ? 0.25f : 0;
        var juggleScore = session.EnableJuggleScaling ? 0.25f : 0;
        var frameDataScore = session.EnableFrameDataDisplay ? 0.25f : 0;
        var inputBufferScore = session.EnableInputBuffering ? 0.25f : 0;

        return zAxisScore + juggleScore + frameDataScore + inputBufferScore;
    }

    #endregion
}
