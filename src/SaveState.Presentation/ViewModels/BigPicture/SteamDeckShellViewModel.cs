using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Input.Services;
using SaveState.Core.Performance.Services;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.BigPicture;

public partial class SteamDeckShellViewModel : ObservableObject
{
    private readonly ISteamDeckManager _steamDeckManager;
    private readonly IBatteryOptimizer _batteryOptimizer;
    private readonly ITouchController _touchController;
    private readonly ILogger<SteamDeckShellViewModel> _logger;

    [ObservableProperty]
    private bool isSteamDeckModeActive;

    [ObservableProperty]
    private bool isSteamDeckDetected;

    [ObservableProperty]
    private string batteryStatusText = "Battery: Unknown";

    [ObservableProperty]
    private string performanceModeText = "Performance: Balanced";

    [ObservableProperty]
    private double batteryPercentage;

    [ObservableProperty]
    private bool isCharging;

    [ObservableProperty]
    private ObservableCollection<SteamDeckQuickAction> quickActions = new();

    public SteamDeckShellViewModel(
        ISteamDeckManager steamDeckManager,
        IBatteryOptimizer batteryOptimizer,
        ITouchController touchController,
        ILogger<SteamDeckShellViewModel> logger)
    {
        _steamDeckManager = steamDeckManager;
        _batteryOptimizer = batteryOptimizer;
        _touchController = touchController;
        _logger = logger;

        // Use fire-and-forget with proper error handling
        _ = InitializeAsync().ContinueWith(t =>
        {
            if (t.Exception != null)
            {
                _logger.LogError(t.Exception, "Failed to initialize Steam Deck shell");
            }
        }, TaskContinuationOptions.OnlyOnFaulted);

        SetupEventHandlers();
        InitializeQuickActions();
    }

    private async Task InitializeAsync()
    {
        try
        {
            // Detect Steam Deck
            var detectionResult = await _steamDeckManager.DetectSteamDeckAsync();
            IsSteamDeckDetected = detectionResult.Value;

            if (IsSteamDeckDetected)
            {
                // Enable Steam Deck mode automatically
                await EnableSteamDeckModeAsync();
            }

            // Get initial battery status
            await UpdateBatteryStatusAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Steam Deck shell");
            // Set safe defaults on failure
            IsSteamDeckDetected = false;
            BatteryStatusText = "Battery: Error";
            PerformanceModeText = "Performance: Error";
        }
    }

    private void SetupEventHandlers()
    {
        _steamDeckManager.SteamDeckModeChanged += OnSteamDeckModeChanged;
        _batteryOptimizer.BatteryStatusChanged += OnBatteryStatusChanged;
        _batteryOptimizer.LowBatteryWarning += OnLowBatteryWarning;
    }

    private void InitializeQuickActions()
    {
        QuickActions.Add(new SteamDeckQuickAction
        {
            Name = "Performance Mode",
            Icon = "⚡",
            Command = new RelayCommand(async () => await SetPerformanceModeAsync())
        });

        QuickActions.Add(new SteamDeckQuickAction
        {
            Name = "Battery Saver",
            Icon = "🔋",
            Command = new RelayCommand(async () => await SetBatterySaverModeAsync())
        });

        QuickActions.Add(new SteamDeckQuickAction
        {
            Name = "Calibrate Touch",
            Icon = "👆",
            Command = new RelayCommand(async () => await CalibrateTouchAsync())
        });

        QuickActions.Add(new SteamDeckQuickAction
        {
            Name = "Steam Input",
            Icon = "🎮",
            Command = new RelayCommand(async () => await ToggleSteamInputAsync())
        });
    }

    private async Task EnableSteamDeckModeAsync()
    {
        var result = await _steamDeckManager.EnableSteamDeckModeAsync();
        if (result.IsSuccess)
        {
            IsSteamDeckModeActive = true;
        }
    }

    private async Task UpdateBatteryStatusAsync()
    {
        var statusResult = await _batteryOptimizer.GetBatteryStatusAsync();
        if (statusResult.IsSuccess)
        {
            var status = statusResult.Value;
            BatteryPercentage = status.PercentRemaining;
            IsCharging = status.IsCharging;
            BatteryStatusText = $"{status.PercentRemaining}% {(status.IsCharging ? "⚡" : "")}";
            PerformanceModeText = $"Performance: {status.CurrentMode}";
        }
    }

    [RelayCommand]
    private async Task SetPerformanceModeAsync()
    {
        var profilesResult = await _batteryOptimizer.GetAllProfilesAsync();
        if (profilesResult.IsSuccess)
        {
            var performanceProfile = profilesResult.Value.FirstOrDefault(p => p.Mode == Core.Performance.Services.PowerMode.Performance);
            if (performanceProfile != null)
            {
                await _batteryOptimizer.ApplyProfileAsync(performanceProfile.Id);
                await UpdateBatteryStatusAsync();
            }
        }
    }

    [RelayCommand]
    private async Task SetBatterySaverModeAsync()
    {
        var profilesResult = await _batteryOptimizer.GetAllProfilesAsync();
        if (profilesResult.IsSuccess)
        {
            var powerSaverProfile = profilesResult.Value.FirstOrDefault(p => p.Mode == Core.Performance.Services.PowerMode.PowerSaver);
            if (powerSaverProfile != null)
            {
                await _batteryOptimizer.ApplyProfileAsync(powerSaverProfile.Id);
                await UpdateBatteryStatusAsync();
            }
        }
    }

    [RelayCommand]
    private async Task CalibrateTouchAsync()
    {
        var result = await _touchController.CalibrateTouchAsync();
        if (result.IsSuccess)
        {
            // Show success message
        }
    }

    [RelayCommand]
    private async Task ToggleSteamInputAsync()
    {
        // Toggle Steam Input configuration
        // This would interact with Steam's input system
        await Task.CompletedTask;
    }

    private void OnSteamDeckModeChanged(object? sender, SteamDeckModeChangedEventArgs e)
    {
        IsSteamDeckModeActive = e.IsActive;
    }

    private void OnBatteryStatusChanged(object? sender, BatteryStatusChangedEventArgs e)
    {
        BatteryPercentage = e.CurrentStatus.PercentRemaining;
        IsCharging = e.CurrentStatus.IsCharging;
        BatteryStatusText = $"{e.CurrentStatus.PercentRemaining}% {(e.CurrentStatus.IsCharging ? "⚡" : "")}";
        PerformanceModeText = $"Performance: {e.CurrentStatus.CurrentMode}";
    }

    private void OnLowBatteryWarning(object? sender, LowBatteryWarningEventArgs e)
    {
        // Show low battery warning notification
        BatteryStatusText = $"LOW BATTERY: {e.PercentRemaining}% - {e.EstimatedTime:hh\\:mm} remaining";
    }

    public void Dispose()
    {
        _steamDeckManager.SteamDeckModeChanged -= OnSteamDeckModeChanged;
        _batteryOptimizer.BatteryStatusChanged -= OnBatteryStatusChanged;
        _batteryOptimizer.LowBatteryWarning -= OnLowBatteryWarning;
    }
}

public class SteamDeckQuickAction
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public IRelayCommand Command { get; set; } = null!;
}