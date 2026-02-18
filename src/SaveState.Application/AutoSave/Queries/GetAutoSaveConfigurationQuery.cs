using MediatR;
using SaveState.Core.Common;
using SaveState.Core.AutoSave;
using SaveState.Core.AutoSave.Services;

namespace SaveState.Application.AutoSave.Queries;

/// <summary>
/// Query to get auto-save configuration for a game.
/// </summary>
public sealed record GetAutoSaveConfigurationQuery(Guid GameId) : IRequest<Result<AutoSaveConfiguration>>;

/// <summary>
/// Handler for GetAutoSaveConfigurationQuery.
/// </summary>
public sealed class GetAutoSaveConfigurationQueryHandler : IRequestHandler<GetAutoSaveConfigurationQuery, Result<AutoSaveConfiguration>>
{
    private readonly IAutoSaveService _autoSaveService;

    public GetAutoSaveConfigurationQueryHandler(IAutoSaveService autoSaveService)
    {
        _autoSaveService = autoSaveService;
    }

    public async Task<Result<AutoSaveConfiguration>> Handle(GetAutoSaveConfigurationQuery request, CancellationToken cancellationToken)
    {
        return await _autoSaveService.GetConfigurationAsync(request.GameId, cancellationToken);
    }
}
