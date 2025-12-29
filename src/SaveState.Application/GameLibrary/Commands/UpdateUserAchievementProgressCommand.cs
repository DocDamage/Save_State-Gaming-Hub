namespace SaveState.Application.GameLibrary.Commands;

using MediatR;
using SaveState.Core.GameLibrary.Entities;

/// <summary>
/// Command to update user achievement progress.
/// </summary>
public record UpdateUserAchievementProgressCommand(
    Guid UserId,
    AchievementType AchievementType,
    int ProgressIncrement,
    string? Metadata = null
) : IRequest<Unit>;
