namespace SaveState.Application.Social.Commands;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Social.Services;

public record ShareAchievementCommand(
    Guid AchievementId,
    string AchievementName,
    string Description,
    string Rarity,
    IReadOnlyList<Guid> ShareWithFriends) : IRequest<Result>;