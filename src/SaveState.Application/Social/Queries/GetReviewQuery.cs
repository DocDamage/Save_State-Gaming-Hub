using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Social.Entities;

namespace SaveState.Application.Social.Queries;

/// <summary>
/// Query to get a specific review.
/// </summary>
public record GetReviewQuery(Guid ReviewId) : IRequest<Result<GameReview>>;

/// <summary>
/// Handler for getting a specific review.
/// </summary>
public class GetReviewQueryHandler : IRequestHandler<GetReviewQuery, Result<GameReview>>
{
    private readonly Core.Social.Services.IGameReviewService _reviewService;

    public GetReviewQueryHandler(Core.Social.Services.IGameReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    public async Task<Result<GameReview>> Handle(GetReviewQuery request, CancellationToken ct)
    {
        return await _reviewService.GetReviewAsync(request.ReviewId, ct);
    }
}