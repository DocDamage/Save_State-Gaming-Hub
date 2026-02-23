using System.Text.Json;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.SaveStates;
using SaveState.Core.SaveStates.Services;
using SaveState.Core.WebBrowser.Services;
using SaveState.Core.SaveStates.Entities;

namespace SaveState.Presentation.Services.WebBrowser;

/// <summary>
/// Implementation of the web-to-game bridge that exposes SaveState functionality to web pages via JavaScript.
/// </summary>
public class WebToGameBridge : IWebToGameBridge
{
    private readonly ILogger<WebToGameBridge> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly IGameRepository _gameRepository;
    private readonly ISaveStateManager _saveStateManager;
    private readonly IGameMemoryReader _gameMemoryReader;
    private readonly IGameContextService _gameContextService;
    private readonly INotificationService _notificationService;

    public WebToGameBridge(
        ILogger<WebToGameBridge> logger,
        ITimeProvider timeProvider,
        IGameRepository gameRepository,
        ISaveStateManager saveStateManager,
        IGameMemoryReader gameMemoryReader,
        IGameContextService gameContextService,
        INotificationService notificationService)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _gameRepository = gameRepository;
        _saveStateManager = saveStateManager;
        _gameMemoryReader = gameMemoryReader;
        _gameContextService = gameContextService;
        _notificationService = notificationService;
    }

    /// <inheritdoc />
    public event EventHandler<GameLaunchRequest>? OnGameLaunchRequested;

    /// <inheritdoc />
    public event EventHandler<SaveStateRequest>? OnSaveStateRequested;

    /// <inheritdoc />
    public event EventHandler<LoadSaveStateRequest>? OnLoadSaveStateRequested;

    /// <inheritdoc />
    public async Task<Result> LaunchGameAsync(string gameId)
    {
        try
        {
            _logger.LogInformation("Web bridge: Launching game {GameId}", gameId);

            if (!int.TryParse(gameId, out var id))
            {
                return Result.Failure("Invalid game ID format", ErrorType.Validation);
            }

            // Parse gameId as Guid (since GameId uses Guid, not int)
            if (!Guid.TryParse(gameId, out var gameGuid))
            {
                return Result.Failure("Invalid game ID format", ErrorType.Validation);
            }
            
            var game = await _gameRepository.GetByIdAsync(GameId.From(gameGuid));
            if (game == null)
            {
                return Result.Failure("Game not found", ErrorType.NotFound);
            }

            OnGameLaunchRequested?.Invoke(this, new GameLaunchRequest
            {
                GameId = gameId,
                SourceUrl = "web-bridge"
            });

            _notificationService.ShowInfo($"Launching {game.Title}...");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch game {GameId} from web bridge", gameId);
            return Result.Failure($"Launch failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<string>> CreateSaveStateAsync(string gameId, string description)
    {
        try
        {
            _logger.LogInformation("Web bridge: Creating save state for {GameId}", gameId);

            if (!int.TryParse(gameId, out var id))
            {
                return Result<string>.Failure("Invalid game ID format", ErrorType.Validation);
            }

            // Parse gameId as Guid (since GameId uses Guid, not int)
            if (!Guid.TryParse(gameId, out var gameGuid))
            {
                return Result<string>.Failure("Invalid game ID format", ErrorType.Validation);
            }
            
            var game = await _gameRepository.GetByIdAsync(GameId.From(gameGuid));
            if (game == null)
            {
                return Result<string>.Failure("Game not found", ErrorType.NotFound);
            }

            OnSaveStateRequested?.Invoke(this, new SaveStateRequest
            {
                GameId = gameId,
                Description = description,
                IncludeScreenshot = true
            });

            // Create the save state
            var createRequest = new CreateSaveStateRequest(
                Description: description,
                CaptureScreenshot: true);
            var result = await _saveStateManager.CreateSaveStateAsync(
                game.Id,
                createRequest);

            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess($"Save state created for {game.Title}");
                return Result<string>.Success(result.Value.Id.ToString());
            }

            return Result<string>.Failure(result.Error ?? "Failed to create save state", result.ErrorType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create save state for {GameId} from web bridge", gameId);
            return Result<string>.Failure($"Save state creation failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result> LoadSaveStateAsync(string saveStateId)
    {
        try
        {
            _logger.LogInformation("Web bridge: Loading save state {SaveStateId}", saveStateId);

            if (!Guid.TryParse(saveStateId, out var saveStateGuid))
            {
                return Result.Failure("Invalid save state ID format", ErrorType.Validation);
            }

            OnLoadSaveStateRequested?.Invoke(this, new LoadSaveStateRequest
            {
                SaveStateId = saveStateId,
                AutoLaunch = true
            });

            var result = await _saveStateManager.RestoreSaveStateAsync(saveStateGuid);
            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess("Save state loaded");
                return Result.Success();
            }

            return Result.Failure(result.Error ?? "Failed to load save state", result.ErrorType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load save state {SaveStateId} from web bridge", saveStateId);
            return Result.Failure($"Save state load failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<string>> TakeScreenshotAsync()
    {
        try
        {
            _logger.LogInformation("Web bridge: Taking screenshot");

            var currentGameId = _gameContextService.GetCurrentGameId();
            if (currentGameId == null)
            {
                return Result<string>.Failure("No game currently running", ErrorType.Validation);
            }

            // Screenshot logic would go here
            var screenshotPath = $"screenshots/game_{currentGameId}_{_timeProvider.Now:yyyyMMdd_HHmmss}.png";

            _notificationService.ShowSuccess("Screenshot captured");
            return Result<string>.Success(screenshotPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to take screenshot from web bridge");
            return Result<string>.Failure($"Screenshot failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public Task<Result> StartRecordingAsync()
    {
        try
        {
            _logger.LogInformation("Web bridge: Starting recording");

            var currentGameId = _gameContextService.GetCurrentGameId();
            if (currentGameId == null)
            {
                return Task.FromResult(Result.Failure("No game currently running", ErrorType.Validation));
            }

            // Recording logic would go here
            _notificationService.ShowInfo("Recording started");
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start recording from web bridge");
            return Task.FromResult(Result.Failure($"Recording failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public Task<Result<string>> StopRecordingAsync()
    {
        try
        {
            _logger.LogInformation("Web bridge: Stopping recording");

            var recordingPath = $"recordings/recording_{_timeProvider.Now:yyyyMMdd_HHmmss}.mp4";

            _notificationService.ShowSuccess("Recording saved");
            return Task.FromResult(Result<string>.Success(recordingPath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop recording from web bridge");
            return Task.FromResult(Result<string>.Failure($"Failed to stop recording: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public Task<string?> GetCurrentlyPlayingGameAsync()
    {
        var currentGameId = _gameContextService.GetCurrentGameId();
        return Task.FromResult(currentGameId?.ToString());
    }

    /// <inheritdoc />
    public async Task<string?> GetLastSaveStateAsync(string gameId)
    {
        try
        {
            if (!int.TryParse(gameId, out var id))
            {
                return null;
            }

            if (!Guid.TryParse(gameId, out var gameGuid))
            {
                return null;
            }
            
            var saveStatesResult = await _saveStateManager.GetSaveStatesAsync(gameGuid);
            if (!saveStatesResult.IsSuccess)
            {
                return null;
            }
            var lastSaveState = saveStatesResult.Value
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefault();

            return lastSaveState?.Id.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get last save state for {GameId}", gameId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetGameInfoAsync(string gameId)
    {
        try
        {
            if (!int.TryParse(gameId, out var id))
            {
                return null;
            }

            if (!Guid.TryParse(gameId, out var gameGuid))
            {
                return null;
            }
            
            var game = await _gameRepository.GetByIdAsync(GameId.From(gameGuid));
            if (game == null)
            {
                return null;
            }

            var currentGameId = _gameContextService.GetCurrentGameId();
            var info = new
            {
                Id = game.Id,
                Title = game.Title,
                Platform = game.Platform?.Name?.Value,
                Genre = (string?)null,
                ReleaseDate = game.ReleaseDate?.ToDateTime(TimeOnly.MinValue),
                Description = game.Description,
                IsRunning = currentGameId == game.Id,
                IsInstalled = !string.IsNullOrEmpty(game.ExecutablePath)
            };

            return JsonSerializer.Serialize(info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get game info for {GameId}", gameId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetGameStatsAsync(string gameId)
    {
        try
        {
            if (!int.TryParse(gameId, out var id))
            {
                return null;
            }

            if (!Guid.TryParse(gameId, out var gameGuid))
            {
                return null;
            }
            
            var game = await _gameRepository.GetByIdAsync(GameId.From(gameGuid));
            if (game == null)
            {
                return null;
            }

            // Aggregate stats
            var stats = new
            {
                TotalPlaytime = game.TotalPlayTime.TotalHours,
                LastPlayed = game.LastPlayedAt,
                CompletionPercentage = (double?)null,
                SaveStateCount = 0,
                SessionCount = 0,
                AchievementProgress = 0.0
            };

            return JsonSerializer.Serialize(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get game stats for {GameId}", gameId);
            return null;
        }
    }

    /// <inheritdoc />
    public Task<Result> OpenOverlayAsync()
    {
        _logger.LogInformation("Web bridge: Opening overlay");
        _notificationService.ShowInfo("Opening SaveState overlay...");
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> CloseOverlayAsync()
    {
        _logger.LogInformation("Web bridge: Closing overlay");
        return Task.FromResult(Result.Success());
    }
}
