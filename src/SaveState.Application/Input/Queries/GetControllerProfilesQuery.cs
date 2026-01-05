using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Input.Entities;

namespace SaveState.Application.Input.Queries;

/// <summary>
/// Query to get all controller profiles, optionally filtered by game or type.
/// </summary>
public record GetControllerProfilesQuery(
    Guid? GameId = null,
    ControllerType? Type = null,
    bool IncludeGlobal = true) : IRequest<Result<IReadOnlyList<ControllerProfileDto>>>;

/// <summary>
/// DTO for controller profile data.
/// </summary>
public record ControllerProfileDto(
    Guid Id,
    string Name,
    ControllerType Type,
    Guid? GameId,
    string? ControllerId,
    IReadOnlyDictionary<string, string> ButtonMappings,
    bool IsDefault,
    DateTime CreatedAt,
    DateTime? LastUsedAt);
