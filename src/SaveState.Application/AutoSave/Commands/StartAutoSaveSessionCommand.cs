using MediatR;
using SaveState.Core.Common;
using SaveState.Core.AutoSave;
using SaveState.Core.AutoSave.Services;

namespace SaveState.Application.AutoSave.Commands;

/// <summary>
/// Command to start an auto-save session.
/// </summary>
public sealed record StartAutoSaveSessionCommand(Guid GameId) : IRequest<Result<AutoSaveSession>>;

/// <summary>
/// Handler for StartAutoSaveSessionCommand.
/// </summary>
public sealed class StartAutoSaveSessionCommandHandler : IRequestHandler<StartAutoSaveSessionCommand, Result<AutoSaveSession>>
{
    private readonly IAutoSaveService _autoSaveService;

    public StartAutoSaveSessionCommandHandler(IAutoSaveService autoSaveService)
    {
        _autoSaveService = autoSaveService;
    }

    public async Task<Result<AutoSaveSession>> Handle(StartAutoSaveSessionCommand request, CancellationToken cancellationToken)
    {
        return await _autoSaveService.StartSessionAsync(request.GameId, cancellationToken);
    }
}
