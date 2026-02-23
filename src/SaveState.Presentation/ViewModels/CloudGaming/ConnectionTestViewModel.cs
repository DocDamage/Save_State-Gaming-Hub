using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Presentation.Models.CloudGaming;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.CloudGaming;

/// <summary>
/// ViewModel for the Connection Test view.
/// </summary>
public partial class ConnectionTestViewModel : ObservableObject
{
    private readonly ILogger<ConnectionTestViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the ConnectionTestViewModel.
    /// </summary>
    public ConnectionTestViewModel(ILogger<ConnectionTestViewModel> logger)
    {
        _logger = logger;
        TestHistory = new ObservableCollection<ConnectionTestResult>();
        AvailableProviders = new ObservableCollection<CloudProvider>();
    }

    #region Observable Properties

    /// <summary>
    /// Provider being tested.
    /// </summary>
    [ObservableProperty]
    private CloudProvider _selectedProvider;

    /// <summary>
    /// Available providers to test.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<CloudProvider> _availableProviders;

    /// <summary>
    /// Current test results.
    /// </summary>
    [ObservableProperty]
    private ConnectionTestResult? _currentResult;

    /// <summary>
    /// History of previous tests.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ConnectionTestResult> _testHistory;

    /// <summary>
    /// Whether a test is currently running.
    /// </summary>
    [ObservableProperty]
    private bool _isTesting;

    /// <summary>
    /// Current test progress (0-100).
    /// </summary>
    [ObservableProperty]
    private int _testProgress;

    /// <summary>
    /// Current test phase description.
    /// </summary>
    [ObservableProperty]
    private string _testPhase = string.Empty;

    /// <summary>
    /// Data centers for the selected provider.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<DataCenter> _dataCenters = new();

    /// <summary>
    /// Selected data center for testing.
    /// </summary>
    [ObservableProperty]
    private DataCenter? _selectedDataCenter;

    /// <summary>
    /// Overall connection quality rating.
    /// </summary>
    public string ConnectionRating
    {
        get
        {
            if (CurrentResult is null) return "Not Tested";

            return CurrentResult.Ping switch
            {
                <= 20 => "Excellent",
                <= 40 => "Very Good",
                <= 60 => "Good",
                <= 100 => "Fair",
                _ => "Poor"
            };
        }
    }

    /// <summary>
    /// Color indicator for connection quality.
    /// </summary>
    public string ConnectionColor
    {
        get
        {
            if (CurrentResult is null) return "Gray";

            return CurrentResult.Ping switch
            {
                <= 20 => "#4CAF50", // Green
                <= 40 => "#8BC34A", // Light Green
                <= 60 => "#FFC107", // Yellow
                <= 100 => "#FF9800", // Orange
                _ => "#F44336" // Red
            };
        }
    }

    /// <summary>
    /// Whether results should be shown.
    /// </summary>
    public bool HasResults => CurrentResult is not null;

    #endregion

    #region Commands

    /// <summary>
    /// Runs the connection test.
    /// </summary>
    [RelayCommand]
    private async Task RunTestAsync()
    {
        IsTesting = true;
        TestProgress = 0;
        CurrentResult = null;

        _logger.LogInformation("Starting connection test for {Provider}", SelectedProvider);

        try
        {
            // Phase 1: Ping test
            TestPhase = "Testing latency...";
            await SimulateProgressAsync(30, 500);

            // Phase 2: Bandwidth test
            TestPhase = "Measuring bandwidth...";
            await SimulateProgressAsync(60, 800);

            // Phase 3: Jitter and packet loss
            TestPhase = "Analyzing stability...";
            await SimulateProgressAsync(90, 600);

            // Phase 4: Final calculation
            TestPhase = "Calculating results...";
            await SimulateProgressAsync(100, 300);

            // Generate results (in real implementation, these would be actual measurements)
            CurrentResult = new ConnectionTestResult
            {
                TestedAt = DateTime.UtcNow,
                Ping = Random.Shared.Next(10, 80),
                Jitter = Random.Shared.Next(1, 10),
                PacketLoss = Random.Shared.NextSingle() * 2,
                DownloadSpeed = 50 + Random.Shared.NextSingle() * 100,
                UploadSpeed = 10 + Random.Shared.NextSingle() * 40,
                RecommendedQuality = SessionQuality.High,
                CanStream4K = Random.Shared.NextDouble() > 0.5
            };

            // Determine recommended quality based on results
            if (CurrentResult.DownloadSpeed >= 80 && CurrentResult.Ping <= 30)
            {
                CurrentResult.RecommendedQuality = SessionQuality.Ultra;
                CurrentResult.CanStream4K = true;
            }
            else if (CurrentResult.DownloadSpeed >= 40 && CurrentResult.Ping <= 50)
            {
                CurrentResult.RecommendedQuality = SessionQuality.High;
                CurrentResult.CanStream4K = false;
            }
            else if (CurrentResult.DownloadSpeed >= 25)
            {
                CurrentResult.RecommendedQuality = SessionQuality.Medium;
                CurrentResult.CanStream4K = false;
            }
            else
            {
                CurrentResult.RecommendedQuality = SessionQuality.Low;
                CurrentResult.CanStream4K = false;
            }

            TestHistory.Insert(0, CurrentResult);

            _logger.LogInformation("Connection test complete: {Ping}ms ping, {Quality} recommended",
                CurrentResult.Ping, CurrentResult.RecommendedQuality);
        }
        finally
        {
            IsTesting = false;
            TestPhase = string.Empty;

            OnPropertyChanged(nameof(ConnectionRating));
            OnPropertyChanged(nameof(ConnectionColor));
            OnPropertyChanged(nameof(HasResults));
        }
    }

