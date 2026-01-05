using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Recommendations.DTOs;
using SaveState.Core.Recommendations.Queries;
using SaveState.Core.Recommendations.Services;

namespace SaveState.Infrastructure.Recommendations.Queries;

/// <summary>
/// Handler for GetGameRecommendationsQuery.
/// </summary>
public class GetGameRecommendationsQueryHandler
    : IRequestHandler<GetGameRecommendationsQuery, Result<IReadOnlyList<SmartGameRecommendation>>>
{
    private readonly IGameRecommendationService _recommendationService;

    public GetGameRecommendationsQueryHandler(IGameRecommendationService recommendationService)
    {
        _recommendationService = recommendationService;
    }

    public async Task<Result<IReadOnlyList<SmartGameRecommendation>>> Handle(
        GetGameRecommendationsQuery request,
        CancellationToken cancellationToken)
    {
        return await _recommendationService.GetRecommendationsAsync(
            request.UserId,
            request.Count,
            cancellationToken);
    }
}

/// <summary>
/// Handler for GetSimilarGamesQuery.
/// </summary>
public class GetSimilarGamesQueryHandler
    : IRequestHandler<GetSimilarGamesQuery, Result<IReadOnlyList<SmartSimilarGame>>>
{
    private readonly IGameRecommendationService _recommendationService;

    public GetSimilarGamesQueryHandler(IGameRecommendationService recommendationService)
    {
        _recommendationService = recommendationService;
    }

    public async Task<Result<IReadOnlyList<SmartSimilarGame>>> Handle(
        GetSimilarGamesQuery request,
        CancellationToken cancellationToken)
    {
        return await _recommendationService.GetSimilarGamesAsync(
            request.GameId,
            request.Count,
            cancellationToken);
    }
}

/// <summary>
/// Handler for GetTrendingGamesQuery.
/// </summary>
public class GetTrendingGamesQueryHandler
    : IRequestHandler<GetTrendingGamesQuery, Result<IReadOnlyList<SmartTrendingGame>>>
{
    private readonly IGameRecommendationService _recommendationService;

    public GetTrendingGamesQueryHandler(IGameRecommendationService recommendationService)
    {
        _recommendationService = recommendationService;
    }

    public async Task<Result<IReadOnlyList<SmartTrendingGame>>> Handle(
        GetTrendingGamesQuery request,
        CancellationToken cancellationToken)
    {
        return await _recommendationService.GetTrendingGamesAsync(
            request.Count,
            cancellationToken);
    }
}

/// <summary>
/// Handler for GetBacklogRecommendationsQuery.
/// </summary>
public class GetBacklogRecommendationsQueryHandler
    : IRequestHandler<GetBacklogRecommendationsQuery, Result<IReadOnlyList<SmartBacklogRecommendation>>>
{
    private readonly IGameRecommendationService _recommendationService;

    public GetBacklogRecommendationsQueryHandler(IGameRecommendationService recommendationService)
    {
        _recommendationService = recommendationService;
    }

    public async Task<Result<IReadOnlyList<SmartBacklogRecommendation>>> Handle(
        GetBacklogRecommendationsQuery request,
        CancellationToken cancellationToken)
    {
        return await _recommendationService.GetBacklogRecommendationsAsync(
            request.UserId,
            request.Count,
            cancellationToken);
    }
}

