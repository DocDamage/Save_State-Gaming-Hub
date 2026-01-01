using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services.DTOs;

namespace SaveState.Application.GameLibrary.Commands;

/// <summary>
/// Command to configure launch experience settings for a game.
/// </summary>
public record ConfigureLaunchExperienceCommand(Guid GameId, LaunchExperienceConfig Config) : IRequest<Result>;

/// <summary>
/// Command to reset launch experience configuration to defaults.
/// </summary>
public record ResetLaunchExperienceCommand(Guid GameId) : IRequest<Result>;

/// <summary>
/// Command to generate a launch sequence for a game.
/// </summary>
public record GenerateLaunchSequenceCommand(Guid GameId) : IRequest<Result<LaunchSequence>>;

/// <summary>
/// Command to execute a launch sequence.
/// </summary>
public record ExecuteLaunchSequenceCommand(LaunchSequence Sequence) : IRequest<Result>;
