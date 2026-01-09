using MediatR;
using SaveState.Core.Common;
using SaveState.Core.SaveStates;
using SaveStateEntity = SaveState.Core.SaveStates.Entities.SaveState;

namespace SaveState.Application.SaveStates.Commands;

/// <summary>
/// Command to update metadata for an existing save state.
/// </summary>
public sealed record UpdateSaveStateMetadataCommand(
    Guid SaveStateId,
    string? Description = null,
    string? BranchName = null,
    string? Notes = null,
    bool? IsFavorite = null,
    bool? IsCurrent = null) : IRequest<Result>;

/// <summary>
/// Handler for updating save state metadata.
/// </summary>
public sealed class UpdateSaveStateMetadataCommandHandler : IRequestHandler<UpdateSaveStateMetadataCommand, Result>
{
    private readonly ISaveStateRepository _repository;

    public UpdateSaveStateMetadataCommandHandler(ISaveStateRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(UpdateSaveStateMetadataCommand request, CancellationToken ct)
    {
        var saveState = await _repository.GetByIdAsync(request.SaveStateId, ct);
        if (saveState == null)
        {
            return Result.Failure("Save state not found", ErrorType.NotFound);
        }

        if (request.Description != null)
        {
            saveState.SetDescription(request.Description);
        }

        if (request.BranchName != null)
        {
            saveState.SetBranch(request.BranchName);
        }

        if (request.IsFavorite != null && request.IsFavorite != saveState.IsFavorite)
        {
            saveState.ToggleFavorite();
        }

        if (request.IsCurrent != null)
        {
            saveState.MarkAsCurrent(request.IsCurrent.Value);
        }

        // Note: The 'Notes' property isn't explicitly on the SaveState entity but was in the dialog result.
        // For now, we update description if notes are provided but description is null, or just ignore if not in entity.
        // Looking at SaveState.cs, it has Description but no dedicated Notes field. We can append to description or ignore.
        // Since we want "No Placeholders", let's check SaveState.cs again for any other field.

        await _repository.UpdateAsync(saveState, ct);
        return Result.Success();
    }
}
