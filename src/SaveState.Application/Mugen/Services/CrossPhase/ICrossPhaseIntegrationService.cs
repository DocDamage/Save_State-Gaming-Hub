using SaveState.Application.Mugen.Models.CrossPhase;
using SaveState.Core.Common;

namespace SaveState.Application.Mugen.Services.CrossPhase;

/// <summary>
/// Cross-phase integration service providing seamless interaction between all advanced mechanics.
/// Coordinates quantum, emotional, reality, bio, and combat systems for unified gameplay.
/// </summary>
public interface ICrossPhaseIntegrationService
{
    /// <summary>
    /// Processes a mechanic interaction and applies cross-phase effects.
    /// </summary>
    Task<Result<IntegrationResult>> ProcessMechanicInteractionAsync(
        string sessionId,
        MechanicType primaryMechanic,
        MechanicInteraction interaction,
        CancellationToken ct = default);

    /// <summary>
    /// Calculates synergy between two mechanics.
    /// </summary>
    Task<Result<MechanicSynergy>> CalculateMechanicSynergyAsync(
        MechanicType mechanic1,
        MechanicType mechanic2,
        string context,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the unified game state for a session.
    /// </summary>
    Task<Result<UnifiedGameState>> GetUnifiedGameStateAsync(
        string sessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Optimizes integration for a session.
    /// </summary>
    Task<Result<IntegrationOptimization>> OptimizeIntegrationAsync(
        string sessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Resolves mechanic conflicts for a session.
    /// </summary>
    Task<Result<MechanicConflictResolution>> ResolveMechanicConflictsAsync(
        string sessionId,
        IReadOnlyList<MechanicConflict> conflicts,
        CancellationToken ct = default);
}
