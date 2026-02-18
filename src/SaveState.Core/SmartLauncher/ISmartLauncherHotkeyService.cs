// Copyright (c) 2026 SaveStateReborn. All rights reserved.

namespace SaveState.Core.SmartLauncher;

/// <summary>
/// Service for managing Smart Launcher keyboard shortcuts and hotkeys.
/// </summary>
public interface ISmartLauncherHotkeyService
{
    /// <summary>
    /// Registers all default hotkeys.
    /// </summary>
    Task RegisterDefaultHotkeysAsync(CancellationToken ct = default);

    /// <summary>
    /// Unregisters all hotkeys.
    /// </summary>
    Task UnregisterAllHotkeysAsync(CancellationToken ct = default);

    /// <summary>
    /// Registers a hotkey for quick launching a game.
    /// </summary>
    Task<bool> RegisterGameHotkeyAsync(Guid gameId, string hotkey, CancellationToken ct = default);

    /// <summary>
    /// Unregisters a game hotkey.
    /// </summary>
    Task<bool> UnregisterGameHotkeyAsync(Guid gameId, CancellationToken ct = default);

    /// <summary>
    /// Gets the hotkey assigned to a game.
    /// </summary>
    Task<string?> GetGameHotkeyAsync(Guid gameId, CancellationToken ct = default);

    /// <summary>
    /// Gets all registered game hotkeys.
    /// </summary>
    Task<IReadOnlyList<GameHotkeyMapping>> GetAllGameHotkeysAsync(CancellationToken ct = default);

    /// <summary>
    /// Event raised when a game hotkey is pressed.
    /// </summary>
    event EventHandler<GameHotkeyPressedEventArgs>? GameHotkeyPressed;

    /// <summary>
    /// Event raised when the stop game hotkey is pressed.
    /// </summary>
    event EventHandler<StopGameHotkeyPressedEventArgs>? StopGameHotkeyPressed;

    /// <summary>
    /// Event raised when the show launcher hotkey is pressed.
    /// </summary>
    event EventHandler<ShowLauncherHotkeyPressedEventArgs>? ShowLauncherHotkeyPressed;
}

/// <summary>
/// Mapping between a game and its hotkey.
/// </summary>
public class GameHotkeyMapping
{
    public Guid GameId { get; set; }
    public string GameName { get; set; } = string.Empty;
    public string Hotkey { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
}

/// <summary>
/// Event args for game hotkey pressed.
/// </summary>
public class GameHotkeyPressedEventArgs : EventArgs
{
    public Guid GameId { get; set; }
    public string GameName { get; set; } = string.Empty;
    public string Hotkey { get; set; } = string.Empty;
}

/// <summary>
/// Event args for stop game hotkey pressed.
/// </summary>
public class StopGameHotkeyPressedEventArgs : EventArgs
{
    public string Hotkey { get; set; } = string.Empty;
}

/// <summary>
/// Event args for show launcher hotkey pressed.
/// </summary>
public class ShowLauncherHotkeyPressedEventArgs : EventArgs
{
    public string Hotkey { get; set; } = string.Empty;
}

/// <summary>
/// Configuration for Smart Launcher hotkeys.
/// </summary>
public class SmartLauncherHotkeyConfig
{
    /// <summary>
    /// Hotkey to stop the current game (default: Ctrl+Alt+End).
    /// </summary>
    public string StopGameHotkey { get; set; } = "Ctrl+Alt+End";

    /// <summary>
    /// Hotkey to show the launcher (default: Ctrl+Alt+Home).
    /// </summary>
    public string ShowLauncherHotkey { get; set; } = "Ctrl+Alt+Home";

    /// <summary>
    /// Hotkey to toggle optimization overlay (default: Ctrl+Alt+O).
    /// </summary>
    public string ToggleOverlayHotkey { get; set; } = "Ctrl+Alt+O";

    /// <summary>
    /// Enable game-specific hotkeys (Ctrl+Alt+1 through Ctrl+Alt+9).
    /// </summary>
    public bool EnableNumberedHotkeys { get; set; } = true;

    /// <summary>
    /// Whether hotkeys are globally registered (work even when app is not focused).
    /// </summary>
    public bool GlobalHotkeys { get; set; } = true;
}
