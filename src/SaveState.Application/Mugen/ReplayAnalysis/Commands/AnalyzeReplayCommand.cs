using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ReplayAnalysis;
using SaveState.Core.Mugen.ReplayAnalysis.Services;
using ReplayAnalysisModel = SaveState.Core.Mugen.ReplayAnalysis.ReplayAnalysis;

namespace SaveState.Application.Mugen.ReplayAnalysis.Commands;

/// <summary>
/// Command to analyze a replay file.
/// </summary>
public sealed record AnalyzeReplayCommand(
    string ReplayFilePath,
    string? Name = null,
    string? Description = null,
    ReplayAnalysisOptions? Options = null) : IRequest<Result<ReplayAnalysisModel>>;

/// <summary>
/// Handler for AnalyzeReplayCommand.
/// </summary>
public sealed class AnalyzeReplayCommandHandler : IRequestHandler<AnalyzeReplayCommand, Result<ReplayAnalysisModel>>
{
    private readonly IReplayAnalysisService _analysisService;

    public AnalyzeReplayCommandHandler(IReplayAnalysisService analysisService)
    {
        _analysisService = analysisService;
    }

    public async Task<Result<ReplayAnalysisModel>> Handle(AnalyzeReplayCommand request, CancellationToken cancellationToken)
    {
        var analysisRequest = new ReplayAnalysisRequest
        {
            ReplayFilePath = request.ReplayFilePath,
            Name = request.Name,
            Description = request.Description,
            Options = request.Options ?? new ReplayAnalysisOptions()
        };

        return await _analysisService.AnalyzeReplayAsync(analysisRequest, cancellationToken);
    }
}
