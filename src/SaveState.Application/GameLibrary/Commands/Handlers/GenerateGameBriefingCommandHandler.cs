using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.GameLibrary.Services.DTOs;

namespace SaveState.Application.GameLibrary.Commands.Handlers;

public class GenerateGameBriefingCommandHandler :
    IRequestHandler<GenerateGameBriefingCommand, Result<GameBriefing>>
{
    private readonly IGameBriefingService _gameBriefingService;
    private readonly IGameRepository _gameRepository;
    private readonly ILogger<GenerateGameBriefingCommandHandler> _logger;

    public GenerateGameBriefingCommandHandler(
        IGameBriefingService gameBriefingService,
        IGameRepository gameRepository,
        ILogger<GenerateGameBriefingCommandHandler> logger)
    {
        _gameBriefingService = gameBriefingService;
        _gameRepository = gameRepository;
        _logger = logger;
    }

    public async Task<Result<GameBriefing>> Handle(GenerateGameBriefingCommand request, CancellationToken ct)
    {
        try
        {
            // Verify game exists
            var game = await _gameRepository.GetByIdAsync(GameId.From(request.GameId), ct)
                .ConfigureAwait(false);

            if (game == null)
            {
                return Result.Failure<GameBriefing>($"Game with ID {request.GameId} not found");
            }

            var result = await _gameBriefingService.GenerateBriefingAsync(
                request.GameId, ct).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Generated briefing for game {GameId} ({GameTitle})",
                    request.GameId, game.Title);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate briefing for game {GameId}", request.GameId);
            return Result.Failure<GameBriefing>($"Failed to generate briefing: {ex.Message}");
        }
    }
}

public class GenerateLastSessionSummaryCommandHandler :
    IRequestHandler<GenerateLastSessionSummaryCommand, Result<string>>
{
    private readonly IGameBriefingService _gameBriefingService;
    private readonly IGameRepository _gameRepository;
    private readonly ILogger<GenerateLastSessionSummaryCommandHandler> _logger;

    public GenerateLastSessionSummaryCommandHandler(
        IGameBriefingService gameBriefingService,
        IGameRepository gameRepository,
        ILogger<GenerateLastSessionSummaryCommandHandler> logger)
    {
        _gameBriefingService = gameBriefingService;
        _gameRepository = gameRepository;
        _logger = logger;
    }

    public async Task<Result<string>> Handle(GenerateLastSessionSummaryCommand request, CancellationToken ct)
    {
        try
        {
            // Verify game exists
            var game = await _gameRepository.GetByIdAsync(GameId.From(request.GameId), ct)
                .ConfigureAwait(false);

            if (game == null)
            {
                return Result.Failure<string>($"Game with ID {request.GameId} not found");
            }

            var result = await _gameBriefingService.GenerateLastSessionSummaryAsync(
                request.GameId, ct).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Generated last session summary for game {GameId} ({GameTitle})",
                    request.GameId, game.Title);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate last session summary for game {GameId}", request.GameId);
            return Result.Failure<string>($"Failed to generate last session summary: {ex.Message}");
        }
    }
}

public class GetCurrentObjectivesCommandHandler :
    IRequestHandler<GetCurrentObjectivesCommand, Result<IReadOnlyList<string>>>
{
    private readonly IGameBriefingService _gameBriefingService;
    private readonly IGameRepository _gameRepository;
    private readonly ILogger<GetCurrentObjectivesCommandHandler> _logger;

    public GetCurrentObjectivesCommandHandler(
        IGameBriefingService gameBriefingService,
        IGameRepository gameRepository,
        ILogger<GetCurrentObjectivesCommandHandler> logger)
    {
        _gameBriefingService = gameBriefingService;
        _gameRepository = gameRepository;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<string>>> Handle(GetCurrentObjectivesCommand request, CancellationToken ct)
    {
        try
        {
            // Verify game exists
            var game = await _gameRepository.GetByIdAsync(GameId.From(request.GameId), ct)
                .ConfigureAwait(false);

            if (game == null)
            {
                return Result.Failure<IReadOnlyList<string>>($"Game with ID {request.GameId} not found");
            }

            var result = await _gameBriefingService.GetCurrentObjectivesAsync(
                request.GameId, ct).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Retrieved current objectives for game {GameId} ({GameTitle})",
                    request.GameId, game.Title);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current objectives for game {GameId}", request.GameId);
            return Result.Failure<IReadOnlyList<string>>($"Failed to get current objectives: {ex.Message}");
        }
    }
}

public class GetGameTipsCommandHandler :
    IRequestHandler<GetGameTipsCommand, Result<IReadOnlyList<string>>>
{
    private readonly IGameBriefingService _gameBriefingService;
    private readonly IGameRepository _gameRepository;
    private readonly ILogger<GetGameTipsCommandHandler> _logger;

    public GetGameTipsCommandHandler(
        IGameBriefingService gameBriefingService,
        IGameRepository gameRepository,
        ILogger<GetGameTipsCommandHandler> logger)
    {
        _gameBriefingService = gameBriefingService;
        _gameRepository = gameRepository;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<string>>> Handle(GetGameTipsCommand request, CancellationToken ct)
    {
        try
        {
            // Verify game exists
            var game = await _gameRepository.GetByIdAsync(GameId.From(request.GameId), ct)
                .ConfigureAwait(false);

            if (game == null)
            {
                return Result.Failure<IReadOnlyList<string>>($"Game with ID {request.GameId} not found");
            }

            var result = await _gameBriefingService.GetGameTipsAsync(
                request.GameId, ct).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Retrieved game tips for game {GameId} ({GameTitle})",
                    request.GameId, game.Title);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get game tips for game {GameId}", request.GameId);
            return Result.Failure<IReadOnlyList<string>>($"Failed to get game tips: {ex.Message}");
        }
    }
}

public class GenerateQuickBriefingCommandHandler :
    IRequestHandler<GenerateQuickBriefingCommand, Result<GameBriefing>>
{
    private readonly IGameBriefingService _gameBriefingService;
    private readonly IGameRepository _gameRepository;
    private readonly ILogger<GenerateQuickBriefingCommandHandler> _logger;

    public GenerateQuickBriefingCommandHandler(
        IGameBriefingService gameBriefingService,
        IGameRepository gameRepository,
        ILogger<GenerateQuickBriefingCommandHandler> logger)
    {
        _gameBriefingService = gameBriefingService;
        _gameRepository = gameRepository;
        _logger = logger;
    }

    public async Task<Result<GameBriefing>> Handle(GenerateQuickBriefingCommand request, CancellationToken ct)
    {
        try
        {
            // Verify game exists
            var game = await _gameRepository.GetByIdAsync(GameId.From(request.GameId), ct)
                .ConfigureAwait(false);

            if (game == null)
            {
                return Result.Failure<GameBriefing>($"Game with ID {request.GameId} not found");
            }

            var result = await _gameBriefingService.GenerateQuickBriefingAsync(
                request.GameId, ct).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Generated quick briefing for game {GameId} ({GameTitle})",
                    request.GameId, game.Title);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate quick briefing for game {GameId}", request.GameId);
            return Result.Failure<GameBriefing>($"Failed to generate quick briefing: {ex.Message}");
        }
    }
}
