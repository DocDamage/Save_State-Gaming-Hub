using System;

namespace SaveState.Presentation.Services;

/// <summary>
/// Service for managing application overlays (command palette, AI assistant, etc.).
/// </summary>
public interface IOverlayService
{
    /// <summary>
    /// Gets whether the command palette is currently visible.
    /// </summary>
    bool ShowCommandPalette { get; }

    /// <summary>
    /// Gets whether the quick search overlay is visible.
    /// </summary>
    bool ShowQuickSearch { get; }

    /// <summary>
    /// Gets whether the AI assistant is visible.
    /// </summary>
    bool ShowAiAssistant { get; }

    /// <summary>
    /// Gets whether the performance HUD is visible.
    /// </summary>
    bool ShowPerformanceHud { get; }

    /// <summary>
    /// Gets whether the voice indicator is visible.
    /// </summary>
    bool IsVoiceActive { get; }

    /// <summary>
    /// Gets whether any overlay is dimming the background.
    /// </summary>
    bool ShowDim { get; }

    /// <summary>
    /// Gets whether the session details overlay is visible.
    /// </summary>
    bool ShowSessionDetails { get; }

    /// <summary>
    /// Gets whether the achievement details overlay is visible.
    /// </summary>
    bool ShowAchievementDetails { get; }

    /// <summary>
    /// Gets whether the mod details overlay is visible.
    /// </summary>
    bool ShowModDetails { get; }

    /// <summary>
    /// Gets the game ID for the current session details overlay.
    /// </summary>
    Guid? CurrentSessionGameId { get; }

    /// <summary>
    /// Gets the achievement ID for the current achievement details overlay.
    /// </summary>
    Guid? CurrentAchievementId { get; }

    /// <summary>
    /// Gets the mod ID for the current mod details overlay.
    /// </summary>
    Guid? CurrentModId { get; }

    /// <summary>
    /// Shows the command palette overlay.
    /// </summary>
    void ShowCommandPaletteOverlay();

    /// <summary>
    /// Hides the command palette overlay.
    /// </summary>
    void HideCommandPaletteOverlay();

    /// <summary>
    /// Toggles the command palette overlay.
    /// </summary>
    void ToggleCommandPaletteOverlay();

    /// <summary>
    /// Shows the quick search overlay.
    /// </summary>
    void ShowQuickSearchOverlay();

    /// <summary>
    /// Hides the quick search overlay.
    /// </summary>
    void HideQuickSearchOverlay();

    /// <summary>
    /// Toggles the quick search overlay.
    /// </summary>
    void ToggleQuickSearchOverlay();

    /// <summary>
    /// Shows the AI assistant overlay.
    /// </summary>
    void ShowAiAssistantOverlay();

    /// <summary>
    /// Hides the AI assistant overlay.
    /// </summary>
    void HideAiAssistantOverlay();

    /// <summary>
    /// Toggles the AI assistant overlay.
    /// </summary>
    void ToggleAiAssistantOverlay();

    /// <summary>
    /// Shows the performance HUD overlay.
    /// </summary>
    void ShowPerformanceHudOverlay();

    /// <summary>
    /// Hides the performance HUD overlay.
    /// </summary>
    void HidePerformanceHudOverlay();

    /// <summary>
    /// Toggles the performance HUD overlay.
    /// </summary>
    void TogglePerformanceHudOverlay();

    /// <summary>
    /// Sets the voice indicator visibility.
    /// </summary>
    /// <param name="isActive">Whether voice recognition is active.</param>
    void SetVoiceActive(bool isActive);

    /// <summary>
    /// Shows the session details overlay for a specific game.
    /// </summary>
    /// <param name="gameId">The game ID to show session details for.</param>
    void ShowSessionDetailsOverlay(Guid gameId);

    /// <summary>
    /// Hides the session details overlay.
    /// </summary>
    void HideSessionDetailsOverlay();

    /// <summary>
    /// Shows the achievement details overlay for a specific achievement.
    /// </summary>
    /// <param name="achievementId">The achievement ID to show details for.</param>
    void ShowAchievementDetailsOverlay(Guid achievementId);

    /// <summary>
    /// Hides the achievement details overlay.
    /// </summary>
    void HideAchievementDetailsOverlay();

    /// <summary>
    /// Shows the mod details overlay for a specific mod.
    /// </summary>
    /// <param name="modId">The mod ID to show details for.</param>
    void ShowModDetailsOverlay(Guid modId);

