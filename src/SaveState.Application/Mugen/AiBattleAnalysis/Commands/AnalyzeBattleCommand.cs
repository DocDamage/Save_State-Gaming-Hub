using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.AiBattleAnalysis;
using SaveState.Core.Mugen.AiBattleAnalysis.Services;
using AiBattleAnalysisModel = SaveState.Core.Mugen.AiBattleAnalysis.AiBattleAnalysis;

namespace SaveState.Application.Mugen.AiBattleAnalysis.Commands;

/// <summary>
/// Command to analyze a battle replay.
/// </summary>
public sealed record AnalyzeBattleCommand(
    string CharacterName,
    string OpponentName,
    string ReplayFilePath,
    BattleAnalysisOptions Options) : IRequest<Result<AiBattleAnalysisModel>>;

/// <summary>
/// Handler for AnalyzeBattleCommand.
/// </summary>
public sealed class AnalyzeBattleCommandHandler : IRequestHandler<AnalyzeBattleCommand, Result<AiBattleAnalysisModel>>
{
    private readonly IAiBattleAnalysisService _analysisService;

    public AnalyzeBattleCommandHandler(IAiBattleAnalysisService analysisService)
    {
        _analysisService = analysisService;
    }

    public async Task<Result<AiBattleAnalysisModel>> Handle(AnalyzeBattleCommand request, CancellationToken cancellationToken)
    {
        var battleRequest = new BattleAnalysisRequest
        {
            CharacterName = request.CharacterName,
            OpponentName = request.OpponentName,
            ReplayFilePath = request.ReplayFilePath,
            Options = request.Options
        };

        return await _analysisService.AnalyzeBattleAsync(battleRequest, cancellationToken);
    }
}
