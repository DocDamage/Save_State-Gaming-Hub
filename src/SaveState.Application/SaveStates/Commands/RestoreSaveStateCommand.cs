using MediatR;
using SaveState.Core.Common;
using SaveState.Core.SaveStates.Services;

namespace SaveState.Application.SaveStates.Commands;

public sealed record RestoreSaveStateCommand(Guid SaveStateId) : IRequest<Result>;

public sealed class RestoreSaveStateCommandHandler : IRequestHandler<RestoreSaveStateCommand, Result>
{
    private readonly ISaveStateManager _saveStateManager;

    public RestoreSaveStateCommandHandler(ISaveStateManager saveStateManager)
    {
        _saveStateManager = saveStateManager;
    }

    public async Task<Result> Handle(RestoreSaveStateCommand request, CancellationToken ct)
    {
        return await _saveStateManager.RestoreSaveStateAsync(request.SaveStateId, ct);
    }
}