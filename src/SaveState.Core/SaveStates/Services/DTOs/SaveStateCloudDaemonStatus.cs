namespace SaveState.Core.SaveStates.Services.DTOs;

/// <summary>
/// Snapshot of background save-state cloud daemon telemetry.
/// </summary>
public sealed record SaveStateCloudDaemonStatus
{
    public required bool Enabled { get; init; }
    public required bool IsRunning { get; init; }
    public required DateTime UpdatedAtUtc { get; init; }
    public DateTime? LastSyncAtUtc { get; init; }
    public Guid? LastGameId { get; init; }
    public required int SuccessfulSyncCount { get; init; }
    public required int FailedSyncCount { get; init; }
    public required int ConflictCount { get; init; }
    public required int SkippedCount { get; init; }
    public required string LastMessage { get; init; }
}
