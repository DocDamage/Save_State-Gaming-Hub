using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.AiBattleAnalysis.Services;
using AiBattleAnalysisModel = SaveState.Core.Mugen.AiBattleAnalysis.AiBattleAnalysis;

namespace SaveState.Application.Mugen.AiBattleAnalysis.Commands;

/// <summary>
/// Command to stop real-time battle analysis.
/// </summary>
public sealed record StopRealTimeAnalysisCommand(Guid SessionId) : IRequest<Result<AiBattleAnalysisModel>>;

/// <summary>
/// Handler for StopRealTimeAnalysisCommand.
/// </summary>
public sealed class StopRealTimeAnalysisCommandHandler : IRequestHandler<StopRealTimeAnalysisCommand, Result<AiBattleAnalysisModel>>
{
    private readonly IAiBattleAnalysisService _analysisService;

    public StopRealTimeAnalysisCommandHandler(IAiBattleAnalysisService analysisService)
    {
        _analysisService = analysisService;
    }

    public async Task<Result<AiBattleAnalysisModel>> Handle(StopRealTimeAnalysisCommand request, CancellationToken cancellationToken)
    {
        return await _analysisService.StopRealTimeAnalysisAsync(request.SessionId, cancellationToken);
    }
}
