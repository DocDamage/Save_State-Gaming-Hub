namespace SaveState.Application.GameLibrary.Commands.Handlers;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;

public class StartAiCoachingCommandHandler : IRequestHandler<StartAiCoachingCommand, Result<CoachingSession>>
{
    private readonly IAiCoachService _aiCoachService;

    public StartAiCoachingCommandHandler(IAiCoachService aiCoachService)
    {
        _aiCoachService = aiCoachService;
    }

    public async Task<Result<CoachingSession>> Handle(StartAiCoachingCommand request, CancellationToken ct)
    {
        var preferences = new CoachingPreferences(
            Style: request.Style,
            TargetSkillLevel: request.TargetSkillLevel,
            FocusAreas: request.FocusAreas,
            EnableRealTimeFeedback: request.EnableRealTimeFeedback,
            EnableStrategyAnalysis: request.EnableStrategyAnalysis,
            EnableOpponentAnalysis: request.EnableOpponentAnalysis);

        return await _aiCoachService.StartCoachingSessionAsync(request.GameId, preferences, ct);
    }
}