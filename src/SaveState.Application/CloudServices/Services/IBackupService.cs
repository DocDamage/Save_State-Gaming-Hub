using SaveState.Core.Common.ValueObjects;
using SaveState.Core.Common.Enums;

namespace SaveState.Application.CloudServices.Services;

public interface IBackupService
{
    Task<BackupResult> CreateBackupAsync(
        BackupType type,
        string? name,
        IEnumerable<GameId>? gameIds,
        bool includeSettings,
        CancellationToken ct = default);
}

public record BackupResult(
    BackupId BackupId,
    string BackupPath,
    long TotalSize,
    int GamesBackedUp,
    TimeSpan Duration);
