using SaveState.Core.Common;
using SaveState.Core.SaveStates.Entities;
using SaveStateEntity = SaveState.Core.SaveStates.Entities.SaveState;

namespace SaveState.Core.SaveStates.Services;

public interface ISaveStateManager
{
    Task<Result<SaveStateEntity>> CreateSaveStateAsync(Guid gameId, CreateSaveStateRequest request, CancellationToken ct = default);
    Task<Result> RestoreSaveStateAsync(Guid saveStateId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<SaveStateEntity>>> GetSaveStatesAsync(Guid gameId, CancellationToken ct = default);
    Task<Result> DeleteSaveStateAsync(Guid saveStateId, CancellationToken ct = default);
    Task<Result> ExportSaveStateAsync(Guid saveStateId, string exportPath, CancellationToken ct = default);
    Task<Result<SaveStateEntity>> ImportSaveStateAsync(Guid gameId, string importPath, CancellationToken ct = default);
    Task<Result<byte[]?>> GetThumbnailAsync(Guid saveStateId, CancellationToken ct = default);
    Task<Result<SaveStateTimeline>> GetTimelineAsync(Guid gameId, CancellationToken ct = default);
}

public sealed record CreateSaveStateRequest(
    string? Description = null,
    bool CaptureScreenshot = true,
    Guid? ParentStateId = null);

public sealed record SaveStateTimeline(
    Guid GameId,
    IReadOnlyList<SaveStateNode> Nodes,
    int TotalCount);

public sealed record SaveStateNode(
    Guid Id,
    DateTime CreatedAt,
    string? Description,
    Guid? ParentId,
    bool IsFavorite,
    string? ThumbnailPath);