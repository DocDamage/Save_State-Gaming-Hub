using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Application.Common.Options;
using SaveState.Core.Common.Interfaces;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.DomainServices;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Application.GameLibrary.Commands.Handlers;

/// <summary>
/// Handler for launching games.
/// Validates game existence and launches game processes with proper monitoring.
/// </summary>
public class LaunchGameCommandHandler : IRequestHandler<LaunchGameCommand, Result<ProcessInfo>>
{
    private readonly IGameRepository _gameRepository;
    private readonly IGameValidationService _validationService;
    private readonly IProcessLauncher _processLauncher;
    private readonly ILogger<LaunchGameCommandHandler> _logger;

    public LaunchGameCommandHandler(
        IGameRepository gameRepository,
        IGameValidationService validationService,
        IProcessLauncher processLauncher,
        ILogger<LaunchGameCommandHandler> logger)
    {
        _gameRepository = gameRepository;
        _validationService = validationService;
        _processLauncher = processLauncher;
        _logger = logger;
    }

    /// <summary>
    /// Handles the command to launch a game.
    /// </summary>
    /// <param name="request">The launch game command with game ID.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the process information or an error.</returns>
    public async Task<Result<ProcessInfo>> Handle(LaunchGameCommand request, CancellationToken ct)
    {
        var game = await _gameRepository.GetByIdAsync(request.GameId, ct).ConfigureAwait(false);
        if (game is null)
            return Result<ProcessInfo>.Failure("Game not found");

        // Validate game can be launched
        if (!await _validationService.CanLaunchGameAsync(game, ct).ConfigureAwait(false))
            return Result<ProcessInfo>.Failure("Game cannot be launched");

        try
        {
            // Get launch configuration
            var launchConfig = GetLaunchConfiguration(game, request.Options);

            // Launch the game
            var processInfo = await _processLauncher.LaunchAsync(launchConfig, ct).ConfigureAwait(false);

            // Record play session
            game.MarkAsRunning();
            await _gameRepository.UpdateAsync(game, ct).ConfigureAwait(false);

            _logger.LogInformation("Launched game {GameId}: {Title}", game.Id, game.Title);

            return Result<ProcessInfo>.Success(processInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch game {GameId}: {Title}", game.Id, game.Title);
            return Result<ProcessInfo>.Failure($"Failed to launch game: {ex.Message}");
        }
    }

    private static LaunchConfiguration GetLaunchConfiguration(Game game, LaunchOptions? options)
    {
        // Find the main executable (this is a simplified version)
        var executablePath = GetMainExecutablePath(game);

        return new LaunchConfiguration
        {
            ExecutablePath = executablePath,
            Arguments = options?.Arguments,
            WorkingDirectory = options?.WorkingDirectory ?? Path.GetDirectoryName(executablePath),
            WaitForExit = options?.WaitForExit ?? false,
            Timeout = options?.Timeout
        };
    }

    private static string GetMainExecutablePath(Game game)
    {
        // This is a simplified implementation - in a real system, this would be more sophisticated
        if (!string.IsNullOrEmpty(game.InstallPath))
        {
            // Look for common executable patterns
            var patterns = new[] { "*.exe", "*.bat", "*.cmd" };

            foreach (var pattern in Directory.GetFiles(game.InstallPath, "*", SearchOption.TopDirectoryOnly))
            {
                var fileName = Path.GetFileName(pattern);
                if (patterns.Any(p => fileName.EndsWith(p.TrimStart('*'))))
                {
                    return pattern;
                }
            }
        }

        // Fallback - this would normally not happen if validation passed
        throw new InvalidOperationException("No executable found for game");
    }
}
