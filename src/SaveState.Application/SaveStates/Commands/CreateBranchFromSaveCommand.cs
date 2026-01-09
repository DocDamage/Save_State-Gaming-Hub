using MediatR;
using SaveState.Core.Common;
using SaveState.Core.SaveStates;
using SaveState.Core.SaveStates.Entities;
using SaveStateEntity = SaveState.Core.SaveStates.Entities.SaveState;

namespace SaveState.Application.SaveStates.Commands;

/// <summary>
/// Command to create a new branch from a specific save state.
/// This creates a new save state in the new branch, using the specified save as the starting point.
/// </summary>
public sealed record CreateBranchFromSaveCommand(
    Guid SaveStateId,
    string BranchName,
    string? BranchDescription = null) : IRequest<Result<SaveStateEntity>>;

/// <summary>
/// Handler for creating a new branch from an existing save state.
/// </summary>
public sealed class CreateBranchFromSaveCommandHandler : IRequestHandler<CreateBranchFromSaveCommand, Result<SaveStateEntity>>
{
    private readonly ISaveStateRepository _repository;

    public CreateBranchFromSaveCommandHandler(ISaveStateRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<SaveStateEntity>> Handle(CreateBranchFromSaveCommand request, CancellationToken ct)
    {
        // Validate branch name
        if (string.IsNullOrWhiteSpace(request.BranchName))
        {
            return Result.Failure<SaveStateEntity>("Branch name cannot be empty", ErrorType.Validation);
        }

        // Get the source save state
        var sourceSaveState = await _repository.GetByIdAsync(request.SaveStateId, ct);
        if (sourceSaveState == null)
        {
            return Result.Failure<SaveStateEntity>("Source save state not found", ErrorType.NotFound);
        }

        // Create a new save state in the new branch
        var branchSaveState = SaveStateEntity.Create(
            sourceSaveState.GameId,
            sourceSaveState.FilePath, // Will reference the same file initially
            sourceSaveState.PlaytimeAtSave,
            isAutoSave: false);

        // Set branch metadata
        branchSaveState.SetBranch(request.BranchName);
        branchSaveState.SetDescription(request.BranchDescription ?? $"Branch '{request.BranchName}' created from {sourceSaveState.Description}");
        branchSaveState.SetGameLocation(sourceSaveState.GameLocation);
        branchSaveState.SetParent(sourceSaveState.Id); // Link to source save as parent
        branchSaveState.SetFileSize(sourceSaveState.FileSizeBytes);
        branchSaveState.MarkAsCurrent(true); // Mark as current in the new branch

        if (!string.IsNullOrEmpty(sourceSaveState.ThumbnailPath))
        {
            branchSaveState.SetThumbnail(sourceSaveState.ThumbnailPath);
        }

        // Save the new branch save state
        await _repository.AddAsync(branchSaveState, ct);

        return Result.Success(branchSaveState);
    }
}

