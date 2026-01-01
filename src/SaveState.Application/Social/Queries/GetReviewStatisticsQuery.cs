using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Social;

namespace SaveState.Application.Social.Queries;

/// <summary>
/// Query to get review statistics.
/// </summary>
public record GetReviewStatisticsQuery(Guid? GameId = null) : IRequest<Result<GameReviewStatistics>>;

/// <summary>
/// Handler for getting review statistics.
/// </summary>
public class GetReviewStatisticsQueryHandler : IRequestHandler<GetReviewStatisticsQuery, Result<GameReviewStatistics>>
{
    private readonly Core.Social.Services.IGameReviewService _reviewService;

    public GetReviewStatisticsQueryHandler(Core.Social.Services.IGameReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    public async Task<Result<GameReviewStatistics>> Handle(GetReviewStatisticsQuery request, CancellationToken ct)
    {
        return await _reviewService.GetStatisticsAsync(request.GameId, ct);
    }
}