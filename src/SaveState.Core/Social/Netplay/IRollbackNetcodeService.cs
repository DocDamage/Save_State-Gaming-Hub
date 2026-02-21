using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Core.Social.Netplay;

/// <summary>
/// Service for managing rollback netcode for fighting games and retro multiplayer.
/// </summary>
public interface IRollbackNetcodeService
{
    /// <summary>
    /// Initializes the rollback netcode for a session.
    /// </summary>
    Task<Result<RollbackState>> InitializeAsync(RollbackConfiguration config, CancellationToken ct = default);

    /// <summary>
    /// Processes local player input and returns confirmed game state.
    /// </summary>
    Task<Result<GameStateUpdate>> ProcessLocalInputAsync(InputFrame input, CancellationToken ct = default);

    /// <summary>
    /// Processes remote player input received from network.
    /// </summary>
    Task<Result> ProcessRemoteInputAsync(InputFrame input, CancellationToken ct = default);

    /// <summary>
    /// Saves the current game state for potential rollback.
    /// </summary>
    Task<Result<GameStateSnapshot>> SaveStateAsync(int frameNumber, CancellationToken ct = default);

    /// <summary>
    /// Loads a previously saved game state for rollback.
    /// </summary>
    Task<Result> LoadStateAsync(int frameNumber, CancellationToken ct = default);

    /// <summary>
    /// Performs rollback to a previous frame when remote input arrives late.
    /// </summary>
    Task<Result<RollbackResult>> PerformRollbackAsync(int toFrame, CancellationToken ct = default);

    /// <summary>
    /// Gets the current synchronization state between players.
    /// </summary>
    Task<Result<SynchronizationState>> GetSyncStateAsync(CancellationToken ct = default);

    /// <summary>
    /// Checks for desynchronization between players.
    /// </summary>
    Task<Result<DesyncReport>> CheckForDesyncAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the current frame advantage/disadvantage.
    /// </summary>
    Task<Result<FrameAdvantage>> GetFrameAdvantageAsync(CancellationToken ct = default);

    /// <summary>
    /// Event raised when a rollback occurs.
    /// </summary>
    event EventHandler<RollbackOccurredEventArgs>? RollbackOccurred;

    /// <summary>
    /// Event raised when desynchronization is detected.
    /// </summary>
    event EventHandler<DesyncDetectedEventArgs>? DesyncDetected;
}

/// <summary>
/// Rollback configuration settings.
/// </summary>
public sealed record RollbackConfiguration(
    int MaxRollbackFrames,
    int InputDelayFrames,
    int LocalInputDelay,
    bool PredictiveInputs,
    int FrameRate,
    int SimulationDelayMs,
    int SpectatorDelayFrames = 60);

/// <summary>
/// Current state of the rollback system.
/// </summary>
public sealed record RollbackState(
    bool IsInitialized,
    int CurrentFrame,
    int ConfirmedFrame,
    int RollbackFrameCount,
    int InputDelay,
    DateTime InitializedAt);

/// <summary>
/// Input frame for a specific player.
/// </summary>
public sealed record InputFrame(
    int FrameNumber,
    string PlayerId,
    byte[] InputData,
    DateTime Timestamp,
    InputSource Source);

/// <summary>
/// Game state update from processing inputs.
/// </summary>
public sealed record GameStateUpdate(
    int FrameNumber,
    byte[] GameState,
    bool WasRolledBack,
    int? RollbackFromFrame,
    long ProcessingTimeMs);

/// <summary>
/// Game state snapshot for rollback.
/// </summary>
public sealed record GameStateSnapshot(
    int FrameNumber,
    byte[] StateData,
    uint Checksum,
    DateTime SavedAt);

/// <summary>
/// Result of a rollback operation.
/// </summary>
public sealed record RollbackResult(
    bool Success,
    int FromFrame,
    int ToFrame,
    int FramesRolledBack,
    int InputsReprocessed,
    long RollbackTimeMs);

/// <summary>
/// Synchronization state between players.
/// </summary>
public sealed record SynchronizationState(
    bool IsSynchronized,
    int LocalFrame,
    int RemoteFrame,
    int FrameAdvantage,
    int LastRemoteInputFrame,
    double TimeSinceLastInputMs);

/// <summary>
/// Desync detection report.
/// </summary>
public sealed record DesyncReport(
    bool DesyncDetected,
    int? DesyncFrame,
    uint? LocalChecksum,
    uint? RemoteChecksum,
    string? DesyncLocation,
    DateTime DetectedAt);

/// <summary>
/// Frame advantage information.
/// </summary>
public sealed record FrameAdvantage(
    int Advantage,
    int LocalFrame,
    int RemoteFrame,
    FrameAdvantageStatus Status);

/// <summary>
/// Input source types.
/// </summary>
public enum InputSource
{
    Local,
    Remote,
    Predicted,
    Spectator
}

/// <summary>
/// Frame advantage status.
/// </summary>
public enum FrameAdvantageStatus
{
    Ahead,
    Even,
    Behind,
    Unknown
}

/// <summary>
/// Event args for rollback occurred events.
/// </summary>
public sealed class RollbackOccurredEventArgs : EventArgs
{
    public int FromFrame { get; }
    public int ToFrame { get; }
    public int FramesRolledBack { get; }
    public long RollbackTimeMs { get; }

    public RollbackOccurredEventArgs(int fromFrame, int toFrame, int framesRolledBack, long rollbackTimeMs)
    {
        FromFrame = fromFrame;
        ToFrame = toFrame;
        FramesRolledBack = framesRolledBack;
        RollbackTimeMs = rollbackTimeMs;
    }
}

/// <summary>
/// Event args for desync detected events.
/// </summary>
public sealed class DesyncDetectedEventArgs : EventArgs
{
    public int DesyncFrame { get; }
    public uint LocalChecksum { get; }
    public uint RemoteChecksum { get; }
    public string? DesyncLocation { get; }
    public DateTime DetectedAt { get; }

    public DesyncDetectedEventArgs(int desyncFrame, uint localChecksum, uint remoteChecksum, string? desyncLocation, ITimeProvider? timeProvider = null)
    {
        DesyncFrame = desyncFrame;
        LocalChecksum = localChecksum;
        RemoteChecksum = remoteChecksum;
        DesyncLocation = desyncLocation;
        DetectedAt = (timeProvider ?? SystemTimeProvider.Instance).UtcNow;
    }
}
