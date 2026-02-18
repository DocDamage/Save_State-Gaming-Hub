using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.AiBattleAnalysis.Services;

namespace SaveState.Application.Mugen.AiBattleAnalysis.Commands;

/// <summary>
/// Command to start real-time battle analysis.
/// </summary>
public sealed record StartRealTimeAnalysisCommand(
    string CharacterName,
    string OpponentName) : IRequest<Result<RealTimeAnalysis>>;

/// <summary>
/// Handler for StartRealTimeAnalysisCommand.
/// </summary>
public sealed class StartRealTimeAnalysisCommandHandler : IRequestHandler<StartRealTimeAnalysisCommand, Result<RealTimeAnalysis>>
{
    private readonly IAiBattleAnalysisService _analysisService;

    public StartRealTimeAnalysisCommandHandler(IAiBattleAnalysisService analysisService)
    {
        _analysisService = analysisService;
    }

    public async Task<Result<RealTimeAnalysis>> Handle(StartRealTimeAnalysisCommand request, CancellationToken cancellationToken)
    {
        return await _analysisService.StartRealTimeAnalysisAsync(
            request.CharacterName, request.OpponentName, cancellationToken);
    }
}
