using SaveState.Core.SaveStates.Services.DTOs;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// Represents a backup history item for display.
/// </summary>
public sealed class BackupHistoryItem
{
    public required string Name { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required long SizeBytes { get; init; }
    public required string BackupType { get; init; }
    public required string Status { get; init; }
}

/// <summary>
/// Internal record for save-state conflict tracking.
/// </summary>
internal sealed record SaveStateConflictEntry(Guid GameId, SaveStateConflictResolution Conflict);

/// <summary>
/// Internal record for conflict resolution results.
/// </summary>
internal sealed record ConflictApplyResult(bool Success, string? Error)
{
    public static ConflictApplyResult Successful() => new(true, null);
    public static ConflictApplyResult Failed(string? error = null) => new(false, error);
}

/// <summary>
/// Internal record for daemon health evaluation.
/// </summary>
internal sealed record DaemonHealthSnapshot(
    string Status,
    string Cue,
    bool ShowResolveConflictsQuickAction,
    bool ShowRetrySyncQuickAction,
    bool ShowConfigureProviderQuickAction);