    /// <summary>
    /// Hides the mod details overlay.
    /// </summary>
    void HideModDetailsOverlay();

    /// <summary>
    /// Shows the notifications panel overlay.
    /// </summary>
    void ShowNotificationsOverlay();

    /// <summary>
    /// Hides the notifications panel overlay.
    /// </summary>
    void HideNotificationsOverlay();

    /// <summary>
    /// Toggles the notifications panel overlay.
    /// </summary>
    void ToggleNotificationsOverlay();

    /// <summary>
    /// Gets whether the notifications panel is visible.
    /// </summary>
    bool ShowNotifications { get; }

    /// <summary>
    /// Gets whether the user profile overlay is visible.
    /// </summary>
    bool ShowUserProfile { get; }

    /// <summary>
    /// Shows the user profile overlay.
    /// </summary>
    void ShowUserProfileOverlay();

    /// <summary>
    /// Hides the user profile overlay.
    /// </summary>
    void HideUserProfileOverlay();

    /// <summary>
    /// Toggles the user profile overlay.
    /// </summary>
    void ToggleUserProfileOverlay();

    /// <summary>
    /// Gets whether the network diagnostics overlay is visible.
    /// </summary>
    bool ShowNetworkDiagnostics { get; }

    /// <summary>
    /// Shows the network diagnostics overlay.
    /// </summary>
    void ShowNetworkDiagnosticsOverlay();

    /// <summary>
    /// Hides the network diagnostics overlay.
    /// </summary>
    void HideNetworkDiagnosticsOverlay();

    /// <summary>
    /// Toggles the network diagnostics overlay.
    /// </summary>
    void ToggleNetworkDiagnosticsOverlay();

    /// <summary>
    /// Gets whether the sync status overlay is visible.
    /// </summary>
    bool ShowSyncStatus { get; }

    /// <summary>
    /// Shows the sync status overlay.
    /// </summary>
    void ShowSyncStatusOverlay();

    /// <summary>
    /// Hides the sync status overlay.
    /// </summary>
    void HideSyncStatusOverlay();

    /// <summary>
    /// Toggles the sync status overlay.
    /// </summary>
    void ToggleSyncStatusOverlay();

    /// <summary>
    /// Gets whether the conflicts resolution overlay is visible.
    /// </summary>
    bool ShowConflictsResolution { get; }

    /// <summary>
    /// Shows the conflicts resolution overlay.
    /// </summary>
    void ShowConflictsResolutionOverlay();

    /// <summary>
    /// Hides the conflicts resolution overlay.
    /// </summary>
    void HideConflictsResolutionOverlay();

    /// <summary>
    /// Gets whether the provider configuration dialog is visible.
    /// </summary>
    bool ShowProviderConfiguration { get; }

    /// <summary>
    /// Shows the provider configuration dialog.
    /// </summary>
    void ShowProviderConfigurationDialog();

    /// <summary>
    /// Hides the provider configuration dialog.
    /// </summary>
    void HideProviderConfigurationDialog();

    /// <summary>
    /// Gets whether the dashboard customization dialog is visible.
    /// </summary>
    bool ShowDashboardCustomization { get; }

    /// <summary>
    /// Shows the dashboard customization dialog.
    /// </summary>
    void ShowDashboardCustomizationDialog();

    /// <summary>
    /// Hides the dashboard customization dialog.
    /// </summary>
    void HideDashboardCustomizationDialog();

    /// <summary>
    /// Gets whether the create collection dialog is visible.
    /// </summary>
    bool ShowCreateCollection { get; }

    /// <summary>
    /// Shows the create collection dialog.
    /// </summary>
    void ShowCreateCollectionDialog();

    /// <summary>
    /// Hides the create collection dialog.
    /// </summary>
    void HideCreateCollectionDialog();

    /// <summary>
    /// Hides all overlays.
    /// </summary>
    void HideAllOverlays();

    /// <summary>
    /// Raised when overlay visibility changes.
    /// </summary>
    event EventHandler<OverlayChangedEventArgs>? OverlayChanged;
}

/// <summary>
/// Event arguments for overlay visibility changes.
/// </summary>
public class OverlayChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the overlay that changed.
    /// </summary>
    public string OverlayName { get; }

    /// <summary>
    /// Gets whether the overlay is now visible.
    /// </summary>
    public bool IsVisible { get; }

    public OverlayChangedEventArgs(string overlayName, bool isVisible)
    {
        OverlayName = overlayName;
        IsVisible = isVisible;
    }
}
