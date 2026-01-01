namespace SaveState.Application.GameLibrary.Queries.Handlers;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;

public class GetCoachingFeedbackQueryHandler : IRequestHandler<GetCoachingFeedbackQuery, Result<CoachingFeedback>>
{
    private readonly IAiCoachService _aiCoachService;

    public GetCoachingFeedbackQueryHandler(IAiCoachService aiCoachService)
    {
        _aiCoachService = aiCoachService;
    }

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