    /// <summary>
    /// Tests a specific data center.
    /// </summary>
    [RelayCommand]
    private async Task TestDataCenterAsync(DataCenter? dataCenter)
    {
        if (dataCenter is null) return;

        SelectedDataCenter = dataCenter;
        _logger.LogInformation("Testing data center: {Name}", dataCenter.Name);

        await RunTestAsync();
    }

    /// <summary>
    /// Closes the test view.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        // TODO: Close dialog
    }

    /// <summary>
    /// Saves the current results.
    /// </summary>
    [RelayCommand]
    private async Task SaveResultsAsync()
    {
        if (CurrentResult is null) return;

        _logger.LogInformation("Saving connection test results");

        // TODO: Save to settings or export
        await Task.Delay(100);
    }

    /// <summary>
    /// Copies results to clipboard.
    /// </summary>
    [RelayCommand]
    private void CopyResults()
    {
        if (CurrentResult is null) return;

        var text = $"""
            Cloud Gaming Connection Test Results
            Tested: {CurrentResult.TestedAt:g}
            Provider: {SelectedProvider}
            Ping: {CurrentResult.Ping}ms
            Jitter: {CurrentResult.Jitter:F1}ms
            Packet Loss: {CurrentResult.PacketLoss:F2}%
            Download: {CurrentResult.DownloadSpeed:F1} Mbps
            Upload: {CurrentResult.UploadSpeed:F1} Mbps
            Recommended Quality: {CurrentResult.RecommendedQuality}
            4K Capable: {(CurrentResult.CanStream4K ? "Yes" : "No")}
            """;

        // TODO: Copy to clipboard
        _logger.LogInformation("Results copied to clipboard");
    }

    /// <summary>
    /// Clears test history.
    /// </summary>
    [RelayCommand]
    private void ClearHistory()
    {
        TestHistory.Clear();
        _logger.LogInformation("Test history cleared");
    }

    #endregion

    private async Task SimulateProgressAsync(int targetProgress, int delayMs)
    {
        var step = (targetProgress - TestProgress) / 5;
        for (var i = 0; i < 5; i++)
        {
            TestProgress += step;
            await Task.Delay(delayMs / 5);
        }
    }

    /// <summary>
    /// Initializes the view model with available providers.
    /// </summary>
    public void Initialize(IEnumerable<CloudProvider> providers, CloudProvider defaultProvider)
    {
        AvailableProviders.Clear();
        foreach (var provider in providers)
        {
            AvailableProviders.Add(provider);
        }

        SelectedProvider = defaultProvider;

        // Initialize mock data centers
        DataCenters = new ObservableCollection<DataCenter>
        {
            new() { Id = "us-west", Name = "US West (San Jose)", Region = "North America", Ping = 12, IsRecommended = true },
            new() { Id = "us-east", Name = "US East (New York)", Region = "North America", Ping = 45 },
            new() { Id = "eu-west", Name = "EU West (London)", Region = "Europe", Ping = 85 },
            new() { Id = "ap-northeast", Name = "Asia Pacific (Tokyo)", Region = "Asia", Ping = 120 }
        };
    }

    partial void OnCurrentResultChanged(ConnectionTestResult? value)
    {
        OnPropertyChanged(nameof(ConnectionRating));
        OnPropertyChanged(nameof(ConnectionColor));
        OnPropertyChanged(nameof(HasResults));
    }

    partial void OnSelectedProviderChanged(CloudProvider value)
    {
        // Refresh data centers when provider changes
        CurrentResult = null;
        OnPropertyChanged(nameof(HasResults));
    }
}
