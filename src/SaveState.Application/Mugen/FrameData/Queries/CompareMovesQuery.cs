using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.CharacterFrameAnalysis.Services;

namespace SaveState.Application.Mugen.FrameData.Queries;

/// <summary>
/// Query to compare two moves.
/// </summary>
public sealed record CompareMovesQuery(
    string Character1Name, 
    string Move1Name, 
    string Character2Name, 
    string Move2Name) : IRequest<Result<MoveComparison>>;

/// <summary>
/// Handler for CompareMovesQuery.
/// </summary>
public sealed class CompareMovesQueryHandler : IRequestHandler<CompareMovesQuery, Result<MoveComparison>>
{
    private readonly IFrameDataService _frameDataService;

    public CompareMovesQueryHandler(IFrameDataService frameDataService)
    {
        _frameDataService = frameDataService;
    }

    public async Task<Result<MoveComparison>> Handle(CompareMovesQuery request, CancellationToken cancellationToken)
    {
        return await _frameDataService.CompareMovesAsync(
            request.Character1Name, request.Move1Name,
            request.Character2Name, request.Move2Name,
            cancellationToken);
    }
}
