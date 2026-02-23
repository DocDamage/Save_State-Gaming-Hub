using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.RgbSync.Models;
using SaveState.Core.RgbSync.Services;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.RgbSync;

/// <summary>
/// View model for individual RGB device configuration.
/// </summary>
public partial class RgbDeviceEditorViewModel : ObservableObject
{
    private readonly IRgbSyncService _rgbService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<RgbDeviceEditorViewModel> _logger;

    [ObservableProperty]
    private RgbDevice? _device;

    [ObservableProperty]
    private ObservableCollection<RgbZoneViewModel> _zones = new();

    [ObservableProperty]
    private ObservableCollection<RgbLedViewModel> _leds = new();

    [ObservableProperty]
    private RgbZoneViewModel? _selectedZone;

    [ObservableProperty]
    private RgbLedViewModel? _selectedLed;

    [ObservableProperty]
    private RgbColor _zoneColor = RgbColor.White;

    [ObservableProperty]
    private bool _isDirectModeEnabled;

    [ObservableProperty]
    private bool _showLedMap = true;

    [ObservableProperty]
    private int _brightness = 100;

    [ObservableProperty]
    private string _deviceStatus = string.Empty;

    [ObservableProperty]
    private ObservableCollection<RgbEffect> _availableEffects = new();

    [ObservableProperty]
    private RgbEffect? _selectedEffect;

    public RgbDeviceEditorViewModel(
        IRgbSyncService rgbService,
        IDialogService dialogService,
        ILogger<RgbDeviceEditorViewModel> logger)
    {
        _rgbService = rgbService;
        _dialogService = dialogService;
        _logger = logger;

        InitializeEffects();
    }

    partial void OnDeviceChanged(RgbDevice? value)
    {
        if (value != null)
        {
            LoadDeviceData();
        }
        else
        {
            Zones.Clear();
            Leds.Clear();
        }
    }

    private void InitializeEffects()
    {
        AvailableEffects.Add(new RgbEffect { Name = "Static", Type = RgbEffectType.Static });
        AvailableEffects.Add(new RgbEffect { Name = "Breathing", Type = RgbEffectType.Breathing });
        AvailableEffects.Add(new RgbEffect { Name = "Rainbow", Type = RgbEffectType.Rainbow });
        AvailableEffects.Add(new RgbEffect { Name = "Wave", Type = RgbEffectType.Wave });
        AvailableEffects.Add(new RgbEffect { Name = "Reactive", Type = RgbEffectType.Reactive });
        AvailableEffects.Add(new RgbEffect { Name = "Spectrum Cycle", Type = RgbEffectType.SpectrumCycle });
    }

    private void LoadDeviceData()
    {
        if (Device == null) return;

        DeviceStatus = Device.IsConnected ? "Connected" : "Disconnected";
        IsDirectModeEnabled = Device.SupportsDirectMode;

        // Load zones
        Zones.Clear();
        foreach (var zone in Device.Zones)
        {
            Zones.Add(new RgbZoneViewModel
            {
                Name = zone.Name,
                StartLedIndex = zone.StartLedIndex,
                LedCount = zone.LedCount,
                Color = RgbColor.White
            });
        }

        // Load LEDs
        Leds.Clear();
        for (int i = 0; i < Device.LedCount; i++)
        {
            var led = Device.Leds.FirstOrDefault(l => l.Index == i);
            Leds.Add(new RgbLedViewModel
            {
                Index = i,
                Name = led?.Name ?? $"LED {i + 1}",
                Color = led?.Color ?? RgbColor.Black,
                X = (i % 10) * 20, // Simple grid layout
                Y = (i / 10) * 20
            });
        }
    }

    [RelayCommand]
    private async Task ApplyZoneColorAsync()
    {
        if (SelectedZone == null || Device == null) return;

        try
        {
            var ledColors = new Dictionary<int, RgbColor>();
            for (int i = SelectedZone.StartLedIndex; i < SelectedZone.StartLedIndex + SelectedZone.LedCount; i++)
            {
                if (i < Leds.Count)
                {
                    ledColors[i] = ZoneColor;
                    Leds[i].Color = ZoneColor;
                }
            }

            var result = await _rgbService.SetDeviceLedsAsync(Device.Id, ledColors, CancellationToken.None);
            if (result.IsSuccess)
            {
                SelectedZone.Color = ZoneColor;
                DeviceStatus = "Zone color applied";
            }
            else
            {
                DeviceStatus = $"Failed: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying zone color");
            DeviceStatus = "Error applying color";
        }
    }

    [RelayCommand]
    private async Task ApplyEffectToZoneAsync()
    {
        if (SelectedZone == null || Device == null || SelectedEffect == null) return;

        try
        {
            var result = await _rgbService.SetDeviceEffectAsync(Device.Id, SelectedEffect, CancellationToken.None);
            if (result.IsSuccess)
            {
                DeviceStatus = $"Effect applied to {SelectedZone.Name}";
            }
            else
            {
                DeviceStatus = $"Failed: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying zone effect");
            DeviceStatus = "Error applying effect";
        }
    }

    [RelayCommand]
    private async Task PickColorForZoneAsync()
    {
        if (SelectedZone == null) return;

        var dialog = new RgbColorPickerViewModel(_dialogService);
        var result = await _dialogService.ShowDialogAsync<RgbColor?>(dialog);

        if (result != null)
        {
            ZoneColor = result;
        }
    }

    [RelayCommand]
    private async Task PickColorForLedAsync(RgbLedViewModel? led)
    {
        if (led == null || Device == null) return;

        var dialog = new RgbColorPickerViewModel(_dialogService);
        var result = await _dialogService.ShowDialogAsync<RgbColor?>(dialog);

        if (result != null)
        {
            led.Color = result;
            var ledColors = new Dictionary<int, RgbColor> { [led.Index] = result };
            await _rgbService.SetDeviceLedsAsync(Device.Id, ledColors, CancellationToken.None);
        }
    }

    [RelayCommand]
    private async Task SetAllLedsAsync(RgbColor? color)
    {
        if (Device == null || color == null) return;

        try
        {
            var result = await _rgbService.SetDeviceColorAsync(Device.Id, color, CancellationToken.None);
            if (result.IsSuccess)
            {
                foreach (var led in Leds)
                {
                    led.Color = color;
                }
                DeviceStatus = "All LEDs updated";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting all LEDs");
            DeviceStatus = "Error updating LEDs";
        }
    }

    [RelayCommand]
    private async Task ToggleDirectModeAsync()
    {
        if (Device == null) return;

        IsDirectModeEnabled = !IsDirectModeEnabled;
        DeviceStatus = IsDirectModeEnabled ? "Direct mode enabled" : "Direct mode disabled";
    }

    [RelayCommand]
    private void ClearAllLeds()
    {
        foreach (var led in Leds)
        {
            led.Color = RgbColor.Black;
        }
    }

    [RelayCommand]
    private async Task SaveDeviceConfigurationAsync()
    {
        // Save device-specific configuration
        DeviceStatus = "Configuration saved";
    }
}

/// <summary>
/// View model for an RGB zone.
/// </summary>
public partial class RgbZoneViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private int _startLedIndex;

    [ObservableProperty]
    private int _ledCount;

    [ObservableProperty]
    private RgbColor _color = RgbColor.White;
}

/// <summary>
/// View model for an individual RGB LED.
/// </summary>
public partial class RgbLedViewModel : ObservableObject
{
    [ObservableProperty]
    private int _index;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private RgbColor _color = RgbColor.Black;

    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    [ObservableProperty]
    private bool _isSelected;
}
