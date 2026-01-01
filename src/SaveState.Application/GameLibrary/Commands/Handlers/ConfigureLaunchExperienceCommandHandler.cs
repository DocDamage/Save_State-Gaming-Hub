using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.GameLibrary.Services.DTOs;

namespace SaveState.Application.GameLibrary.Commands.Handlers;

public class ConfigureLaunchExperienceCommandHandler :
    IRequestHandler<ConfigureLaunchExperienceCommand, Result>
{
    private readonly ILaunchExperienceManager _launchExperienceManager;
    private readonly IGameRepository _gameRepository;
    private readonly ILogger<ConfigureLaunchExperienceCommandHandler> _logger;

    public ConfigureLaunchExperienceCommandHandler(
        ILaunchExperienceManager launchExperienceManager,
        IGameRepository gameRepository,
        ILogger<ConfigureLaunchExperienceCommandHandler> logger)
    {
        _launchExperienceManager = launchExperienceManager;
        _gameRepository = gameRepository;
        _logger = logger;
    }

    public async Task<Result> Handle(ConfigureLaunchExperienceCommand request, CancellationToken ct)
    {
        try
        {
            // Verify game exists
            var game = await _gameRepository.GetByIdAsync(GameId.From(request.GameId), ct)
                .ConfigureAwait(false);

            if (game == null)
            {
                return Result.Failure($"Game with ID {request.GameId} not found");
            }

            var result = await _launchExperienceManager.ConfigureLaunchExperienceAsync(
                request.GameId,
                request.Config,
                ct).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Configured launch experience for game {GameId} ({GameTitle})",
                    request.GameId, game.Title);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure launch experience for game {GameId}", request.GameId);
            return Result.Failure($"Failed to configure launch experience: {ex.Message}");
        }
    }
}

public class ResetLaunchExperienceCommandHandler :
    IRequestHandler<ResetLaunchExperienceCommand, Result>
{
    private readonly ILaunchExperienceManager _launchExperienceManager;
    private readonly IGameRepository _gameRepository;
    private readonly ILogger<ResetLaunchExperienceCommandHandler> _logger;

    public ResetLaunchExperienceCommandHandler(
        ILaunchExperienceManager launchExperienceManager,
        IGameRepository gameRepository,
        ILogger<ResetLaunchExperienceCommandHandler> logger)
    {
        _launchExperienceManager = launchExperienceManager;
        _gameRepository = gameRepository;
        _logger = logger;
    }

    public async Task<Result> Handle(ResetLaunchExperienceCommand request, CancellationToken ct)
    {
        try
        {
            // Verify game exists
            var game = await _gameRepository.GetByIdAsync(GameId.From(request.GameId), ct)
                .ConfigureAwait(false);

            if (game == null)
            {
                return Result.Failure($"Game with ID {request.GameId} not found");
            }

            var result = await _launchExperienceManager.ResetLaunchExperienceConfigAsync(
                request.GameId, ct).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Reset launch experience config for game {GameId} ({GameTitle})",
                    request.GameId, game.Title);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset launch experience config for game {GameId}", request.GameId);
            return Result.Failure($"Failed to reset launch experience config: {ex.Message}");
        }
    }
}

public class GenerateLaunchSequenceCommandHandler :
    IRequestHandler<GenerateLaunchSequenceCommand, Result<Core.GameLibrary.Services.DTOs.LaunchSequence>>
{
    private readonly ILaunchExperienceManager _launchExperienceManager;
    private readonly IGameRepository _gameRepository;
    private readonly ILogger<GenerateLaunchSequenceCommandHandler> _logger;

    public GenerateLaunchSequenceCommandHandler(
        ILaunchExperienceManager launchExperienceManager,
        IGameRepository gameRepository,
        ILogger<GenerateLaunchSequenceCommandHandler> logger)
    {
        _launchExperienceManager = launchExperienceManager;
        _gameRepository = gameRepository;
        _logger = logger;
    }

    public async Task<Result<Core.GameLibrary.Services.DTOs.LaunchSequence>> Handle(
        GenerateLaunchSequenceCommand request,
        CancellationToken ct)
    {
        try
        {
            // Verify game exists
            var game = await _gameRepository.GetByIdAsync(GameId.From(request.GameId), ct)
                .ConfigureAwait(false);

            if (game == null)
            {
                return Result<Core.GameLibrary.Services.DTOs.LaunchSequence>.Failure(
                    $"Game with ID {request.GameId} not found");
            }

            var result = await _launchExperienceManager.GenerateLaunchSequenceAsync(
                request.GameId, ct).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Generated launch sequence for game {GameId} ({GameTitle})",
                    request.GameId, game.Title);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate launch sequence for game {GameId}", request.GameId);
            return Result<LaunchSequence>.Failure(
                $"Failed to generate launch sequence: {ex.Message}");
        }
    }
}

public class ExecuteLaunchSequenceCommandHandler :
    IRequestHandler<ExecuteLaunchSequenceCommand, Result>
{
    private readonly ILaunchExperienceManager _launchExperienceManager;
    private readonly ILogger<ExecuteLaunchSequenceCommandHandler> _logger;

    public ExecuteLaunchSequenceCommandHandler(
        ILaunchExperienceManager launchExperienceManager,
        ILogger<ExecuteLaunchSequenceCommandHandler> logger)
    {
        _launchExperienceManager = launchExperienceManager;
        _logger = logger;
    }

    public async Task<Result> Handle(ExecuteLaunchSequenceCommand request, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Executing launch sequence for game {GameId}", request.Sequence.GameId);

            await _launchExperienceManager.ExecuteLaunchSequenceAsync(
                request.Sequence, ct).ConfigureAwait(false);

            _logger.LogInformation("Completed launch sequence execution for game {GameId}",
                request.Sequence.GameId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute launch sequence for game {GameId}", request.Sequence.GameId);
            return Result.Failure($"Failed to execute launch sequence: {ex.Message}");
        }
    }
}