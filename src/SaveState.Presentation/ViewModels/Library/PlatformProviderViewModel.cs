using CommunityToolkit.Mvvm.ComponentModel;

namespace SaveState.Presentation.ViewModels.Library;

/// <summary>
/// View model representing a game platform provider (Steam, GOG, Epic, etc.).
/// </summary>
public partial class PlatformProviderViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _icon = string.Empty;

    [ObservableProperty]
    private string _color = "#666666";

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private bool _supportsDiscovery;

    [ObservableProperty]
    private bool _supportsMetadata;

    [ObservableProperty]
    private bool _supportsLaunch;

    [ObservableProperty]
    private string _status = "Ready";

    [ObservableProperty]
    private bool _isHealthy = true;

    [ObservableProperty]
    private int _gamesFound;

    [ObservableProperty]
    private string? _errorMessage;

    public PlatformProviderViewModel()
    {
    }

    public PlatformProviderViewModel(
        string name,
        string displayName,
        string icon,
        string color,
        bool supportsDiscovery,
        bool supportsMetadata,
        bool supportsLaunch)
    {
        Name = name;
        DisplayName = displayName;
        Icon = icon;
        Color = color;
        SupportsDiscovery = supportsDiscovery;
        SupportsMetadata = supportsMetadata;
        SupportsLaunch = supportsLaunch;
    }

    public void UpdateStatus(string status, bool isHealthy = true, string? errorMessage = null)
    {
        Status = status;
        IsHealthy = isHealthy;
        ErrorMessage = errorMessage;
    }

    public void UpdateGamesFound(int count)
    {
        GamesFound = count;
    }
}
