using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.Sync.Services;
using SaveState.Core.Sync.Services.DTOs;

namespace SaveState.Application.Sync.Commands.Handlers;

/// <summary>
/// Handler for starting cloud gaming sessions.
/// Manages cloud gaming infrastructure and session initialization.
/// </summary>
public class StartCloudSessionCommandHandler :
    IRequestHandler<StartCloudSessionCommand, Result<CloudSession>>
{
    private readonly ICloudGamingManager _cloudGamingManager;
    private readonly IGameRepository _gameRepository;
    private readonly ILogger<StartCloudSessionCommandHandler> _logger;

    public StartCloudSessionCommandHandler(
        ICloudGamingManager cloudGamingManager,
        IGameRepository gameRepository,
        ILogger<StartCloudSessionCommandHandler> logger)
    {
        _cloudGamingManager = cloudGamingManager;
        _gameRepository = gameRepository;
        _logger = logger;
    }

    public async Task<Result<CloudSession>> Handle(StartCloudSessionCommand request, CancellationToken ct)
    {
        try
        {
            // Verify game exists
            var game = await _gameRepository.GetByIdAsync(GameId.From(request.GameId), ct)
                .ConfigureAwait(false);

            if (game == null)
            {
                return Result<CloudSession>.Failure($"Game with ID {request.GameId} not found");
            }

            var result = await _cloudGamingManager.StartSessionAsync(
                request.GameId, request.Provider, ct).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Started cloud gaming session for game {GameId} ({GameTitle}) on {Provider}",
                    request.GameId, game.Title, request.Provider);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start cloud session for game {GameId} on {Provider}",
                request.GameId, request.Provider);
            return Result<CloudSession>.Failure($"Failed to start cloud session: {ex.Message}");
        }
    }
}

public class EndCloudSessionCommandHandler :
    IRequestHandler<EndCloudSessionCommand, Result>
{
    private readonly ICloudGamingManager _cloudGamingManager;
    private readonly ILogger<EndCloudSessionCommandHandler> _logger;

    public EndCloudSessionCommandHandler(
        ICloudGamingManager cloudGamingManager,
        ILogger<EndCloudSessionCommandHandler> logger)
    {
        _cloudGamingManager = cloudGamingManager;
        _logger = logger;
    }

    public async Task<Result> Handle(EndCloudSessionCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _cloudGamingManager.EndSessionAsync(request.SessionId, ct)
                .ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Ended cloud gaming session {SessionId}", request.SessionId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to end cloud session {SessionId}", request.SessionId);
            return Result.Failure($"Failed to end cloud session: {ex.Message}");
        }
    }
}
