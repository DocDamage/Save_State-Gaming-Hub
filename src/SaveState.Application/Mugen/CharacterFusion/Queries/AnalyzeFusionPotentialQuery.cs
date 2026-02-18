using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.CharacterFusion;
using SaveState.Core.Mugen.CharacterFusion.Services;

namespace SaveState.Application.Mugen.CharacterFusion.Queries;

/// <summary>
/// Query to analyze fusion potential between two characters.
/// </summary>
public sealed record AnalyzeFusionPotentialQuery(
    Guid Parent1Id,
    Guid Parent2Id) : IRequest<Result<FusionAnalysis>>;

/// <summary>
/// Handler for AnalyzeFusionPotentialQuery.
/// </summary>
public sealed class AnalyzeFusionPotentialQueryHandler : IRequestHandler<AnalyzeFusionPotentialQuery, Result<FusionAnalysis>>
{
    private readonly ICharacterFusionService _fusionService;

    public AnalyzeFusionPotentialQueryHandler(ICharacterFusionService fusionService)
    {
        _fusionService = fusionService;
    }

    public async Task<Result<FusionAnalysis>> Handle(AnalyzeFusionPotentialQuery request, CancellationToken cancellationToken)
    {
        return await _fusionService.AnalyzeFusionPotentialAsync(
            request.Parent1Id, request.Parent2Id, cancellationToken);
    }
}
