namespace SaveState.Application.GameLibrary.Commands.Handlers;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Models.AiCoach;
using SaveState.Core.GameLibrary.Services;
using SkillLevel = SaveState.Core.GameLibrary.Models.AiCoach.SkillLevel;

/// <summary>
/// Handler for starting AI coaching sessions.
/// Initializes personalized coaching based on user preferences and skill level.
/// </summary>
public class StartAiCoachingCommandHandler : IRequestHandler<StartAiCoachingCommand, Result<CoachingSession>>
{
    private readonly IAiCoachService _aiCoachService;

    public StartAiCoachingCommandHandler(IAiCoachService aiCoachService)
    {
        _aiCoachService = aiCoachService;
    }

    /// <summary>
    /// Handles the command to start an AI coaching session.
    /// </summary>
    /// <param name="request">The start coaching command with preferences.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the coaching session or an error.</returns>
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