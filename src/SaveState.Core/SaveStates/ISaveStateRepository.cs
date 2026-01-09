using SaveState.Core.Common;
using SaveState.Core.SaveStates.Entities;
using SaveStateEntity = SaveState.Core.SaveStates.Entities.SaveState;

namespace SaveState.Core.SaveStates;

public interface ISaveStateRepository
{
    Task<SaveStateEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<SaveStateEntity>> GetByGameIdAsync(Guid gameId, CancellationToken ct = default);
    Task<PagedResult<SaveStateEntity>> GetPagedByGameIdAsync(
        Guid gameId,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken ct = default);
    Task AddAsync(SaveStateEntity saveState, CancellationToken ct = default);
    Task UpdateAsync(SaveStateEntity saveState, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<int> CountByGameIdAsync(Guid gameId, CancellationToken ct = default);
    Task<IReadOnlyList<SaveStateEntity>> GetTimelineAsync(Guid gameId, CancellationToken ct = default);
    Task AddBranchAsync(SaveStateBranch branch, CancellationToken ct = default);
}
