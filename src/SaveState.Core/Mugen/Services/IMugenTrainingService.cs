namespace SaveState.Core.Mugen.Services;

using SaveState.Core.Common;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Service interface for MUGEN training mode enhancements.
/// </summary>
public interface IMugenTrainingService
{
    /// <summary>
    /// Records dummy actions for playback in training.
    /// </summary>
    /// <param name="sessionId">The training session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<Result> RecordDummyActionsAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>
    /// Plays back recorded dummy actions.
    /// </summary>
    /// <param name="sessionId">The training session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<Result> PlaybackDummyActionsAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>
    /// Starts a new training session.
    /// </summary>
    /// <param name="characterId">Character ID for training.</param>
    /// <param name="config">Training configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The training session.</returns>
    Task<Result<TrainingSession>> StartSessionAsync(Guid characterId, TrainingConfig config, CancellationToken ct = default);

    /// <summary>
    /// Ends a training session and returns statistics.
    /// </summary>
    /// <param name="sessionId">The training session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The training statistics.</returns>
    Task<Result<TrainingStats>> EndSessionAsync(Guid sessionId, CancellationToken ct = default);
}

/// <summary>
/// Represents an active training session.
/// </summary>
public sealed record TrainingSession(
    Guid Id,
    Guid CharacterId,
    TrainingConfig Config,
    DateTime StartedAt);