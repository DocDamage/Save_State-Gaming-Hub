using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Social.Entities;

namespace SaveState.Application.Social.Queries;

/// <summary>
/// Query to get the review for a specific game.
/// </summary>
public record GetGameReviewQuery(Guid GameId) : IRequest<Result<GameReview?>>;

/// <summary>
/// Handler for getting a game's review.
/// </summary>
public class GetGameReviewQueryHandler : IRequestHandler<GetGameReviewQuery, Result<GameReview?>>
{
    private readonly Core.Social.Services.IGameReviewService _reviewService;

    public GetGameReviewQueryHandler(Core.Social.Services.IGameReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    public async Task<Result<GameReview?>> Handle(GetGameReviewQuery request, CancellationToken ct)
    {
        return await _reviewService.GetGameReviewAsync(request.GameId, ct);
    }
}