using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.AiBattleAnalysis;
using SaveState.Core.Mugen.AiBattleAnalysis.Services;

namespace SaveState.Application.Mugen.AiBattleAnalysis.Queries;

/// <summary>
/// Query to generate training recommendations.
/// </summary>
public sealed record GenerateTrainingPlanQuery(
    string CharacterName,
    int SessionMinutes = 30) : IRequest<Result<List<TrainingRecommendation>>>;

/// <summary>
/// Handler for GenerateTrainingPlanQuery.
/// </summary>
public sealed class GenerateTrainingPlanQueryHandler : IRequestHandler<GenerateTrainingPlanQuery, Result<List<TrainingRecommendation>>>
{
    private readonly IAiBattleAnalysisService _analysisService;

    public GenerateTrainingPlanQueryHandler(IAiBattleAnalysisService analysisService)
    {
        _analysisService = analysisService;
    }

    public async Task<Result<List<TrainingRecommendation>>> Handle(GenerateTrainingPlanQuery request, CancellationToken cancellationToken)
    {
        return await _analysisService.GenerateTrainingPlanAsync(
            request.CharacterName, request.SessionMinutes, cancellationToken);
    }
}
