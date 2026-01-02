using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace SaveState.Presentation.Services;

/// <summary>
/// Implementation of the overlay service.
/// </summary>
public class OverlayService : ObservableObject, IOverlayService
{
    private readonly ILogger<OverlayService> _logger;

    private bool _showCommandPalette;
    private bool _showQuickSearch;
    private bool _showAiAssistant;
    private bool _showPerformanceHud;
    private bool _isVoiceActive;

    public OverlayService(ILogger<OverlayService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public bool ShowCommandPalette
    {
        get => _showCommandPalette;
        private set => SetProperty(ref _showCommandPalette, value);
    }

    /// <inheritdoc />
    public bool ShowQuickSearch
    {
        get => _showQuickSearch;
        private set => SetProperty(ref _showQuickSearch, value);
    }

    /// <inheritdoc />
    public bool ShowAiAssistant
    {
        get => _showAiAssistant;
        private set => SetProperty(ref _showAiAssistant, value);
    }

    /// <inheritdoc />
    public bool ShowPerformanceHud
    {
        get => _showPerformanceHud;
        private set => SetProperty(ref _showPerformanceHud, value);
    }

    /// <inheritdoc />
    public bool IsVoiceActive
    {
        get => _isVoiceActive;
        private set => SetProperty(ref _isVoiceActive, value);
    }

    /// <inheritdoc />
    public bool ShowDim => ShowCommandPalette || ShowQuickSearch || ShowAiAssistant;

    /// <inheritdoc />
    public void ShowCommandPaletteOverlay()
    {
        ShowCommandPalette = true;
        _logger.LogDebug("Showing command palette overlay");
        OverlayChanged?.Invoke(this, new OverlayChangedEventArgs("CommandPalette", true));
    }

    /// <inheritdoc />
    public void HideCommandPaletteOverlay()
    {
        ShowCommandPalette = false;
        _logger.LogDebug("Hiding command palette overlay");
        OverlayChanged?.Invoke(this, new OverlayChangedEventArgs("CommandPalette", false));
    }

    /// <inheritdoc />
    public void ToggleCommandPaletteOverlay()
    {
        if (ShowCommandPalette)
            HideCommandPaletteOverlay();
        else
            ShowCommandPaletteOverlay();
    }

    /// <inheritdoc />
    public void ShowQuickSearchOverlay()
    {
        ShowQuickSearch = true;
        _logger.LogDebug("Showing quick search overlay");
        OverlayChanged?.Invoke(this, new OverlayChangedEventArgs("QuickSearch", true));
    }

    /// <inheritdoc />
    public void HideQuickSearchOverlay()
    {
        ShowQuickSearch = false;
        _logger.LogDebug("Hiding quick search overlay");
        OverlayChanged?.Invoke(this, new OverlayChangedEventArgs("QuickSearch", false));
    }

    /// <inheritdoc />
    public void ToggleQuickSearchOverlay()
    {
        if (ShowQuickSearch)
            HideQuickSearchOverlay();
        else
            ShowQuickSearchOverlay();
    }

    /// <inheritdoc />
    public void ShowAiAssistantOverlay()
    {
        ShowAiAssistant = true;
        _logger.LogDebug("Showing AI assistant overlay");
        OverlayChanged?.Invoke(this, new OverlayChangedEventArgs("AiAssistant", true));
    }

    /// <inheritdoc />
    public void HideAiAssistantOverlay()
    {
        ShowAiAssistant = false;
        _logger.LogDebug("Hiding AI assistant overlay");
        OverlayChanged?.Invoke(this, new OverlayChangedEventArgs("AiAssistant", false));
    }

    /// <inheritdoc />
    public void ToggleAiAssistantOverlay()
    {
        if (ShowAiAssistant)
            HideAiAssistantOverlay();
        else
            ShowAiAssistantOverlay();
    }

    /// <inheritdoc />
    public void ShowPerformanceHudOverlay()
    {
        ShowPerformanceHud = true;
        _logger.LogDebug("Showing performance HUD overlay");
        OverlayChanged?.Invoke(this, new OverlayChangedEventArgs("PerformanceHud", true));
    }

    /// <inheritdoc />
    public void HidePerformanceHudOverlay()
    {
        ShowPerformanceHud = false;
        _logger.LogDebug("Hiding performance HUD overlay");
        OverlayChanged?.Invoke(this, new OverlayChangedEventArgs("PerformanceHud", false));
    }

    /// <inheritdoc />
    public void TogglePerformanceHudOverlay()
    {
        if (ShowPerformanceHud)
            HidePerformanceHudOverlay();
        else
            ShowPerformanceHudOverlay();
    }

    /// <inheritdoc />
    public void SetVoiceActive(bool isActive)
    {
        IsVoiceActive = isActive;
        _logger.LogDebug("Voice indicator set to: {IsActive}", isActive);
        OverlayChanged?.Invoke(this, new OverlayChangedEventArgs("VoiceIndicator", isActive));
    }

    /// <inheritdoc />
    public void HideAllOverlays()
    {
        var hadOverlays = ShowCommandPalette || ShowQuickSearch || ShowAiAssistant || ShowPerformanceHud;

        ShowCommandPalette = false;
        ShowQuickSearch = false;
        ShowAiAssistant = false;
        ShowPerformanceHud = false;
        IsVoiceActive = false;

        if (hadOverlays)
        {
            _logger.LogDebug("Hiding all overlays");
        }
    }

    /// <inheritdoc />
    public event EventHandler<OverlayChangedEventArgs>? OverlayChanged;
}