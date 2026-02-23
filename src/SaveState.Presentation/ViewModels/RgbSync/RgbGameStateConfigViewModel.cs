using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Core.RgbSync.Models;
using SaveState.Core.RgbSync.Services;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.RgbSync;

/// <summary>
/// View model for configuring RGB triggers for game events.
/// </summary>
public partial class RgbGameStateConfigViewModel : ObservableObject
{
    private readonly IRgbSyncService _rgbService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<RgbGameStateConfigViewModel> _logger;
    private readonly ITimeProvider _timeProvider;

    [ObservableProperty]
    private ObservableCollection<GameStateRgbConfig> _gameStateConfigs = new();

    [ObservableProperty]
    private GameStateRgbConfig? _selectedConfig;

    [ObservableProperty]
    private ObservableCollection<RgbDevice> _availableDevices = new();

    [ObservableProperty]
    private bool _isHealthIndicatorEnabled = true;

    [ObservableProperty]
    private int _healthCriticalThreshold = 25;

    [ObservableProperty]
    private int _healthLowThreshold = 50;

    [ObservableProperty]
    private RgbColor _healthCriticalColor = RgbColor.Red;

    [ObservableProperty]
    private RgbColor _healthLowColor = RgbColor.Yellow;

    [ObservableProperty]
    private RgbColor _healthNormalColor = RgbColor.Green;

    [ObservableProperty]
    private bool _isAchievementEffectEnabled = true;

    [ObservableProperty]
    private bool _isLevelUpEffectEnabled = true;

    [ObservableProperty]
    private bool _isSaveStateEffectEnabled = true;

    [ObservableProperty]
    private bool _isBossEncounterEffectEnabled = true;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isPreviewing;

    [ObservableProperty]
    private GameStateRgbTrigger? _previewingTrigger;

    public RgbGameStateConfigViewModel(
        IRgbSyncService rgbService,
        IDialogService dialogService,
        ILogger<RgbGameStateConfigViewModel> logger,
        ITimeProvider timeProvider)
    {
        _rgbService = rgbService;
        _dialogService = dialogService;
        _logger = logger;
        _timeProvider = timeProvider;

        InitializeDefaultConfigs();
        LoadDevices();
    }

    private void InitializeDefaultConfigs()
    {
        GameStateConfigs.Add(new GameStateRgbConfig
        {
            Trigger = GameStateRgbTrigger.HealthLow,
            Effect = new RgbEffect
            {
                Name = "Low Health",
                Type = RgbEffectType.Pulse,
                Colors = new List<RgbColor> { RgbColor.Red, RgbColor.Black },
                Speed = 2.0f,
                DurationMs = 3000
            },
            DurationMs = 3000,
            Priority = 3
        });

        GameStateConfigs.Add(new GameStateRgbConfig
        {
            Trigger = GameStateRgbTrigger.HealthCritical,
            Effect = new RgbEffect
            {
                Name = "Critical Health",
                Type = RgbEffectType.Flashing,
                Colors = new List<RgbColor> { RgbColor.Red },
                Speed = 3.0f,
                DurationMs = 5000
            },
            DurationMs = 5000,
            Priority = 5
        });

        GameStateConfigs.Add(new GameStateRgbConfig
        {
            Trigger = GameStateRgbTrigger.AchievementUnlocked,
            Effect = new RgbEffect
            {
                Name = "Achievement",
                Type = RgbEffectType.Flashing,
                Colors = new List<RgbColor> { RgbColor.Yellow, RgbColor.Gold },
                Speed = 1.5f,
                DurationMs = 5000
            },
            DurationMs = 5000,
            Priority = 4
        });

        GameStateConfigs.Add(new GameStateRgbConfig
        {
            Trigger = GameStateRgbTrigger.LevelUp,
            Effect = new RgbEffect
            {
                Name = "Level Up",
                Type = RgbEffectType.Rainbow,
                Colors = new List<RgbColor> { RgbColor.Red, RgbColor.Green, RgbColor.Blue },
                Speed = 1.0f,
                DurationMs = 10000
            },
            DurationMs = 10000,
            Priority = 2
        });

        GameStateConfigs.Add(new GameStateRgbConfig
        {
            Trigger = GameStateRgbTrigger.SaveStateCreated,
            Effect = new RgbEffect
            {
                Name = "Save State",
                Type = RgbEffectType.Breathing,
                Colors = new List<RgbColor> { RgbColor.Cyan },
                Speed = 1.0f,
                DurationMs = 2000
            },
            DurationMs = 2000,
            Priority = 1
        });

        GameStateConfigs.Add(new GameStateRgbConfig
        {
            Trigger = GameStateRgbTrigger.BossEncounter,
            Effect = new RgbEffect
            {
                Name = "Boss Fight",
                Type = RgbEffectType.Wave,
                Colors = new List<RgbColor> { RgbColor.Red, RgbColor.Orange },
                Speed = 1.5f,
                DurationMs = 0 // Until boss defeated
            },
            DurationMs = 0,
            Interruptible = false,
            Priority = 5
        });

        GameStateConfigs.Add(new GameStateRgbConfig
        {
            Trigger = GameStateRgbTrigger.Victory,
            Effect = new RgbEffect
            {
                Name = "Victory",
                Type = RgbEffectType.Starlight,
                Colors = new List<RgbColor> { RgbColor.Gold, RgbColor.Yellow, RgbColor.White },
                Speed = 0.5f,
                DurationMs = 8000
            },
            DurationMs = 8000,
            Priority = 4
        });

        GameStateConfigs.Add(new GameStateRgbConfig
        {
            Trigger = GameStateRgbTrigger.GameOver,
            Effect = new RgbEffect
            {
                Name = "Game Over",
                Type = RgbEffectType.Breathing,
                Colors = new List<RgbColor> { RgbColor.Purple, RgbColor.Red },
                Speed = 0.5f,
                DurationMs = 5000
            },
            DurationMs = 5000,
            Priority = 3
        });
    }

