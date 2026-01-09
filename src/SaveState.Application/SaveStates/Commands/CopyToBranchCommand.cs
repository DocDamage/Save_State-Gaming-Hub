using MediatR;
using SaveState.Core.Common;
using SaveState.Core.SaveStates;
using SaveState.Core.SaveStates.Entities;
using SaveStateEntity = SaveState.Core.SaveStates.Entities.SaveState;

namespace SaveState.Application.SaveStates.Commands;

/// <summary>
/// Command to copy a save state to a different branch.
/// This creates a new save state in the target branch while preserving the original.
/// </summary>
public sealed record CopyToBranchCommand(
    Guid SaveStateId,
    string TargetBranchName,
    bool MarkAsCurrent = false) : IRequest<Result<SaveStateEntity>>;

/// <summary>
/// Handler for copying a save state to another branch.
/// </summary>
public sealed class CopyToBranchCommandHandler : IRequestHandler<CopyToBranchCommand, Result<SaveStateEntity>>
{
    private readonly ISaveStateRepository _repository;

    public CopyToBranchCommandHandler(ISaveStateRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<SaveStateEntity>> Handle(CopyToBranchCommand request, CancellationToken ct)
    {
        // Validate target branch name
        if (string.IsNullOrWhiteSpace(request.TargetBranchName))
        {
            return Result.Failure<SaveStateEntity>("Target branch name cannot be empty", ErrorType.Validation);
        }

        // Get the source save state
        var sourceSaveState = await _repository.GetByIdAsync(request.SaveStateId, ct);
        if (sourceSaveState == null)
        {
            return Result.Failure<SaveStateEntity>("Source save state not found", ErrorType.NotFound);
        }

        // Prevent copying to the same branch
        if (sourceSaveState.BranchName.Equals(request.TargetBranchName, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<SaveStateEntity>(
                $"Save state is already in branch '{request.TargetBranchName}'",
                ErrorType.Validation);
        }

        // Create a copy in the target branch
        var copiedSaveState = SaveStateEntity.Create(
            sourceSaveState.GameId,
            sourceSaveState.FilePath, // Will reference the same file
            sourceSaveState.PlaytimeAtSave,
            isAutoSave: false);

        // Set metadata for the copied save
        copiedSaveState.SetBranch(request.TargetBranchName);
        copiedSaveState.SetDescription($"{sourceSaveState.Description} (copied from {sourceSaveState.BranchName})");
        copiedSaveState.SetGameLocation(sourceSaveState.GameLocation);
        copiedSaveState.SetParent(sourceSaveState.Id); // Link to source as parent
        copiedSaveState.SetFileSize(sourceSaveState.FileSizeBytes);

        if (!string.IsNullOrEmpty(sourceSaveState.ThumbnailPath))
        {
            copiedSaveState.SetThumbnail(sourceSaveState.ThumbnailPath);
        }

        if (request.MarkAsCurrent)
        {
            copiedSaveState.MarkAsCurrent(true);
        }

        // Save the copied save state
        await _repository.AddAsync(copiedSaveState, ct);

        return Result.Success(copiedSaveState);
    }
}

