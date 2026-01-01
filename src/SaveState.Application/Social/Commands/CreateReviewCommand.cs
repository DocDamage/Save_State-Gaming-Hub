using MediatR;
using SaveState.Core.Common;

namespace SaveState.Application.Social.Commands;

/// <summary>
/// Command to create a new game review.
/// </summary>
public record CreateReviewCommand(
    Guid GameId,
    int Rating,
    bool IsRecommended,
    string? Title = null,
    string? Content = null,
    TimeSpan? PlaytimeAtReview = null) : IRequest<Result>;

/// <summary>
/// Handler for creating game reviews.
/// </summary>
public class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, Result>
{
    private readonly Core.Social.Services.IGameReviewService _reviewService;

    public CreateReviewCommandHandler(Core.Social.Services.IGameReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    public async Task<Result> Handle(CreateReviewCommand request, CancellationToken ct)
    {
        var result = await _reviewService.CreateReviewAsync(
            request.GameId,
            request.Rating,
            request.IsRecommended,
            request.PlaytimeAtReview,
            ct);

        if (!result.IsSuccess)
        {
            return Result.Failure(result.Error!, result.ErrorType);
        }

        // Optionally update the review with title/content if provided
        if (request.Title is not null || request.Content is not null)
        {
            var updateResult = await _reviewService.UpdateReviewAsync(
                result.Value!.Id,
                title: request.Title,
                content: request.Content,
                ct: ct);

            if (!updateResult.IsSuccess)
            {
                return Result.Failure(updateResult.Error!, updateResult.ErrorType);
            }
        }

        return Result.Success();
    }
}