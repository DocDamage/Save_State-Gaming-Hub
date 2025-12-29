namespace SaveState.Application.GameLibrary.Commands;

using MediatR;
using SaveState.Core.GameLibrary.Entities;

/// <summary>
/// Command to create a new achievement definition.
/// </summary>
public record CreateAchievementCommand(
    string Name,
    string Description,
    string IconPath,
    int Points,
    AchievementType Type,
    string? Criteria = null
) : IRequest<Guid>;
