using MediatR;
using SaveState.Core.Common;

namespace SaveState.Application.RetroArch.Commands;

/// <summary>
/// Command to install a RetroArch core.
/// </summary>
public record InstallCoreCommand(string CoreName) : IRequest<Result>;

/// <summary>
/// Command to update a RetroArch core.
/// </summary>
public record UpdateCoreCommand(string CoreName) : IRequest<Result>;

/// <summary>
/// Command to sync saves via RetroArch cloud.
/// </summary>
public record SyncSavesCommand : IRequest<Result>;

/// <summary>
/// Command to launch a game in RetroArch.
/// </summary>
public record LaunchRetroArchGameCommand(string GamePath, string CorePath) : IRequest<Result>;

/// <summary>
/// Command to import RetroArch games into SaveState library.
/// </summary>
public record ImportRetroArchGamesCommand : IRequest<Result<int>>;
