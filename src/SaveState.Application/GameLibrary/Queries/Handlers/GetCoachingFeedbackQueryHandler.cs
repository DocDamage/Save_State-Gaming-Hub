namespace SaveState.Application.GameLibrary.Queries.Handlers;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Models.AiCoach;
using SaveState.Core.GameLibrary.Services;

/// <summary>
/// Handler for retrieving real-time AI coaching feedback.
/// Provides immediate suggestions and tips during gameplay.
/// </summary>
public class GetCoachingFeedbackQueryHandler : IRequestHandler<GetCoachingFeedbackQuery, Result<CoachingFeedback>>
{
    private readonly IAiCoachService _aiCoachService;

    public GetCoachingFeedbackQueryHandler(IAiCoachService aiCoachService)
    {
        _aiCoachService = aiCoachService;
    }

    /// <summary>
    /// Handles the query to get real-time coaching feedback.
    /// </summary>
    /// <param name="request">The coaching feedback query with game state information.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the coaching feedback or an error.</returns>
    public async Task<Result<CoachingFeedback>> Handle(GetCoachingFeedbackQuery request, CancellationToken ct)
    {
        var gameState = new GameStateSnapshot(
            Timestamp: DateTime.UtcNow,
            GameMode: request.GameMode,
            PlayerScore: request.PlayerScore,
            OpponentScore: request.OpponentScore,
            GameTime: request.GameTime,
            GameSpecificData: request.GameSpecificData ?? new Dictionary<string, object>());

        return await _aiCoachService.GetRealTimeFeedbackAsync(request.SessionId, gameState, ct);
    }
}