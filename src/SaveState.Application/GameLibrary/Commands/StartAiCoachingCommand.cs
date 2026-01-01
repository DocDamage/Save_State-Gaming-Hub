namespace SaveState.Application.GameLibrary.Commands;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;

public record StartAiCoachingCommand(
    Guid GameId,
    CoachingStyle Style,
    SkillLevel TargetSkillLevel,
    IReadOnlyList<CoachingFocus> FocusAreas,
    bool EnableRealTimeFeedback = true,
    bool EnableStrategyAnalysis = true,
    bool EnableOpponentAnalysis = true) : IRequest<Result<CoachingSession>>;