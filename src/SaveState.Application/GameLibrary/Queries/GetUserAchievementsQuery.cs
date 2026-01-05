namespace SaveState.Application.GameLibrary.Queries;

using MediatR;
using SaveState.Application.GameLibrary.DTOs;

/// <summary>
/// Query to retrieve user achievements and progress.
/// </summary>
public record GetUserAchievementsQuery(
    Guid UserId,
    Guid? GameId = null,
    bool IncludeLocked = true
) : IRequest<IReadOnlyList<UserAchievementDto>>;
