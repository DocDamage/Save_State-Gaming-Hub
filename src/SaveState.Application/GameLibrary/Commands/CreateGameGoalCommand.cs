using MediatR;
using SaveState.Core.Common;

namespace SaveState.Application.GameLibrary.Commands;

/// <summary>
/// Command to create a new game goal.
/// </summary>
public record CreateGameGoalCommand(
    Guid GameId,
    Guid UserId,
    string Title,
    string? Description = null,
    int? TargetValue = null,
    string? Unit = null,
    DateTime? DueDate = null) : IRequest<Result<Guid>>;
