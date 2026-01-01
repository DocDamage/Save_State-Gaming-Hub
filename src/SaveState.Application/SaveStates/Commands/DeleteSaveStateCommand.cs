using MediatR;
using SaveState.Core.Common;
using SaveState.Core.SaveStates.Services;

namespace SaveState.Application.SaveStates.Commands;

public sealed record DeleteSaveStateCommand(Guid SaveStateId) : IRequest<Result>;

public sealed class DeleteSaveStateCommandHandler : IRequestHandler<DeleteSaveStateCommand, Result>
{
    private readonly ISaveStateManager _saveStateManager;

    public DeleteSaveStateCommandHandler(ISaveStateManager saveStateManager)
    {
        _saveStateManager = saveStateManager;
    }

    public async Task<Result> Handle(DeleteSaveStateCommand request, CancellationToken ct)
    {
        return await _saveStateManager.DeleteSaveStateAsync(request.SaveStateId, ct);
    }
}