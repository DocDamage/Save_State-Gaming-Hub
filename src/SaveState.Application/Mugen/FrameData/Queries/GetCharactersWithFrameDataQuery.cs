using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.CharacterFrameAnalysis.Services;

namespace SaveState.Application.Mugen.FrameData.Queries;

/// <summary>
/// Query to get all characters with frame data.
/// </summary>
public sealed record GetCharactersWithFrameDataQuery : IRequest<Result<List<string>>>;

/// <summary>
/// Handler for GetCharactersWithFrameDataQuery.
/// </summary>
public sealed class GetCharactersWithFrameDataQueryHandler : IRequestHandler<GetCharactersWithFrameDataQuery, Result<List<string>>>
{
    private readonly IFrameDataService _frameDataService;

    public GetCharactersWithFrameDataQueryHandler(IFrameDataService frameDataService)
    {
        _frameDataService = frameDataService;
    }

    public async Task<Result<List<string>>> Handle(GetCharactersWithFrameDataQuery request, CancellationToken cancellationToken)
    {
        return await _frameDataService.GetCharactersWithFrameDataAsync(cancellationToken);
    }
}
