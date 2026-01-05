using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Input.Entities;

namespace SaveState.Application.Input.Commands;

/// <summary>
/// Command to create a new controller profile.
/// </summary>
public record CreateControllerProfileCommand(
    string Name,
    ControllerType Type,
    Guid? GameId = null,
    Dictionary<string, string>? ButtonMappings = null,
    string? ControllerId = null) : IRequest<Result<Guid>>;
