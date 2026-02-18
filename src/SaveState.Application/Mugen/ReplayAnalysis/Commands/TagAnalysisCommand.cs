using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ReplayAnalysis.Services;

namespace SaveState.Application.Mugen.ReplayAnalysis.Commands;

/// <summary>
/// Command to tag a replay analysis with custom tags.
/// </summary>
public sealed record TagAnalysisCommand(
    Guid AnalysisId,
    List<string> Tags) : IRequest<Result>;

/// <summary>
/// Handler for TagAnalysisCommand.
/// </summary>
public sealed class TagAnalysisCommandHandler : IRequestHandler<TagAnalysisCommand, Result>
{
    private readonly IReplayAnalysisService _analysisService;

    public TagAnalysisCommandHandler(IReplayAnalysisService analysisService)
    {
        _analysisService = analysisService;
    }

    public async Task<Result> Handle(TagAnalysisCommand request, CancellationToken cancellationToken)
    {
        return await _analysisService.TagAnalysisAsync(
            request.AnalysisId, 
            request.Tags, 
            cancellationToken);
    }
}
