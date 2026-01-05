using MediatR;
using SaveState.Core.Common;
using SaveState.Core.SaveStates.Services;

namespace SaveState.Application.SaveStates.Commands;

/// <summary>
/// Configure auto-save for a game.
/// </summary>
public record ConfigureAutoSaveCommand(
    Guid GameId,
    AutoSaveConfig Config) : IRequest<Result>;

/// <summary>
/// Enable auto-save for a game.
/// </summary>
public record EnableAutoSaveCommand(
    Guid GameId) : IRequest<Result>;

/// <summary>
/// Disable auto-save for a game.
/// </summary>
public record DisableAutoSaveCommand(
    Guid GameId) : IRequest<Result>;

/// <summary>
/// Manually trigger an auto-save for a game.
/// </summary>
public record TriggerAutoSaveCommand(
    Guid GameId,
    SaveTrigger Trigger) : IRequest<Result>;

/// <summary>
/// Get auto-save status for a game.
/// </summary>
public record GetAutoSaveStatusCommand(
    Guid GameId) : IRequest<Result<AutoSaveStatus>>;