    private async void LoadDevices()
    {
        try
        {
            var result = await _rgbService.GetDevicesAsync(CancellationToken.None);
            if (result.IsSuccess)
            {
                AvailableDevices.Clear();
                foreach (var device in result.Value)
                {
                    if (device.IsConnected)
                    {
                        AvailableDevices.Add(device);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading devices");
        }
    }

    [RelayCommand]
    private async Task EditConfigAsync(GameStateRgbConfig? config)
    {
        if (config == null) return;

        SelectedConfig = config;

        // Show effect editor for this config
        var dialog = new RgbColorPickerViewModel(_dialogService);
        var result = await _dialogService.ShowDialogAsync<RgbColor?>(dialog);

        if (result != null && config.Effect.Colors.Count > 0)
        {
            config.Effect.Colors[0] = result;
            StatusMessage = $"Updated {config.Trigger} effect";
        }
    }

    [RelayCommand]
    private async Task PreviewEffectAsync(GameStateRgbConfig? config)
    {
        if (config == null) return;

        try
        {
            IsPreviewing = true;
            PreviewingTrigger = config.Trigger;

            // Trigger the game state effect
            await _rgbService.TriggerGameStateEffectAsync(config.Trigger, CancellationToken.None);

            StatusMessage = $"Previewing {config.Trigger} effect...";

            // Auto-stop preview after duration
            if (config.DurationMs > 0)
            {
                await Task.Delay(config.DurationMs);
                IsPreviewing = false;
                PreviewingTrigger = null;
                StatusMessage = "Preview complete";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing effect");
            StatusMessage = "Error previewing effect";
            IsPreviewing = false;
            PreviewingTrigger = null;
        }
    }

    [RelayCommand]
    private void StopPreview()
    {
        IsPreviewing = false;
        PreviewingTrigger = null;
        StatusMessage = "Preview stopped";
    }

    [RelayCommand]
    private void AddCustomTrigger()
    {
        var config = new GameStateRgbConfig
        {
            Trigger = GameStateRgbTrigger.Menu,
            Effect = new RgbEffect
            {
                Name = "Custom Trigger",
                Type = RgbEffectType.Static,
                Colors = new List<RgbColor> { RgbColor.White },
                DurationMs = 1000
            },
            DurationMs = 1000
        };

        GameStateConfigs.Add(config);
        SelectedConfig = config;
        StatusMessage = "Custom trigger added";
    }

    [RelayCommand]
    private async Task RemoveConfigAsync(GameStateRgbConfig? config)
    {
        if (config == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Remove Trigger",
            $"Remove {config.Trigger} trigger configuration?");

        if (confirmed)
        {
            GameStateConfigs.Remove(config);
            StatusMessage = $"{config.Trigger} trigger removed";
        }
    }

    [RelayCommand]
    private async Task SaveConfigurationAsync()
    {
        try
        {
            // Configure all game state effects
            await _rgbService.ConfigureGameStateEffectsAsync(GameStateConfigs.ToList(), CancellationToken.None);

            StatusMessage = "Configuration saved";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving configuration");
            StatusMessage = "Error saving configuration";
        }
    }

    [RelayCommand]
    private async Task PickHealthColorAsync(string type)
    {
        var dialog = new RgbColorPickerViewModel(_dialogService);
        var result = await _dialogService.ShowDialogAsync<RgbColor?>(dialog);

        if (result == null) return;

        switch (type)
        {
            case "critical":
                HealthCriticalColor = result;
                break;
            case "low":
                HealthLowColor = result;
                break;
            case "normal":
                HealthNormalColor = result;
                break;
        }
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        GameStateConfigs.Clear();
        InitializeDefaultConfigs();
        StatusMessage = "Reset to defaults";
    }

    [RelayCommand]
    private async Task ImportConfigAsync()
    {
        var filePath = await _dialogService.ShowOpenFileDialogAsync(
            "Import Configuration",
            new[] { "json" });

        if (string.IsNullOrWhiteSpace(filePath)) return;

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var configs = System.Text.Json.JsonSerializer.Deserialize<List<GameStateRgbConfig>>(json);

            if (configs != null)
            {
                GameStateConfigs.Clear();
                foreach (var config in configs)
                {
                    GameStateConfigs.Add(config);
                }
                StatusMessage = "Configuration imported";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing configuration");
            StatusMessage = "Error importing configuration";
        }
    }

    [RelayCommand]
    private async Task ExportConfigAsync()
    {
        var filePath = await _dialogService.ShowSaveFileDialogAsync(
            "Export Configuration",
            new[] { "json" },
            "GameStateRgbConfig.json");

        if (string.IsNullOrWhiteSpace(filePath)) return;

        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                GameStateConfigs.ToList(),
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

            await File.WriteAllTextAsync(filePath, json);
            StatusMessage = "Configuration exported";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting configuration");
            StatusMessage = "Error exporting configuration";
        }
    }
}
