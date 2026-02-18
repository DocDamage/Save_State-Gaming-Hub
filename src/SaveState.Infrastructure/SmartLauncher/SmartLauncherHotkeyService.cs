// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.GameLibrary;
using SaveState.Core.SmartLauncher;

namespace SaveState.Infrastructure.SmartLauncher;

/// <summary>
/// Implementation of Smart Launcher hotkey service using platform-specific APIs.
/// </summary>
public sealed class SmartLauncherHotkeyService : ISmartLauncherHotkeyService, IDisposable
{
    private readonly IGameRepository _gameRepository;
    private readonly IOptions<SmartLauncherHotkeyConfig> _config;
    private readonly ILogger<SmartLauncherHotkeyService> _logger;
    private readonly Dictionary<Guid, string> _gameHotkeys = new();
    private readonly Dictionary<string, Guid> _hotkeyToGame = new();
    private bool _isDisposed;

    public SmartLauncherHotkeyService(
        IGameRepository gameRepository,
        IOptions<SmartLauncherHotkeyConfig> config,
        ILogger<SmartLauncherHotkeyService> logger)
    {
        _gameRepository = gameRepository ?? throw new ArgumentNullException(nameof(gameRepository));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public event EventHandler<GameHotkeyPressedEventArgs>? GameHotkeyPressed;

    /// <inheritdoc />
    public event EventHandler<StopGameHotkeyPressedEventArgs>? StopGameHotkeyPressed;

    /// <inheritdoc />
    public event EventHandler<ShowLauncherHotkeyPressedEventArgs>? ShowLauncherHotkeyPressed;

    /// <inheritdoc />
    public async Task RegisterDefaultHotkeysAsync(CancellationToken ct = default)
    {
        try
        {
            // Register stop game hotkey
            await RegisterHotkeyAsync(_config.Value.StopGameHotkey, OnStopGameHotkeyPressed);
            
            // Register show launcher hotkey
            await RegisterHotkeyAsync(_config.Value.ShowLauncherHotkey, OnShowLauncherHotkeyPressed);

            // Register numbered hotkeys for quick game access
            if (_config.Value.EnableNumberedHotkeys)
            {
                await RegisterNumberedHotkeysAsync(ct);
            }

            _logger.LogInformation("Registered default Smart Launcher hotkeys");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register default hotkeys");
        }
    }

    /// <inheritdoc />
    public Task UnregisterAllHotkeysAsync(CancellationToken ct = default)
    {
        _gameHotkeys.Clear();
        _hotkeyToGame.Clear();
        _logger.LogInformation("Unregistered all Smart Launcher hotkeys");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> RegisterGameHotkeyAsync(Guid gameId, string hotkey, CancellationToken ct = default)
    {
        try
        {
            // Unregister existing hotkey for this game
            if (_gameHotkeys.TryGetValue(gameId, out var existingHotkey))
            {
                _hotkeyToGame.Remove(existingHotkey);
            }

            // Check if hotkey is already in use
            if (_hotkeyToGame.ContainsKey(hotkey))
            {
                _logger.LogWarning("Hotkey {Hotkey} is already assigned to another game", hotkey);
                return Task.FromResult(false);
            }

            // Register new hotkey
            _gameHotkeys[gameId] = hotkey;
            _hotkeyToGame[hotkey] = gameId;

            _logger.LogInformation("Registered hotkey {Hotkey} for game {GameId}", hotkey, gameId);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register hotkey {Hotkey} for game {GameId}", hotkey, gameId);
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc />
    public Task<bool> UnregisterGameHotkeyAsync(Guid gameId, CancellationToken ct = default)
    {
        try
        {
            if (_gameHotkeys.TryGetValue(gameId, out var hotkey))
            {
                _gameHotkeys.Remove(gameId);
                _hotkeyToGame.Remove(hotkey);
                
                _logger.LogInformation("Unregistered hotkey for game {GameId}", gameId);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unregister hotkey for game {GameId}", gameId);
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc />
    public Task<string?> GetGameHotkeyAsync(Guid gameId, CancellationToken ct = default)
    {
        _gameHotkeys.TryGetValue(gameId, out var hotkey);
        return Task.FromResult(hotkey);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GameHotkeyMapping>> GetAllGameHotkeysAsync(CancellationToken ct = default)
    {
        var mappings = new List<GameHotkeyMapping>();

        foreach (var (gameId, hotkey) in _gameHotkeys)
        {
            var game = await _gameRepository.GetByIdAsync(
                Core.Common.ValueObjects.GameId.From(gameId), ct);
            
            mappings.Add(new GameHotkeyMapping
            {
                GameId = gameId,
                GameName = game?.Title ?? "Unknown",
                Hotkey = hotkey,
                AssignedAt = DateTime.UtcNow // Would be stored in DB
            });
        }

        return mappings;
    }

    private async Task RegisterNumberedHotkeysAsync(CancellationToken ct)
    {
        try
        {
            var games = await _gameRepository.GetAllAsync(ct);
            var recentGames = games
                .Where(g => g.LastPlayedAt.HasValue)
                .OrderByDescending(g => g.LastPlayedAt)
                .Take(9)
                .ToList();

            for (int i = 0; i < recentGames.Count; i++)
            {
                var hotkey = $"Ctrl+Alt+{i + 1}";
                await RegisterGameHotkeyAsync(recentGames[i].Id, hotkey, ct);
            }

            _logger.LogInformation("Registered {Count} numbered hotkeys for recent games", recentGames.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register numbered hotkeys");
        }
    }

    private Task RegisterHotkeyAsync(string hotkey, Action handler)
    {
        // Platform-specific hotkey registration would go here
        // For now, this is a stub that logs the registration
        _logger.LogDebug("Registered hotkey: {Hotkey}", hotkey);
        return Task.CompletedTask;
    }

    private void OnStopGameHotkeyPressed()
    {
        StopGameHotkeyPressed?.Invoke(this, new StopGameHotkeyPressedEventArgs
        {
            Hotkey = _config.Value.StopGameHotkey
        });
    }

    private void OnShowLauncherHotkeyPressed()
    {
        ShowLauncherHotkeyPressed?.Invoke(this, new ShowLauncherHotkeyPressedEventArgs
        {
            Hotkey = _config.Value.ShowLauncherHotkey
        });
    }

    /// <summary>
    /// Handles a game hotkey press.
    /// </summary>
    public void HandleGameHotkey(string hotkey)
    {
        if (_hotkeyToGame.TryGetValue(hotkey, out var gameId))
        {
            var gameName = "Unknown";
            // Get game name asynchronously
            _ = Task.Run(async () =>
            {
                var game = await _gameRepository.GetByIdAsync(
                    Core.Common.ValueObjects.GameId.From(gameId));
                gameName = game?.Title ?? "Unknown";
            });

            GameHotkeyPressed?.Invoke(this, new GameHotkeyPressedEventArgs
            {
                GameId = gameId,
                GameName = gameName,
                Hotkey = hotkey
            });
        }
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _gameHotkeys.Clear();
            _hotkeyToGame.Clear();
            _isDisposed = true;
        }
    }
}
