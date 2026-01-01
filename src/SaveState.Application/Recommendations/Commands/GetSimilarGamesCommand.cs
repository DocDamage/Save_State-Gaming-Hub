using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Recommendations.Services;

namespace SaveState.Application.Recommendations.Commands;

public sealed record GetSimilarGamesCommand(Guid GameId, int Count = 5) : IRequest<Result<IReadOnlyList<GameRecommendation>>>;

public sealed class GetSimilarGamesCommandHandler : IRequestHandler<GetSimilarGamesCommand, Result<IReadOnlyList<GameRecommendation>>>
{
    private readonly IRecommendationService _recommendationService;

    public GetSimilarGamesCommandHandler(IRecommendationService recommendationService)
    {
        _recommendationService = recommendationService;
    }

    public async Task<Result<IReadOnlyList<GameRecommendation>>> Handle(GetSimilarGamesCommand request, CancellationToken ct)
    {
        return await _recommendationService.GetSimilarGamesAsync(request.GameId, request.Count, ct);
    }
}