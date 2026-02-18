// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary;
using SaveState.Core.Input.Services;
using SaveState.Core.Input.Services.DTOs;
using SaveState.Core.SmartLauncher;

namespace SaveState.Application.SmartLauncher.VoiceCommands;

/// <summary>
/// Handles voice commands for the Smart Launcher feature.
/// </summary>
public sealed class SmartLauncherVoiceCommandHandler
{
    private readonly ISmartLauncherService _launcherService;
    private readonly IGameRepository _gameRepository;
    private readonly ILogger<SmartLauncherVoiceCommandHandler> _logger;

    public SmartLauncherVoiceCommandHandler(
        ISmartLauncherService launcherService,
        IGameRepository gameRepository,
        ILogger<SmartLauncherVoiceCommandHandler> logger)
    {
        _launcherService = launcherService ?? throw new ArgumentNullException(nameof(launcherService));
        _gameRepository = gameRepository ?? throw new ArgumentNullException(nameof(gameRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers all Smart Launcher voice commands.
    /// </summary>
    public async Task RegisterCommandsAsync(IVoiceCommandService voiceCommandService, CancellationToken ct = default)
    {
        // Note: The voice command system uses a predefined set of actions
        // Smart Launcher commands would be handled by the existing LaunchGame and CloseGame actions
        // with game-specific parameters
        
        _logger.LogInformation("Smart Launcher voice commands are available through standard voice command actions");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Handles the LaunchGame voice command.
    /// </summary>
    public async Task<Result> HandleLaunchGameAsync(string gameName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(gameName))
        {
            return Result.Failure("Please specify a game name");
        }

        try
        {
            // Search for the game
            var games = await _gameRepository.GetAllAsync(ct);
            var game = games.FirstOrDefault(g => 
                g.Title.Contains(gameName, StringComparison.OrdinalIgnoreCase));

            if (game == null)
            {
                return Result.Failure($"Game '{gameName}' not found");
            }

            // Launch the game
            var result = await _launcherService.LaunchGameAsync(game.Id, ct: ct);
            
            if (result.Success)
            {
                return Result.Success();
            }
            else
            {
                return Result.Failure(result.ErrorMessage ?? "Failed to launch game");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling launch game voice command");
            return Result.Failure($"Error launching game: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles the CloseGame voice command.
    /// </summary>
    public async Task<Result> HandleCloseGameAsync(CancellationToken ct = default)
    {
        try
        {
            var activeSessionResult = await _launcherService.GetActiveSessionAsync(ct);
            if (!activeSessionResult.IsSuccess)
            {
                return Result.Failure("No game is currently running");
            }

            var result = await _launcherService.EndSessionAsync(activeSessionResult.Value.Id, ct);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling close game voice command");
            return Result.Failure($"Error stopping game: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the list of launchable games for voice command suggestions.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetLaunchableGameNamesAsync(CancellationToken ct = default)
    {
        try
        {
            var games = await _gameRepository.GetAllAsync(ct);
            return games.Select(g => g.Title).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting launchable game names");
            return new List<string>();
        }
    }

    /// <summary>
    /// Gets the currently running game name for voice feedback.
    /// </summary>
    public async Task<string?> GetActiveGameNameAsync(CancellationToken ct = default)
    {
        try
        {
            var sessionResult = await _launcherService.GetActiveSessionAsync(ct);
            return sessionResult.IsSuccess ? sessionResult.Value.GameName : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active game name");
            return null;
        }
    }
}
