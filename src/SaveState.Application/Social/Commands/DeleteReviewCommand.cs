using MediatR;
using SaveState.Core.Common;

namespace SaveState.Application.Social.Commands;

/// <summary>
/// Command to delete a game review.
/// </summary>
public record DeleteReviewCommand(Guid ReviewId) : IRequest<Result>;

/// <summary>
/// Handler for deleting game reviews.
/// </summary>
public class DeleteReviewCommandHandler : IRequestHandler<DeleteReviewCommand, Result>
{
    private readonly Core.Social.Services.IGameReviewService _reviewService;

    public DeleteReviewCommandHandler(Core.Social.Services.IGameReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    public async Task<Result> Handle(DeleteReviewCommand request, CancellationToken ct)
    {
        return await _reviewService.DeleteReviewAsync(request.ReviewId, ct);
    }
}