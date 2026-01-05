using MediatR;
using SaveState.Core.Common;

namespace SaveState.Application.Input.Commands;

/// <summary>
/// Command to apply a controller profile to the current session.
/// </summary>
public record ApplyControllerProfileCommand(
    Guid ProfileId,
    Guid? GameId = null) : IRequest<Result>;
