using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.CharacterFusion;
using SaveState.Core.Mugen.CharacterFusion.Services;

namespace SaveState.Application.Mugen.CharacterFusion.Queries;

/// <summary>
/// Query to get a fused character by ID.
/// </summary>
public sealed record GetFusionQuery(Guid FusionId) : IRequest<Result<FusedCharacter>>;

/// <summary>
/// Handler for GetFusionQuery.
/// </summary>
public sealed class GetFusionQueryHandler : IRequestHandler<GetFusionQuery, Result<FusedCharacter>>
{
    private readonly ICharacterFusionService _fusionService;

    public GetFusionQueryHandler(ICharacterFusionService fusionService)
    {
        _fusionService = fusionService;
    }

    public async Task<Result<FusedCharacter>> Handle(GetFusionQuery request, CancellationToken cancellationToken)
    {
        return await _fusionService.GetFusionAsync(request.FusionId, cancellationToken);
    }
}
