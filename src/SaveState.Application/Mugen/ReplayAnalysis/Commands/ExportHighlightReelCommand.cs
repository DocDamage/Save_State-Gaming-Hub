using MediatR;
using SaveState.Core.Common;
using ReplayAnalysisServices = SaveState.Core.Mugen.ReplayAnalysis.Services;

namespace SaveState.Application.Mugen.ReplayAnalysis.Commands;

/// <summary>
/// Command to export a highlight reel to a video file.
/// </summary>
public sealed record ExportHighlightReelCommand(
    Guid ReelId,
    string OutputPath,
    ReplayAnalysisServices.ExportFormat Format = ReplayAnalysisServices.ExportFormat.Mp4) : IRequest<Result<string>>;

/// <summary>
/// Handler for ExportHighlightReelCommand.
/// </summary>
public sealed class ExportHighlightReelCommandHandler : IRequestHandler<ExportHighlightReelCommand, Result<string>>
{
    private readonly ReplayAnalysisServices.IReplayAnalysisService _analysisService;

    public ExportHighlightReelCommandHandler(ReplayAnalysisServices.IReplayAnalysisService analysisService)
    {
        _analysisService = analysisService;
    }

    public async Task<Result<string>> Handle(ExportHighlightReelCommand request, CancellationToken cancellationToken)
    {
        return await _analysisService.ExportHighlightReelAsync(
            request.ReelId, 
            request.OutputPath, 
            request.Format, 
            cancellationToken);
    }
}
