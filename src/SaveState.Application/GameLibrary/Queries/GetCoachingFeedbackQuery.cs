namespace SaveState.Application.GameLibrary.Queries;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;

public record GetCoachingFeedbackQuery(
    Guid SessionId,
    string GameMode,
    int PlayerScore,
    int OpponentScore,
    TimeSpan GameTime,
    IReadOnlyDictionary<string, object>? GameSpecificData = null) : IRequest<Result<CoachingFeedback>>;