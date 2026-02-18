using MediatR;
using SaveState.Core.Common;
using SaveState.Core.AutoSave.Services;

namespace SaveState.Application.AutoSave.Commands;

/// <summary>
/// Command to stop an auto-save session.
/// </summary>
public sealed record StopAutoSaveSessionCommand(Guid SessionId) : IRequest<Result>;

/// <summary>
/// Handler for StopAutoSaveSessionCommand.
/// </summary>
public sealed class StopAutoSaveSessionCommandHandler : IRequestHandler<StopAutoSaveSessionCommand, Result>
{
    private readonly IAutoSaveService _autoSaveService;

    public StopAutoSaveSessionCommandHandler(IAutoSaveService autoSaveService)
    {
        _autoSaveService = autoSaveService;
    }

    public async Task<Result> Handle(StopAutoSaveSessionCommand request, CancellationToken cancellationToken)
    {
        return await _autoSaveService.StopSessionAsync(request.SessionId, cancellationToken);
    }
}
