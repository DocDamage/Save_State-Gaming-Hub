using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.RgbSync.Models;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.RgbSync;

/// <summary>
/// View model for the RGB color picker dialog.
/// </summary>
public partial class RgbColorPickerViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private byte _red = 255;

    [ObservableProperty]
    private byte _green = 255;

    [ObservableProperty]
    private byte _blue = 255;

    [ObservableProperty]
    private string _hexValue = "#FFFFFF";

    [ObservableProperty]
    private ObservableCollection<RgbColor> _presetColors = new();

    [ObservableProperty]
    private ObservableCollection<RgbColor> _recentColors = new();

    [ObservableProperty]
    private double _hue = 0;

    [ObservableProperty]
    private double _saturation = 0;

    [ObservableProperty]
    private double _value = 1.0;

    public RgbColor SelectedColor => new(Red, Green, Blue);

    public RgbColorPickerViewModel(IDialogService dialogService)
    {
        _dialogService = dialogService;
        InitializePresets();
        InitializeRecentColors();
    }

    private void InitializePresets()
    {
        // Standard colors
        PresetColors.Add(RgbColor.White);
        PresetColors.Add(RgbColor.Black);
        PresetColors.Add(RgbColor.Red);
        PresetColors.Add(RgbColor.Green);
        PresetColors.Add(RgbColor.Blue);
        PresetColors.Add(RgbColor.Yellow);
        PresetColors.Add(RgbColor.Cyan);
        PresetColors.Add(RgbColor.Magenta);
        PresetColors.Add(RgbColor.Orange);
        PresetColors.Add(RgbColor.Purple);

        // Extended palette
        PresetColors.Add(new RgbColor(255, 128, 128)); // Light Red
        PresetColors.Add(new RgbColor(128, 255, 128)); // Light Green
        PresetColors.Add(new RgbColor(128, 128, 255)); // Light Blue
        PresetColors.Add(new RgbColor(255, 255, 128)); // Light Yellow
        PresetColors.Add(new RgbColor(255, 128, 255)); // Light Magenta
        PresetColors.Add(new RgbColor(128, 255, 255)); // Light Cyan

        // Gaming themed
        PresetColors.Add(new RgbColor(255, 0, 128));   // Neon Pink
        PresetColors.Add(new RgbColor(0, 255, 128));   // Neon Mint
        PresetColors.Add(new RgbColor(255, 128, 0));   // Neon Orange
        PresetColors.Add(new RgbColor(128, 0, 255));   // Neon Purple
        PresetColors.Add(new RgbColor(0, 128, 255));   // Neon Blue
        PresetColors.Add(new RgbColor(255, 215, 0));   // Gold
    }

    private void InitializeRecentColors()
    {
        // Load from preferences or use defaults
        RecentColors.Add(new RgbColor(255, 0, 0));
        RecentColors.Add(new RgbColor(0, 255, 0));
        RecentColors.Add(new RgbColor(0, 0, 255));
        RecentColors.Add(new RgbColor(255, 255, 0));
        RecentColors.Add(new RgbColor(255, 0, 255));
        RecentColors.Add(new RgbColor(0, 255, 255));
    }

    partial void OnRedChanged(byte value) => UpdateHexFromRgb();
    partial void OnGreenChanged(byte value) => UpdateHexFromRgb();
    partial void OnBlueChanged(byte value) => UpdateHexFromRgb();

    partial void OnHexValueChanged(string value)
    {
        if (value.Length == 7 && value.StartsWith("#"))
        {
            try
            {
                var color = RgbColor.FromHex(value);
                Red = color.R;
                Green = color.G;
                Blue = color.B;
            }
            catch
            {
                // Invalid hex, ignore
            }
        }
    }

    private void UpdateHexFromRgb()
    {
        HexValue = $"#{Red:X2}{Green:X2}{Blue:X2}";
    }

    [RelayCommand]
    private void SelectPreset(RgbColor? color)
    {
        if (color == null) return;
        Red = color.R;
        Green = color.G;
        Blue = color.B;
    }

    [RelayCommand]
    private void SelectRecent(RgbColor? color)
    {
        if (color == null) return;
        Red = color.R;
        Green = color.G;
        Blue = color.B;
    }

    [RelayCommand]
    private void ApplyColor()
    {
        AddToRecent(SelectedColor);
        _dialogService.CloseDialog(SelectedColor);
    }

    [RelayCommand]
    private void Cancel()
    {
        _dialogService.CloseDialog(null);
    }

    private void AddToRecent(RgbColor color)
    {
        // Remove if already exists
        var existing = RecentColors.FirstOrDefault(c => c.R == color.R && c.G == color.G && c.B == color.B);
        if (existing != null)
        {
            RecentColors.Remove(existing);
        }

        // Add to front
        RecentColors.Insert(0, color);

        // Keep only last 12
        while (RecentColors.Count > 12)
        {
            RecentColors.RemoveAt(RecentColors.Count - 1);
        }
    }

    [RelayCommand]
    private void UpdateFromWheel(double angle, double distance)
    {
        // Convert polar to HSV
        Hue = angle;
        Saturation = Math.Min(distance, 1.0);

        // Convert HSV to RGB
        var color = HsvToRgb(Hue, Saturation, Value);
        Red = color.R;
        Green = color.G;
        Blue = color.B;
    }

    private static RgbColor HsvToRgb(double hue, double saturation, double value)
    {
        double h = hue / 360.0;
        double s = saturation;
        double v = value;

        int i = (int)(h * 6);
        double f = h * 6 - i;
        double p = v * (1 - s);
        double q = v * (1 - f * s);
        double t = v * (1 - (1 - f) * s);

        double r, g, b;
        switch (i % 6)
        {
            case 0: r = v; g = t; b = p; break;
            case 1: r = q; g = v; b = p; break;
            case 2: r = p; g = v; b = t; break;
            case 3: r = p; g = q; b = v; break;
            case 4: r = t; g = p; b = v; break;
            default: r = v; g = p; b = q; break;
        }

        return new RgbColor(
            (byte)(r * 255),
            (byte)(g * 255),
            (byte)(b * 255)
        );
    }
}
