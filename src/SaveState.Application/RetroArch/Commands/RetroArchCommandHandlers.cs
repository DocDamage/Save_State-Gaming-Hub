using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.ValueObjects;
using SaveState.Core.RetroArch.Services;

namespace SaveState.Application.RetroArch.Commands;

public class InstallCoreCommandHandler : IRequestHandler<InstallCoreCommand, Result>
{
    private readonly IRetroArchService _retroArchService;
    private readonly ILogger<InstallCoreCommandHandler> _logger;

    public InstallCoreCommandHandler(IRetroArchService retroArchService, ILogger<InstallCoreCommandHandler> logger)
    {
        _retroArchService = retroArchService;
        _logger = logger;
    }

    public async Task<Result> Handle(InstallCoreCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Installing RetroArch core: {CoreName}", request.CoreName);
        return await _retroArchService.InstallCoreAsync(request.CoreName, cancellationToken);
    }
}

public class UpdateCoreCommandHandler : IRequestHandler<UpdateCoreCommand, Result>
{
    private readonly IRetroArchService _retroArchService;
    private readonly ILogger<UpdateCoreCommandHandler> _logger;

    public UpdateCoreCommandHandler(IRetroArchService retroArchService, ILogger<UpdateCoreCommandHandler> logger)
    {
        _retroArchService = retroArchService;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateCoreCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating RetroArch core: {CoreName}", request.CoreName);
        return await _retroArchService.UpdateCoreAsync(request.CoreName, cancellationToken);
    }
}

public class SyncSavesCommandHandler : IRequestHandler<SyncSavesCommand, Result>
{
    private readonly IRetroArchService _retroArchService;
    private readonly ILogger<SyncSavesCommandHandler> _logger;

    public SyncSavesCommandHandler(IRetroArchService retroArchService, ILogger<SyncSavesCommandHandler> logger)
    {
        _retroArchService = retroArchService;
        _logger = logger;
    }

    public async Task<Result> Handle(SyncSavesCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Syncing RetroArch saves");
        return await _retroArchService.SyncSavesAsync(cancellationToken);
    }
}

public class LaunchRetroArchGameCommandHandler : IRequestHandler<LaunchRetroArchGameCommand, Result>
{
    private readonly IRetroArchService _retroArchService;
    private readonly ILogger<LaunchRetroArchGameCommandHandler> _logger;

    public LaunchRetroArchGameCommandHandler(IRetroArchService retroArchService, ILogger<LaunchRetroArchGameCommandHandler> logger)
    {
        _retroArchService = retroArchService;
        _logger = logger;
    }

    public async Task<Result> Handle(LaunchRetroArchGameCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Launching RetroArch game: {GamePath}", request.GamePath);
        return await _retroArchService.LaunchGameAsync(request.GamePath, request.CorePath, cancellationToken);
    }
}

public class ImportRetroArchGamesCommandHandler : IRequestHandler<ImportRetroArchGamesCommand, Result<int>>
{
    private readonly IRetroArchService _retroArchService;
    private readonly IGameRepository _gameRepository;
    private readonly ILogger<ImportRetroArchGamesCommandHandler> _logger;

    public ImportRetroArchGamesCommandHandler(
        IRetroArchService retroArchService,
        IGameRepository gameRepository,
        ILogger<ImportRetroArchGamesCommandHandler> logger)
    {
        _retroArchService = retroArchService;
        _gameRepository = gameRepository;
        _logger = logger;
    }

    public async Task<Result<int>> Handle(ImportRetroArchGamesCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Importing RetroArch games into library");

            var gamesResult = await _retroArchService.GetGamesAsync(cancellationToken);
            if (gamesResult.IsFailure || gamesResult.Value == null)
            {
                return Result.Failure<int>(gamesResult.Error ?? "Failed to get RetroArch games");
            }

            var retroArchGames = gamesResult.Value;
            var importedCount = 0;

            foreach (var retroGame in retroArchGames)
            {
                try
                {
                    // Check if game already exists
                    var existingGames = await _gameRepository.GetAllAsync(cancellationToken);
                    if (existingGames.Any(g => g.Title == retroGame.Label))
                    {
                        _logger.LogDebug("Game already exists: {Title}", retroGame.Label);
                        continue;
                    }

                    // Create new game entry
                    var game = Game.Create(
                        title: retroGame.Label,
                        description: $"RetroArch game - Core: {retroGame.CoreName}",
                        source: "RetroArch",
                        sourceId: retroGame.Crc32
                    );

                    // Set install path and platform
                    game.SetInstallPath(retroGame.Path);
                    // Note: Platform would need to be set via repository if we have the platform entity

                    await _gameRepository.AddAsync(game, cancellationToken);
                    importedCount++;

                    _logger.LogDebug("Imported RetroArch game: {Title}", retroGame.Label);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to import game: {Label}", retroGame.Label);
                }
            }

            _logger.LogInformation("Imported {Count} RetroArch games out of {Total}", importedCount, retroArchGames.Count);
            return Result.Success(importedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing RetroArch games");
            return Result.Failure<int>($"Error importing games: {ex.Message}");
        }
    }

    private static PlatformName DeterminePlatform(string coreName)
    {
        // Map core names to platforms
        return coreName.ToLowerInvariant() switch
        {
            var c when c.Contains("snes") => PlatformName.From("Super Nintendo"),
            var c when c.Contains("genesis") || c.Contains("megadrive") => PlatformName.From("Sega Genesis"),
            var c when c.Contains("gba") || c.Contains("mgba") => PlatformName.From("Game Boy Advance"),
            var c when c.Contains("n64") || c.Contains("mupen") => PlatformName.From("Nintendo 64"),
            var c when c.Contains("psx") || c.Contains("pcsx") => PlatformName.From("PlayStation"),
            var c when c.Contains("gamecube") || c.Contains("dolphin") => PlatformName.From("GameCube"),
            var c when c.Contains("psp") || c.Contains("ppsspp") => PlatformName.From("PSP"),
            var c when c.Contains("nes") || c.Contains("nestopia") => PlatformName.From("NES"),
            _ => PlatformName.From("RetroArch")
        };
    }
}

