using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Presentation.Models.CloudGaming;

namespace SaveState.Presentation.ViewModels.CloudGaming;

/// <summary>
/// ViewModel for the Stream Launcher configuration view.
/// </summary>
public partial class StreamLauncherViewModel : ObservableObject
{
    private readonly ILogger<StreamLauncherViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the StreamLauncherViewModel.
    /// </summary>
    public StreamLauncherViewModel(ILogger<StreamLauncherViewModel> logger)
    {
        _logger = logger;
        AvailableControllers = new List<string>
        {
            "Xbox Controller",
            "PlayStation Controller",
            "Nintendo Switch Pro",
            "Keyboard + Mouse",
            "Steam Controller"
        };
        AvailableMicrophones = new List<string>
        {
            "Default Device",
            "Microphone (USB)",
            "Headset Microphone",
            "Disabled"
        };
    }

    #region Observable Properties

    /// <summary>
    /// The game being launched.
    /// </summary>
    [ObservableProperty]
    private CloudGame? _game;

    /// <summary>
    /// Stream settings for this session.
    /// </summary>
    [ObservableProperty]
    private StreamSettings _settings = new();

    /// <summary>
    /// Available controller options.
    /// </summary>
    [ObservableProperty]
    private List<string> _availableControllers;

    /// <summary>
    /// Available microphone options.
    /// </summary>
    [ObservableProperty]
    private List<string> _availableMicrophones;

    /// <summary>
    /// Current data center.
    /// </summary>
    [ObservableProperty]
    private DataCenter? _currentDataCenter;

    /// <summary>
    /// Connection test results.
    /// </summary>
    [ObservableProperty]
    private ConnectionTestResult? _connectionTest;

    /// <summary>
    /// Whether launch is in progress.
    /// </summary>
    [ObservableProperty]
    private bool _isLaunching;

    /// <summary>
    /// Whether settings should be saved as default.
    /// </summary>
    [ObservableProperty]
    private bool _saveAsDefault;

    /// <summary>
    /// Recommended quality based on connection.
    /// </summary>
    public SessionQuality RecommendedQuality =>
        ConnectionTest?.RecommendedQuality ?? SessionQuality.High;

    /// <summary>
    /// Whether 4K streaming is possible.
    /// </summary>
    public bool CanStream4K => ConnectionTest?.CanStream4K ?? false;

    /// <summary>
    /// Quality description text.
    /// </summary>
    public string QualityDescription => Settings.Quality switch
    {
        SessionQuality.Low => "720p 30fps - Low bandwidth",
        SessionQuality.Medium => "1080p 30fps - Balanced quality",
        SessionQuality.High => "1080p 60fps - High quality",
        SessionQuality.Ultra => "4K 60fps - Ultra quality",
        SessionQuality.Adaptive => "Dynamic - Adjusts to connection",
        _ => "Custom"
    };

    /// <summary>
    /// Data center display text.
    /// </summary>
    public string DataCenterDisplay =>
        CurrentDataCenter is not null
            ? $"{CurrentDataCenter.Name} - {CurrentDataCenter.Ping}ms"
            : "Auto-select";

    #endregion

    #region Commands

    /// <summary>
    /// Launches the game with configured settings.
    /// </summary>
    [RelayCommand]
    private async Task LaunchAsync()
    {
        if (Game is null) return;

        IsLaunching = true;
        _logger.LogInformation("Launching {GameTitle} with {Quality} quality",
            Game.Title, Settings.Quality);

        try
        {
            if (SaveAsDefault)
            {
                // TODO: Save settings as default
                _logger.LogInformation("Saved settings as default");
            }

            // TODO: Initiate actual stream session
            await Task.Delay(2000); // Simulate launch

            _logger.LogInformation("Stream launched successfully");
        }
        finally
        {
            IsLaunching = false;
        }
    }

    /// <summary>
    /// Cancels launch and closes view.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        _logger.LogInformation("Launch cancelled");
        // TODO: Close dialog/view
    }

    /// <summary>
    /// Changes the data center.
    /// </summary>
    [RelayCommand]
    private async Task ChangeDataCenterAsync()
    {
        _logger.LogInformation("Opening data center selection");

        // TODO: Show data center selection dialog
        await Task.Delay(100);
    }

    /// <summary>
    /// Tests connection to the provider.
    /// </summary>
    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (Game is null) return;

        _logger.LogInformation("Testing connection for {Provider}", Game.Provider);

        // TODO: Run connection test
        await Task.Delay(1500);

        ConnectionTest = new ConnectionTestResult
        {
            Ping = 12,
            Jitter = 2,
            PacketLoss = 0.1f,
            DownloadSpeed = 85.5f,
            UploadSpeed = 25.3f,
            RecommendedQuality = SessionQuality.Ultra,
            CanStream4K = true
        };

        OnPropertyChanged(nameof(RecommendedQuality));
        OnPropertyChanged(nameof(CanStream4K));
    }

    /// <summary>
    /// Sets quality to recommended based on connection test.
    /// </summary>
    [RelayCommand]
    private void UseRecommendedQuality()
    {
        Settings.Quality = RecommendedQuality;
        OnPropertyChanged(nameof(Settings));
        OnPropertyChanged(nameof(QualityDescription));
    }

    #endregion

    /// <summary>
    /// Initializes the view model for launching a game.
    /// </summary>
    public void Initialize(CloudGame game, StreamSettings? defaultSettings = null)
    {
        Game = game;
        Settings = defaultSettings ?? new StreamSettings();

        // TODO: Load current data center from provider service
        // TODO: Load connection test results

        OnPropertyChanged(nameof(QualityDescription));
        OnPropertyChanged(nameof(DataCenterDisplay));
    }

    partial void OnSettingsChanged(StreamSettings value)
    {
        OnPropertyChanged(nameof(QualityDescription));
    }
}
