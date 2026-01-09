using MediatR;
using SaveState.Core.Common;
using SaveState.Core.SaveStates;
using SaveState.Core.SaveStates.Entities;
using SaveStateEntity = SaveState.Core.SaveStates.Entities.SaveState;

namespace SaveState.Application.SaveStates.Commands;

/// <summary>
/// Command to duplicate an existing save state with a new description.
/// </summary>
public sealed record DuplicateSaveStateCommand(
    Guid SaveStateId,
    string? NewDescription = null,
    bool MarkAsCurrent = false) : IRequest<Result<SaveStateEntity>>;

/// <summary>
/// Handler for duplicating save states.
/// </summary>
public sealed class DuplicateSaveStateCommandHandler : IRequestHandler<DuplicateSaveStateCommand, Result<SaveStateEntity>>
{
    private readonly ISaveStateRepository _repository;

    public DuplicateSaveStateCommandHandler(ISaveStateRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<SaveStateEntity>> Handle(DuplicateSaveStateCommand request, CancellationToken ct)
    {
        // Get the original save state
        var originalSaveState = await _repository.GetByIdAsync(request.SaveStateId, ct);
        if (originalSaveState == null)
        {
            return Result.Failure<SaveStateEntity>("Save state not found", ErrorType.NotFound);
        }

        // Create a duplicate with the same properties
        var duplicate = SaveStateEntity.Create(
            originalSaveState.GameId,
            originalSaveState.FilePath, // Will need to copy the actual file
            originalSaveState.PlaytimeAtSave,
            isAutoSave: false); // Duplicates are manual saves

        // Copy metadata
        duplicate.SetBranch(originalSaveState.BranchName);
        duplicate.SetDescription(request.NewDescription ?? $"{originalSaveState.Description} (Copy)");
        duplicate.SetGameLocation(originalSaveState.GameLocation);
        duplicate.SetParent(originalSaveState.Id); // Link to original as parent
        duplicate.SetFileSize(originalSaveState.FileSizeBytes);

        if (!string.IsNullOrEmpty(originalSaveState.ThumbnailPath))
        {
            duplicate.SetThumbnail(originalSaveState.ThumbnailPath);
        }

        if (request.MarkAsCurrent)
        {
            duplicate.MarkAsCurrent(true);
        }

        // Save the duplicate
        await _repository.AddAsync(duplicate, ct);

        return Result.Success(duplicate);
    }
}

