using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Social.Entities;

namespace SaveState.Application.Social.Queries;

/// <summary>
/// Query to get reviews with optional filtering.
/// </summary>
public record GetReviewsQuery(
    int PageNumber = 1,
    int PageSize = 50,
    Guid? GameId = null,
    int? MinRating = null,
    int? MaxRating = null,
    bool? IsRecommended = null,
    bool? ContainsSpoilers = null) : IRequest<Result<PagedResult<GameReview>>>;

/// <summary>
/// Handler for getting reviews with filtering.
/// </summary>
public class GetReviewsQueryHandler : IRequestHandler<GetReviewsQuery, Result<PagedResult<GameReview>>>
{
    private readonly Core.Social.Services.IGameReviewService _reviewService;

    public GetReviewsQueryHandler(Core.Social.Services.IGameReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    public async Task<Result<PagedResult<GameReview>>> Handle(GetReviewsQuery request, CancellationToken ct)
    {
        return await _reviewService.GetReviewsAsync(
            request.PageNumber,
            request.PageSize,
            request.GameId,
            request.MinRating,
            request.MaxRating,
            request.IsRecommended,
            request.ContainsSpoilers,
            ct);
    }
}