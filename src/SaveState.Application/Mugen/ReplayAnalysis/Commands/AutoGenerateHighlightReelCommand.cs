using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ReplayAnalysis;
using SaveState.Core.Mugen.ReplayAnalysis.Services;

namespace SaveState.Application.Mugen.ReplayAnalysis.Commands;

/// <summary>
/// Command to automatically generate a highlight reel based on intensity scores.
/// </summary>
public sealed record AutoGenerateHighlightReelCommand(
    Guid AnalysisId,
    int MaxDurationSeconds = 60) : IRequest<Result<HighlightReel>>;

/// <summary>
/// Handler for AutoGenerateHighlightReelCommand.
/// </summary>
public sealed class AutoGenerateHighlightReelCommandHandler : IRequestHandler<AutoGenerateHighlightReelCommand, Result<HighlightReel>>
{
    private readonly IReplayAnalysisService _analysisService;

    public AutoGenerateHighlightReelCommandHandler(IReplayAnalysisService analysisService)
    {
        _analysisService = analysisService;
    }

    public async Task<Result<HighlightReel>> Handle(AutoGenerateHighlightReelCommand request, CancellationToken cancellationToken)
    {
        return await _analysisService.AutoGenerateHighlightReelAsync(
            request.AnalysisId, 
            request.MaxDurationSeconds, 
            cancellationToken);
    }
}
