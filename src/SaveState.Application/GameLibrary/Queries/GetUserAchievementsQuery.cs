namespace SaveState.Application.GameLibrary.Queries;

using MediatR;
using SaveState.Application.GameLibrary.DTOs;

/// <summary>
/// Query to retrieve user achievements and progress.
/// </summary>
public record GetUserAchievementsQuery(
    Guid UserId,
    bool IncludeLocked = true
) : IRequest<IReadOnlyList<UserAchievementDto>>;
