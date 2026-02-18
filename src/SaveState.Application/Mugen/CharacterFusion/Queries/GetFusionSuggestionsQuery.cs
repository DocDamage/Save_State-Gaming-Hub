using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.CharacterFusion.Services;

namespace SaveState.Application.Mugen.CharacterFusion.Queries;

/// <summary>
/// Query to get fusion suggestions for a character.
/// </summary>
public sealed record GetFusionSuggestionsQuery(
    Guid CharacterId,
    int Count = 5) : IRequest<Result<List<FusionSuggestion>>>;

/// <summary>
/// Handler for GetFusionSuggestionsQuery.
/// </summary>
public sealed class GetFusionSuggestionsQueryHandler : IRequestHandler<GetFusionSuggestionsQuery, Result<List<FusionSuggestion>>>
{
    private readonly ICharacterFusionService _fusionService;

    public GetFusionSuggestionsQueryHandler(ICharacterFusionService fusionService)
    {
        _fusionService = fusionService;
    }

    public async Task<Result<List<FusionSuggestion>>> Handle(GetFusionSuggestionsQuery request, CancellationToken cancellationToken)
    {
        return await _fusionService.GetFusionSuggestionsAsync(
            request.CharacterId, request.Count, cancellationToken);
    }
}
