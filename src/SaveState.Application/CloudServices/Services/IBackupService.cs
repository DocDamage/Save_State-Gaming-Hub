using SaveState.Core.Common.ValueObjects;
using SaveState.Core.Common.Enums;
using SaveState.Core.Common;

namespace SaveState.Application.CloudServices.Services;

public interface IBackupService
{
    Task<BackupResult> CreateBackupAsync(
        BackupType type,
        string? name,
        IEnumerable<GameId>? gameIds,
        bool includeSettings,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<BackupMetadata>>> GetBackupHistoryAsync(CancellationToken ct = default);
}

public record BackupMetadata(
    BackupId BackupId,
    string Name,
    DateTime CreatedAt,
    long TotalSize,
    int GamesBackedUp);

public record BackupResult(
    BackupId BackupId,
    string BackupPath,
    long TotalSize,
    int GamesBackedUp,
    TimeSpan Duration);
