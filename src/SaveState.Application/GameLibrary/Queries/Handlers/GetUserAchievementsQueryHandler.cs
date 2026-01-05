namespace SaveState.Application.GameLibrary.Queries.Handlers;

using MediatR;
using SaveState.Application.GameLibrary.DTOs;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Services;

/// <summary>
/// Handles the GetUserAchievementsQuery.
/// </summary>
public class GetUserAchievementsQueryHandler : IRequestHandler<GetUserAchievementsQuery, IReadOnlyList<UserAchievementDto>>
{
    private readonly IAchievementService _achievementService;

    /// <summary>
    /// Initializes a new instance of the GetUserAchievementsQueryHandler.
    /// </summary>
    /// <param name="achievementService">The achievement service.</param>
    public GetUserAchievementsQueryHandler(IAchievementService achievementService)
    {
        _achievementService = achievementService;
    }

    /// <summary>
    /// Handles the get user achievements query.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of user achievement DTOs.</returns>
    public async Task<IReadOnlyList<UserAchievementDto>> Handle(GetUserAchievementsQuery request, CancellationToken cancellationToken)
    {
        var userAchievements = await _achievementService.GetUserProgressAsync(request.UserId, cancellationToken);

        var result = userAchievements
            .Where(ua => request.IncludeLocked || ua.IsUnlocked)
            .Where(ua => !request.GameId.HasValue || (ua.Achievement != null && ua.Achievement.GameId == request.GameId.Value))
            .Select(ua => new UserAchievementDto(
                ua.Id,
                ua.UserId,
                ua.AchievementId,
                ua.Achievement?.Name ?? "Unknown Achievement",
                ua.Achievement?.Description ?? "",
                ua.Achievement?.IconPath ?? "",
                ua.Achievement?.Points ?? 0,
                ua.Achievement?.Type ?? AchievementType.Special,
                ua.CurrentProgress,
                ua.TargetProgress,
                ua.IsUnlocked,
                ua.UnlockedAt,
                ua.LastUpdatedAt
            ))
            .ToList();

        return result;
    }
}
