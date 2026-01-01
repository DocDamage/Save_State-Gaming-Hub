using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Recommendations.Services;

namespace SaveState.Application.Recommendations.Commands;

public sealed record ProvideRecommendationFeedbackCommand(
    Guid RecommendationId,
    RecommendationFeedback Feedback) : IRequest<Result>;

public sealed class ProvideRecommendationFeedbackCommandHandler : IRequestHandler<ProvideRecommendationFeedbackCommand, Result>
{
    private readonly IRecommendationService _recommendationService;

    public ProvideRecommendationFeedbackCommandHandler(IRecommendationService recommendationService)
    {
        _recommendationService = recommendationService;
    }

    public async Task<Result> Handle(ProvideRecommendationFeedbackCommand request, CancellationToken ct)
    {
        return await _recommendationService.ProvideRecommendationFeedbackAsync(
            request.RecommendationId,
            request.Feedback,
            ct);
    }
}