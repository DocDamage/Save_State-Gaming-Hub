using MediatR;
using SaveState.Core.Common;

namespace SaveState.Application.Social.Commands;

/// <summary>
/// Command to update an existing game review.
/// </summary>
public record UpdateReviewCommand(
    Guid ReviewId,
    int? Rating = null,
    string? Title = null,
    string? Content = null,
    bool? ContainsSpoilers = null) : IRequest<Result>;

/// <summary>
/// Handler for updating game reviews.
/// </summary>
public class UpdateReviewCommandHandler : IRequestHandler<UpdateReviewCommand, Result>
{
    private readonly Core.Social.Services.IGameReviewService _reviewService;

    public UpdateReviewCommandHandler(Core.Social.Services.IGameReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    public async Task<Result> Handle(UpdateReviewCommand request, CancellationToken ct)
    {
        return await _reviewService.UpdateReviewAsync(
            request.ReviewId,
            request.Rating,
            request.Title,
            request.Content,
            request.ContainsSpoilers,
            ct);
    }
}