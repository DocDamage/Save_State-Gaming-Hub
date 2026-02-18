using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.CharacterFrameAnalysis;
using SaveState.Core.Mugen.CharacterFrameAnalysis.Services;

namespace SaveState.Application.Mugen.FrameData.Queries;

/// <summary>
/// Query to load (parse) frame data from a character folder.
/// </summary>
public sealed record LoadCharacterFrameDataQuery(string CharacterPath) : IRequest<Result<CharacterFrameData>>;

/// <summary>
/// Handler for LoadCharacterFrameDataQuery.
/// </summary>
public sealed class LoadCharacterFrameDataQueryHandler : IRequestHandler<LoadCharacterFrameDataQuery, Result<CharacterFrameData>>
{
    private readonly IFrameDataService _frameDataService;

    public LoadCharacterFrameDataQueryHandler(IFrameDataService frameDataService)
    {
        _frameDataService = frameDataService;
    }

    public async Task<Result<CharacterFrameData>> Handle(LoadCharacterFrameDataQuery request, CancellationToken cancellationToken)
    {
        return await _frameDataService.LoadFrameDataAsync(request.CharacterPath, cancellationToken);
    }
}
