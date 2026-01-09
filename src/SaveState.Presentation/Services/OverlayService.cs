using System;
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
    private bool _showNotifications;
    private bool _showUserProfile;
    private bool _showNetworkDiagnostics;
    private bool _showSyncStatus;
    private bool _showConflictsResolution;
    private bool _showProviderConfiguration;
    private bool _showDashboardCustomization;
    private bool _showCreateCollection;
    private bool _isVoiceActive;
    private bool _showSessionDetails;
    private bool _showAchievementDetails;
    private bool _showModDetails;
    private Guid? _currentSessionGameId;
    private Guid? _currentAchievementId;
    private Guid? _currentModId;

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
    public bool ShowNotifications
    {
        get => _showNotifications;
        private set => SetProperty(ref _showNotifications, value);
    }

    /// <inheritdoc />
    public bool ShowUserProfile
    {
        get => _showUserProfile;
        private set => SetProperty(ref _showUserProfile, value);
    }

    /// <inheritdoc />
    public bool ShowNetworkDiagnostics
    {
        get => _showNetworkDiagnostics;
        private set => SetProperty(ref _showNetworkDiagnostics, value);
    }

    /// <inheritdoc />
    public bool ShowSyncStatus
    {
        get => _showSyncStatus;
        private set => SetProperty(ref _showSyncStatus, value);
    }

    /// <inheritdoc />
    public bool ShowConflictsResolution
    {
        get => _showConflictsResolution;
        private set => SetProperty(ref _showConflictsResolution, value);
    }

    /// <inheritdoc />
    public bool ShowProviderConfiguration
    {
        get => _showProviderConfiguration;
        private set => SetProperty(ref _showProviderConfiguration, value);
    }

    /// <inheritdoc />
    public bool ShowDashboardCustomization
    {
        get => _showDashboardCustomization;
        private set => SetProperty(ref _showDashboardCustomization, value);
    }

    /// <inheritdoc />
    public bool ShowCreateCollection
    {
        get => _showCreateCollection;
        private set => SetProperty(ref _showCreateCollection, value);
    }

    /// <inheritdoc />
    public bool ShowSessionDetails
    {
        get => _showSessionDetails;
        private set => SetProperty(ref _showSessionDetails, value);
    }

    /// <inheritdoc />
    public bool ShowAchievementDetails
    {
        get => _showAchievementDetails;
        private set => SetProperty(ref _showAchievementDetails, value);
    }

    /// <inheritdoc />
    public bool ShowModDetails
    {
        get => _showModDetails;
        private set => SetProperty(ref _showModDetails, value);
    }

    /// <inheritdoc />
    public Guid? CurrentSessionGameId => _currentSessionGameId;

    /// <inheritdoc />
    public Guid? CurrentAchievementId => _currentAchievementId;

    /// <inheritdoc />
    public Guid? CurrentModId => _currentModId;

    /// <inheritdoc />
    public bool ShowDim => ShowCommandPalette || ShowQuickSearch || ShowAiAssistant || ShowNotifications || ShowUserProfile ||
                           ShowNetworkDiagnostics || ShowSyncStatus || ShowConflictsResolution ||
                           ShowProviderConfiguration || ShowDashboardCustomization || ShowCreateCollection ||
                           ShowSessionDetails || ShowAchievementDetails || ShowModDetails;

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
    public void ShowSessionDetailsOverlay(Guid gameId)
    {
        _logger.LogInformation("Showing session details overlay for game {GameId}", gameId);
        _currentSessionGameId = gameId;
        ShowSessionDetails = true;
        OverlayChanged?.Invoke(this, new OverlayChangedEventArgs("SessionDetails", true));
    }

    /// <inheritdoc />
    public void HideSessionDetailsOverlay()
    {
        ShowSessionDetails = false;
        _currentSessionGameId = null;
        OverlayChanged?.Invoke(this, new OverlayChangedEventArgs("SessionDetails", false));
    }

    /// <inheritdoc />
    public void ShowAchievementDetailsOverlay(Guid achievementId)
    {
        _logger.LogInformation("Showing achievement details overlay for achievement {AchievementId}", achievementId);
        _currentAchievementId = achievementId;
        ShowAchievementDetails = true;
        OverlayChanged?.Invoke(this, new OverlayChangedEventArgs("AchievementDetails", true));
    }

    /// <inheritdoc />
    public void HideAchievementDetailsOverlay()
    {
        ShowAchievementDetails = false;
        _currentAchievementId = null;
        OverlayChanged?.Invoke(this, new OverlayChangedEventArgs("AchievementDetails", false));
    }

    /// <inheritdoc />
    public void ShowModDetailsOverlay(Guid modId)
    {
        _logger.LogInformation("Showing mod details overlay for mod {ModId}", modId);
        _currentModId = modId;
        ShowModDetails = true;
        OverlayChanged?.Invoke(this, new OverlayChangedEventArgs("ModDetails", true));
    }

    /// <inheritdoc />
    public void HideModDetailsOverlay()
    {
        ShowModDetails = false;
        _currentModId = null;
        OverlayChanged?.Invoke(this, new OverlayChangedEventArgs("ModDetails", false));
    }

    /// <inheritdoc />
    public void ShowNotificationsOverlay()
    {
        ShowNotifications = true;
        _logger.LogInformation("Showing notifications overlay");
        OverlayChanged?.Invoke(this, new OverlayChangedEventArgs("Notifications", true));
    }

    /// <inheritdoc />
    public void HideNotificationsOverlay()
    {
        ShowNotifications = false;
        _logger.LogInformation("Hiding notifications overlay");
        OverlayChanged?.Invoke(this, new OverlayChangedEventArgs("Notifications", false));
    }

    /// <inheritdoc />
    public void ToggleNotificationsOverlay()
    {
        if (ShowNotifications)
            HideNotificationsOverlay();
        else
            ShowNotificationsOverlay();
    }

    /// <inheritdoc />
    public void ShowUserProfileOverlay()
    {
        ShowUserProfile = true;
        _logger.LogInformation("Showing user profile overlay");
        OverlayChanged?.Invoke(this, new OverlayChangedEventArgs("UserProfile", true));
    }

    /// <inheritdoc />
    public void HideUserProfileOverlay()
    {
        ShowUserProfile = false;
        _logger.LogInformation("Hiding user profile overlay");
        OverlayChanged?.Invoke(this, new OverlayChangedEventArgs("UserProfile", false));
    }

    /// <inheritdoc />
    public void ToggleUserProfileOverlay()
    {
        if (ShowUserProfile)
            HideUserProfileOverlay();
        else
            ShowUserProfileOverlay();
    }

    /// <inheritdoc />
    public void ShowNetworkDiagnosticsOverlay()
    {
        ShowNetworkDiagnostics = true;
        _logger.LogInformation("Showing network diagnostics overlay");
        OverlayChanged?.Invoke(this, new OverlayChangedEventArgs("NetworkDiagnostics", true));
    }

    /// <inheritdoc />
    public void HideNetworkDiagnosticsOverlay()
    {
        ShowNetworkDiagnostics = false;
        _logger.LogInformation("Hiding network diagnostics overlay");
        OverlayChanged?.Invoke(this, new OverlayChangedEventArgs("NetworkDiagnostics", false));
    }

    /// <inheritdoc />
    public void ToggleNetworkDiagnosticsOverlay()
    {
        if (ShowNetworkDiagnostics)
            HideNetworkDiagnosticsOverlay();
        else
            ShowNetworkDiagnosticsOverlay();
    }

    /// <inheritdoc />
    public void ShowSyncStatusOverlay()
    {
        ShowSyncStatus = true;
        _logger.LogInformation("Showing sync status overlay");
        OverlayChanged?.Invoke(this, new OverlayChangedEventArgs("SyncStatus", true));
    }

    /// <inheritdoc />
    public void HideSyncStatusOverlay()
    {
        ShowSyncStatus = false;
        _logger.LogInformation("Hiding sync status overlay");
        OverlayChanged?.Invoke(this, new OverlayChangedEventArgs("SyncStatus", false));
    }

    /// <inheritdoc />
    public void ToggleSyncStatusOverlay()
    {
        if (ShowSyncStatus)
            HideSyncStatusOverlay();
        else
            ShowSyncStatusOverlay();
    }

    /// <inheritdoc />
    public void ShowConflictsResolutionOverlay()
    {
        ShowConflictsResolution = true;
        _logger.LogInformation("Showing conflicts resolution overlay");
        OverlayChanged?.Invoke(this, new OverlayChangedEventArgs("ConflictsResolution", true));
    }

    /// <inheritdoc />
    public void HideConflictsResolutionOverlay()
    {
        ShowConflictsResolution = false;
        _logger.LogInformation("Hiding conflicts resolution overlay");
        OverlayChanged?.Invoke(this, new OverlayChangedEventArgs("ConflictsResolution", false));
    }

    /// <inheritdoc />
    public void ShowProviderConfigurationDialog()
    {
        ShowProviderConfiguration = true;
        _logger.LogInformation("Showing provider configuration dialog");
        OverlayChanged?.Invoke(this, new OverlayChangedEventArgs("ProviderConfiguration", true));
    }

    /// <inheritdoc />
    public void HideProviderConfigurationDialog()
    {
        ShowProviderConfiguration = false;
        _logger.LogInformation("Hiding provider configuration dialog");
        OverlayChanged?.Invoke(this, new OverlayChangedEventArgs("ProviderConfiguration", false));
    }

    /// <inheritdoc />
    public void ShowDashboardCustomizationDialog()
    {
        ShowDashboardCustomization = true;
        _logger.LogInformation("Showing dashboard customization dialog");
        OverlayChanged?.Invoke(this, new OverlayChangedEventArgs("DashboardCustomization", true));
    }

    /// <inheritdoc />
    public void HideDashboardCustomizationDialog()
    {
        ShowDashboardCustomization = false;
        _logger.LogInformation("Hiding dashboard customization dialog");
        OverlayChanged?.Invoke(this, new OverlayChangedEventArgs("DashboardCustomization", false));
    }

    /// <inheritdoc />
    public void ShowCreateCollectionDialog()
    {
        ShowCreateCollection = true;
        _logger.LogInformation("Showing create collection dialog");
        OverlayChanged?.Invoke(this, new OverlayChangedEventArgs("CreateCollection", true));
    }

    /// <inheritdoc />
    public void HideCreateCollectionDialog()
    {
        ShowCreateCollection = false;
        _logger.LogInformation("Hiding create collection dialog");
        OverlayChanged?.Invoke(this, new OverlayChangedEventArgs("CreateCollection", false));
    }

    /// <inheritdoc />
    public void HideAllOverlays()
    {
        var hadOverlays = ShowCommandPalette || ShowQuickSearch || ShowAiAssistant || ShowPerformanceHud ||
                          ShowNotifications || ShowUserProfile || ShowNetworkDiagnostics || ShowSyncStatus ||
                          ShowConflictsResolution || ShowProviderConfiguration || ShowDashboardCustomization ||
                          ShowCreateCollection || ShowSessionDetails || ShowAchievementDetails || ShowModDetails;

        ShowCommandPalette = false;
        ShowQuickSearch = false;
        ShowAiAssistant = false;
        ShowPerformanceHud = false;
        ShowNotifications = false;
        ShowUserProfile = false;
        ShowNetworkDiagnostics = false;
        ShowSyncStatus = false;
        ShowConflictsResolution = false;
        ShowProviderConfiguration = false;
        ShowDashboardCustomization = false;
        ShowCreateCollection = false;
        ShowSessionDetails = false;
        ShowAchievementDetails = false;
        ShowModDetails = false;
        IsVoiceActive = false;

        if (hadOverlays)
        {
            _logger.LogDebug("Hiding all overlays");
        }
    }

    /// <inheritdoc />
    public event EventHandler<OverlayChangedEventArgs>? OverlayChanged;
}
