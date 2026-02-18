using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.CharacterFrameAnalysis;
using SaveState.Core.Mugen.CharacterFrameAnalysis.Services;

namespace SaveState.Application.Mugen.FrameData.Queries;

/// <summary>
/// Query to analyze matchup between two characters.
/// </summary>
public sealed record AnalyzeMatchupQuery(string Character1Name, string Character2Name) : IRequest<Result<MatchupAnalysis>>;

/// <summary>
/// Handler for AnalyzeMatchupQuery.
/// </summary>
public sealed class AnalyzeMatchupQueryHandler : IRequestHandler<AnalyzeMatchupQuery, Result<MatchupAnalysis>>
{
    private readonly IFrameDataService _frameDataService;

    public AnalyzeMatchupQueryHandler(IFrameDataService frameDataService)
    {
        _frameDataService = frameDataService;
    }

    public async Task<Result<MatchupAnalysis>> Handle(AnalyzeMatchupQuery request, CancellationToken cancellationToken)
    {
        return await _frameDataService.AnalyzeMatchupAsync(
            request.Character1Name, request.Character2Name, cancellationToken);
    }
}
