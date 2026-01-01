using SaveState.Core.Common;
using SaveState.Core.SaveStates.Services;
using SaveState.Core.SaveStates.Entities;
using SaveState.Core.SaveStates;
using SaveState.Core.GameLibrary.Services;
using SaveStateEntity = SaveState.Core.SaveStates.Entities.SaveState;

namespace SaveState.Infrastructure.SaveStates;

public class AutoSaveManager : IAutoSaveManager
{
    private readonly ISaveStateManager _saveStateManager;
    private readonly ISessionTrackingService _sessionTrackingService;
    private readonly IGameMemoryReader _gameMemoryReader;
    private readonly Dictionary<Guid, AutoSaveConfig> _activeConfigs = new();
    private readonly Dictionary<Guid, Timer> _timers = new();
    private readonly Dictionary<Guid, GameStateType> _lastKnownStates = new();

    public AutoSaveManager(
        ISaveStateManager saveStateManager,
        ISessionTrackingService sessionTrackingService,
        IGameMemoryReader gameMemoryReader)
    {
        _saveStateManager = saveStateManager;
        _sessionTrackingService = sessionTrackingService;
        _gameMemoryReader = gameMemoryReader;

        // Subscribe to game state changes for intelligent auto-saving
        _gameMemoryReader.StateChanged += OnGameStateChanged;
    }

    public async Task<Result> ConfigureAutoSaveAsync(Guid gameId, AutoSaveConfig config, CancellationToken ct = default)
    {
        try
        {
            _activeConfigs[gameId] = config;

            if (config.Enabled)
            {
                await EnableAutoSaveAsync(gameId, ct);
            }
            else
            {
                await DisableAutoSaveAsync(gameId, ct);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to configure auto-save: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> TriggerSaveAsync(Guid gameId, SaveTrigger trigger, CancellationToken ct = default)
    {
        try
        {
            if (!_activeConfigs.TryGetValue(gameId, out var config) || !config.Enabled)
            {
                return Result.Success(); // Non-generic Result - Not configured or disabled, nothing to do
            }

            if (!config.EnabledTriggers.Contains(trigger))
            {
                return Result.Success(); // Non-generic Result - Trigger not enabled
            }

            // Check if we need to clean up old auto-saves
            var existingSaves = await _saveStateManager.GetSaveStatesAsync(gameId, ct);
            var autoSaves = existingSaves.Value.Where(s => s.IsAutoSave).ToList();

            if (autoSaves.Count >= config.MaxAutoSaves)
            {
                // Remove oldest auto-saves
                var savesToDelete = autoSaves
                    .OrderBy(s => s.CreatedAt)
                    .Take(autoSaves.Count - config.MaxAutoSaves + 1);

                foreach (var saveToDelete in savesToDelete)
                {
                    await _saveStateManager.DeleteSaveStateAsync(saveToDelete.Id, ct);
                }
            }

            // Create the auto-save
            var request = new CreateSaveStateRequest(
                Description: GetTriggerDescription(trigger),
                CaptureScreenshot: true);

            var result = await _saveStateManager.CreateSaveStateAsync(gameId, request, ct);
            if (result.IsFailure)
            {
                return Result.Failure($"Failed to create auto-save: {result.Error}", ErrorType.Internal);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to trigger auto-save: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> EnableAutoSaveAsync(Guid gameId, CancellationToken ct = default)
    {
        try
        {
            if (!_activeConfigs.TryGetValue(gameId, out var config))
            {
                return Result.Failure("Auto-save not configured for this game", ErrorType.Validation);
            }

            // Disable any existing timer
            await DisableAutoSaveAsync(gameId, ct);

            // Start timer for time-interval triggers
            if (config.EnabledTriggers.Contains(SaveTrigger.TimeInterval))
            {
                var timer = new Timer(
                    async _ => await TriggerSaveAsync(gameId, SaveTrigger.TimeInterval, CancellationToken.None),
                    null,
                    config.Interval,
                    config.Interval);

                _timers[gameId] = timer;
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to enable auto-save: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> DisableAutoSaveAsync(Guid gameId, CancellationToken ct = default)
    {
        try
        {
            if (_timers.TryGetValue(gameId, out var timer))
            {
                await timer.DisposeAsync();
                _timers.Remove(gameId);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to disable auto-save: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<AutoSaveStatus>> GetAutoSaveStatusAsync(Guid gameId, CancellationToken ct = default)
    {
        try
        {
            var config = _activeConfigs.GetValueOrDefault(gameId);
            var existingSaves = await _saveStateManager.GetSaveStatesAsync(gameId, ct);
            var autoSaves = existingSaves.Value.Where(s => s.IsAutoSave).ToList();
            var lastAutoSave = autoSaves.MaxBy(s => s.CreatedAt)?.CreatedAt;

            TimeSpan? timeUntilNextSave = null;
            if (_timers.TryGetValue(gameId, out var timer) && config.Enabled)
            {
                // This is a simplified calculation - in reality you'd need to track when the timer was started
                timeUntilNextSave = config.Interval;
            }

            var status = new AutoSaveStatus(
                config.Enabled,
                lastAutoSave,
                autoSaves.Count,
                timeUntilNextSave ?? TimeSpan.Zero);

            return Result<AutoSaveStatus>.Success(status);
        }
        catch (Exception ex)
        {
            return Result<AutoSaveStatus>.Failure($"Failed to get auto-save status: {ex.Message}", ErrorType.Internal);
        }
    }

    private void OnGameStateChanged(object? sender, GameStateChangedEventArgs e)
    {
        // Extract game ID from the event data or find which game this relates to
        // This is a simplified implementation - in practice, you'd need to track which game is attached

        // For now, we'll trigger significant progress saves when we detect certain state changes
        if (e.StateType == GameStateType.LevelComplete || e.StateType == GameStateType.BossFight)
        {
            // Try to trigger auto-save for any game that has significant progress trigger enabled
            foreach (var (gameId, config) in _activeConfigs)
            {
                if (config.Enabled && config.EnabledTriggers.Contains(SaveTrigger.SignificantProgress))
                {
                    // Fire and forget - we don't want to block the event handler
                    _ = Task.Run(() => TriggerSaveAsync(gameId, SaveTrigger.SignificantProgress, CancellationToken.None));
                }
            }
        }
    }

    private static string GetTriggerDescription(SaveTrigger trigger)
    {
        return trigger switch
        {
            SaveTrigger.TimeInterval => "Time-based auto-save",
            SaveTrigger.SessionStart => "Session start auto-save",
            SaveTrigger.SessionEnd => "Session end auto-save",
            SaveTrigger.ManualRequest => "Manual auto-save request",
            SaveTrigger.SignificantProgress => "Significant progress auto-save",
            _ => "Auto-save"
        };
    }
}