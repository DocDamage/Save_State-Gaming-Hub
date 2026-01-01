using MediatR;
using SaveState.Core.Common;
using SaveState.Core.SaveStates.Entities;
using SaveState.Core.SaveStates.Services;
using SaveStateEntity = SaveState.Core.SaveStates.Entities.SaveState;

namespace SaveState.Application.SaveStates.Commands;

public sealed record CreateSaveStateCommand(
    Guid GameId,
    string? Description = null,
    bool CaptureScreenshot = true,
    Guid? ParentStateId = null) : IRequest<Result<SaveStateEntity>>;

public sealed class CreateSaveStateCommandHandler : IRequestHandler<CreateSaveStateCommand, Result<SaveStateEntity>>
{
    private readonly ISaveStateManager _saveStateManager;

    public CreateSaveStateCommandHandler(ISaveStateManager saveStateManager)
    {
        _saveStateManager = saveStateManager;
    }

    public async Task<Result<SaveStateEntity>> Handle(CreateSaveStateCommand request, CancellationToken ct)
    {
        var createRequest = new CreateSaveStateRequest(
            request.Description,
            request.CaptureScreenshot,
            request.ParentStateId);

        return await _saveStateManager.CreateSaveStateAsync(request.GameId, createRequest, ct);
    }
}