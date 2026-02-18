using MediatR;
using SaveState.Core.Common;
using SaveState.Core.AutoSave;
using SaveState.Core.AutoSave.Services;

namespace SaveState.Application.AutoSave.Queries;

/// <summary>
/// Query to get active auto-save session for a game.
/// </summary>
public sealed record GetActiveAutoSaveSessionQuery(Guid GameId) : IRequest<Result<AutoSaveSession>>;

/// <summary>
/// Handler for GetActiveAutoSaveSessionQuery.
/// </summary>
public sealed class GetActiveAutoSaveSessionQueryHandler : IRequestHandler<GetActiveAutoSaveSessionQuery, Result<AutoSaveSession>>
{
    private readonly IAutoSaveService _autoSaveService;

    public GetActiveAutoSaveSessionQueryHandler(IAutoSaveService autoSaveService)
    {
        _autoSaveService = autoSaveService;
    }

    public async Task<Result<AutoSaveSession>> Handle(GetActiveAutoSaveSessionQuery request, CancellationToken cancellationToken)
    {
        return await _autoSaveService.GetActiveSessionAsync(request.GameId, cancellationToken);
    }
}
