using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Recommendations.Services;

namespace SaveState.Application.Recommendations.Commands;

public sealed record GetRecommendationsCommand(int Count = 10) : IRequest<Result<IReadOnlyList<GameRecommendation>>>;

public sealed class GetRecommendationsCommandHandler : IRequestHandler<GetRecommendationsCommand, Result<IReadOnlyList<GameRecommendation>>>
{
    private readonly IRecommendationService _recommendationService;

    public GetRecommendationsCommandHandler(IRecommendationService recommendationService)
    {
        _recommendationService = recommendationService;
    }

    public async Task<Result<IReadOnlyList<GameRecommendation>>> Handle(GetRecommendationsCommand request, CancellationToken ct)
    {
        return await _recommendationService.GetRecommendationsAsync(request.Count, ct);
    }
}