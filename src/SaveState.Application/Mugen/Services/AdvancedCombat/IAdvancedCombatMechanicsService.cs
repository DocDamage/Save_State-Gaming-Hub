using SaveState.Application.Mugen.Models.AdvancedCombat;
using SaveState.Core.Common;

namespace SaveState.Application.Mugen.Services.AdvancedCombat;

/// <summary>
/// Advanced combat mechanics service interface.
/// </summary>
public interface IAdvancedCombatMechanicsService
{
    // Session Management
    Task<Result<AdvancedCombatSession>> InitializeCombatSessionAsync(AdvancedCombatSessionRequest request, CancellationToken ct = default);
    Task<Result<AdvancedCombatSession>> GetCombatSessionAsync(string sessionId, CancellationToken ct = default);
    Task<Result<bool>> EndCombatSessionAsync(string sessionId, CancellationToken ct = default);

    // Z-Axis Movement
    Task<Result<ZAxisMovement>> ExecuteSidestepAsync(string sessionId, SidestepRequest request, CancellationToken ct = default);
    Task<Result<ZAxisPositioning>> GetZAxisPositioningAsync(string sessionId, CancellationToken ct = default);

    // Juggle & Physics
    Task<Result<JuggleState>> ApplyJuggleGravityAsync(string sessionId, JuggleRequest request, CancellationToken ct = default);
    Task<Result<PhysicsState>> GetPhysicsStateAsync(string sessionId, CancellationToken ct = default);

    // Frame Data
    Task<Result<FrameDataDisplay>> DisplayFrameDataAsync(string sessionId, FrameDataRequest request, CancellationToken ct = default);
    Task<Result<MoveAnalysis>> AnalyzeMoveFramesAsync(MoveAnalysisRequest request, CancellationToken ct = default);

    // Input Buffering
    Task<Result<InputBufferResult>> ProcessInputBufferAsync(string sessionId, InputBufferRequest request, CancellationToken ct = default);
    Task<Result<InputBufferStats>> GetInputBufferStatsAsync(string sessionId, CancellationToken ct = default);

    // Parry & Counter
    Task<Result<ParryResult>> AttemptParryAsync(string sessionId, ParryRequest request, CancellationToken ct = default);
    Task<Result<ParryWindow>> ActivateParryWindowAsync(string sessionId, ParryType type, CancellationToken ct = default);

    // Combos
    Task<Result<ComboSequence>> CreateComboAsync(ComboInputRequest request, CancellationToken ct = default);
    Task<Result<ComboValidation>> ValidateComboAsync(string comboId, CancellationToken ct = default);
    Task<Result<ComboSequence>> AddMoveToComboAsync(string comboId, string moveName, CancellationToken ct = default);

    // Reports
    Task<Result<AdvancedCombatReport>> GenerateCombatReportAsync(string sessionId, CancellationToken ct = default);
}
