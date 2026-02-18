using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ReplayAnalysis.Services;

namespace SaveState.Application.Mugen.ReplayAnalysis.Commands;

/// <summary>
/// Command to delete a replay analysis.
/// </summary>
public sealed record DeleteAnalysisCommand(Guid AnalysisId) : IRequest<Result>;

/// <summary>
/// Handler for DeleteAnalysisCommand.
/// </summary>
public sealed class DeleteAnalysisCommandHandler : IRequestHandler<DeleteAnalysisCommand, Result>
{
    private readonly IReplayAnalysisService _analysisService;

    public DeleteAnalysisCommandHandler(IReplayAnalysisService analysisService)
    {
        _analysisService = analysisService;
    }

    public async Task<Result> Handle(DeleteAnalysisCommand request, CancellationToken cancellationToken)
    {
        return await _analysisService.DeleteAnalysisAsync(request.AnalysisId, cancellationToken);
    }
}
