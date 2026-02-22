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
/// Command to update all installed RetroArch cores.
/// </summary>
public record UpdateAllCoresCommand : IRequest<Result>;

/// <summary>
/// Command to uninstall a RetroArch core.
/// </summary>
public record UninstallCoreCommand(string CoreName) : IRequest<Result>;

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

/// <summary>
/// Command to scan RetroArch library for new games.
/// </summary>
public record ScanLibraryCommand : IRequest<Result<int>>;

/// <summary>
/// Command to join a Netplay lobby.
/// </summary>
public record JoinNetplayLobbyCommand(string LobbyId, string? Password = null) : IRequest<Result>;

/// <summary>
/// Command to join a Netplay lobby by IP address.
/// </summary>
public record JoinNetplayByIpCommand(string HostIp, int Port = 55435) : IRequest<Result>;

/// <summary>
/// Command to host a new Netplay game.
/// </summary>
public record HostNetplayGameCommand(string GamePath, string CorePath, int MaxPlayers = 2, string? Password = null) : IRequest<Result<string>>;

/// <summary>
/// Command to add a game to a playlist.
/// </summary>
public record AddGameToPlaylistCommand(string PlaylistPath, string GamePath, string GameTitle) : IRequest<Result>;

/// <summary>
/// Command to remove a game from a playlist.
/// </summary>
public record RemoveGameFromPlaylistCommand(string PlaylistPath, string GameId) : IRequest<Result>;

/// <summary>
/// Command to create a new playlist.
/// </summary>
public record CreatePlaylistCommand(string Name, string? Description = null) : IRequest<Result<string>>;
