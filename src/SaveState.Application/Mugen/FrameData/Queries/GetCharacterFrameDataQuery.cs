using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.CharacterFrameAnalysis;
using SaveState.Core.Mugen.CharacterFrameAnalysis.Services;

namespace SaveState.Application.Mugen.FrameData.Queries;

/// <summary>
/// Query to get frame data for a character.
/// </summary>
public sealed record GetCharacterFrameDataQuery(string CharacterName) : IRequest<Result<CharacterFrameData>>;

/// <summary>
/// Handler for GetCharacterFrameDataQuery.
/// </summary>
public sealed class GetCharacterFrameDataQueryHandler : IRequestHandler<GetCharacterFrameDataQuery, Result<CharacterFrameData>>
{
    private readonly IFrameDataService _frameDataService;

    public GetCharacterFrameDataQueryHandler(IFrameDataService frameDataService)
    {
        _frameDataService = frameDataService;
    }

    public async Task<Result<CharacterFrameData>> Handle(GetCharacterFrameDataQuery request, CancellationToken cancellationToken)
    {
        return await _frameDataService.GetFrameDataAsync(request.CharacterName, cancellationToken);
    }
}
