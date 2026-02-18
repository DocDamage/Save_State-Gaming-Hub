using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ComboDatabase.Services;

namespace SaveState.Application.Mugen.ComboDatabase.Queries;

/// <summary>
/// Query to get combo routes analysis for a character.
/// </summary>
public sealed record GetComboRoutesQuery(string CharacterName) : IRequest<Result<ComboRoutesAnalysis>>;

/// <summary>
/// Handler for GetComboRoutesQuery.
/// </summary>
public sealed class GetComboRoutesQueryHandler : IRequestHandler<GetComboRoutesQuery, Result<ComboRoutesAnalysis>>
{
    private readonly IComboDatabaseService _comboService;

    public GetComboRoutesQueryHandler(IComboDatabaseService comboService)
    {
        _comboService = comboService;
    }

    public async Task<Result<ComboRoutesAnalysis>> Handle(GetComboRoutesQuery request, CancellationToken cancellationToken)
    {
        return await _comboService.GetComboRoutesAsync(request.CharacterName, cancellationToken);
    }
}
