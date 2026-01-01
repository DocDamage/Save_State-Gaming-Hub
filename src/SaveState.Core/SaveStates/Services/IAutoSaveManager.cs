using SaveState.Core.Common;

namespace SaveState.Core.SaveStates.Services;

public interface IAutoSaveManager
{
    Task<Result> ConfigureAutoSaveAsync(Guid gameId, AutoSaveConfig config, CancellationToken ct = default);
    Task<Result> TriggerSaveAsync(Guid gameId, SaveTrigger trigger, CancellationToken ct = default);
    Task<Result> EnableAutoSaveAsync(Guid gameId, CancellationToken ct = default);
    Task<Result> DisableAutoSaveAsync(Guid gameId, CancellationToken ct = default);
    Task<Result<AutoSaveStatus>> GetAutoSaveStatusAsync(Guid gameId, CancellationToken ct = default);
}

public sealed record AutoSaveConfig(
    bool Enabled,
    TimeSpan Interval,
    int MaxAutoSaves,
    IReadOnlyList<SaveTrigger> EnabledTriggers);

[Flags]
public enum SaveTrigger
{
    None = 0,
    TimeInterval = 1 << 0,
    SessionStart = 1 << 1,
    SessionEnd = 1 << 2,
    ManualRequest = 1 << 3,
    SignificantProgress = 1 << 4  // Via game memory detection
}

public sealed record AutoSaveStatus(
    bool IsEnabled,
    DateTime? LastAutoSave,
    int CurrentAutoSaveCount,
    TimeSpan TimeUntilNextSave);