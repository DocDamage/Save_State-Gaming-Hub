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