using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using SaveState.Core.RgbSync.Models;

namespace SaveState.Presentation.ViewModels.RgbSync;

/// <summary>
/// Converts RgbColor to SolidColorBrush.
/// </summary>
public class ColorToBrushConverter : IValueConverter
{
    public static ColorToBrushConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is RgbColor color)
        {
            return new SolidColorBrush(Color.FromRgb(color.R, color.G, color.B));
        }
        return new SolidColorBrush(Colors.Transparent);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SolidColorBrush brush)
        {
            return new RgbColor(brush.Color.R, brush.Color.G, brush.Color.B);
        }
        return RgbColor.Black;
    }
}

/// <summary>
/// Converts RgbColor to HEX string.
/// </summary>
public class ColorToHexConverter : IValueConverter
{
    public static ColorToHexConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is RgbColor color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }
        return "#000000";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex)
        {
            return RgbColor.FromHex(hex);
        }
        return RgbColor.Black;
    }
}

/// <summary>
/// Converts RgbColor to RGB text representation.
/// </summary>
public class ColorToRgbTextConverter : IValueConverter
{
    public static ColorToRgbTextConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is RgbColor color)
        {
            return $"RGB({color.R}, {color.G}, {color.B})";
        }
        return "RGB(0, 0, 0)";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts boolean to status text (On/Off).
/// </summary>
public class ToggleTextConverter : IValueConverter
{
    public static ToggleTextConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isEnabled && parameter is string param)
        {
            var parts = param.Split('|');
            return isEnabled ? parts[0] : parts[1];
        }
        return "Off";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts boolean to brush (Green for true, Red for false).
/// </summary>
public class BoolToBrushConverter : IValueConverter
{
    public static BoolToBrushConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isAvailable)
        {
            return new SolidColorBrush(isAvailable ? Colors.Green : Colors.Red);
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts boolean to text for preview button.
/// </summary>
public class BoolToTextConverter : IValueConverter
{
    public static BoolToTextConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive && parameter is string param)
        {
            var parts = param.Split('|');
            return isActive ? parts[0] : parts[1];
        }
        return "Preview";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts device type to icon/emoji.
/// </summary>
public class DeviceIconConverter : IValueConverter
{
    public static DeviceIconConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is RgbDeviceType type)
        {
            return type switch
            {
                RgbDeviceType.Keyboard => "⌨️",
                RgbDeviceType.Mouse => "🖱️",
                RgbDeviceType.Headset => "🎧",
                RgbDeviceType.Mousepad => "🖱️",
                RgbDeviceType.Gpu => "🖥️",
                RgbDeviceType.Motherboard => "🔌",
                RgbDeviceType.LedStrip => "💡",
                RgbDeviceType.Fan => "🌀",
                RgbDeviceType.Memory => "💾",
                RgbDeviceType.Case => "🖥️",
                _ => "💡"
            };
        }
        return "💡";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts selected device to background brush.
/// </summary>
public class DeviceSelectionConverter : IValueConverter
{
    public static DeviceSelectionConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is RgbDevice selectedDevice && parameter is RgbDevice currentDevice)
        {
            return selectedDevice.Id == currentDevice.Id
                ? new SolidColorBrush(Color.FromArgb(32, 16, 185, 129)) // #10B981 with 20% opacity
                : null;
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts trigger type to display name.
/// </summary>
public class TriggerDisplayNameConverter : IValueConverter
{
    public static TriggerDisplayNameConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is GameStateRgbTrigger trigger)
        {
            return trigger switch
            {
                GameStateRgbTrigger.HealthLow => "Health Low",
                GameStateRgbTrigger.HealthCritical => "Health Critical",
                GameStateRgbTrigger.ManaLow => "Mana Low",
                GameStateRgbTrigger.LevelUp => "Level Up",
                GameStateRgbTrigger.AchievementUnlocked => "Achievement Unlocked",
                GameStateRgbTrigger.SaveStateCreated => "Save State Created",
                GameStateRgbTrigger.BossEncounter => "Boss Encounter",
                GameStateRgbTrigger.GameOver => "Game Over",
                GameStateRgbTrigger.Victory => "Victory",
                GameStateRgbTrigger.Loading => "Loading",
                GameStateRgbTrigger.Menu => "In Menu",
                GameStateRgbTrigger.Playing => "Playing",
                _ => trigger.ToString()
            };
        }
        return value?.ToString() ?? "";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts trigger type to icon.
/// </summary>
public class TriggerIconConverter : IValueConverter
{
    public static TriggerIconConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is GameStateRgbTrigger trigger)
        {
            return trigger switch
            {
                GameStateRgbTrigger.HealthLow => "💛",
                GameStateRgbTrigger.HealthCritical => "❤️",
                GameStateRgbTrigger.ManaLow => "💙",
                GameStateRgbTrigger.LevelUp => "⬆️",
                GameStateRgbTrigger.AchievementUnlocked => "🏆",
                GameStateRgbTrigger.SaveStateCreated => "💾",
                GameStateRgbTrigger.BossEncounter => "👹",
                GameStateRgbTrigger.GameOver => "💀",
                GameStateRgbTrigger.Victory => "🎉",
                GameStateRgbTrigger.Loading => "⏳",
                GameStateRgbTrigger.Menu => "📋",
                GameStateRgbTrigger.Playing => "🎮",
                _ => "⚡"
            };
        }
        return "⚡";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Checks if trigger is currently being previewed.
/// </summary>
public class TriggerActiveConverter : IValueConverter
{
    public static TriggerActiveConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is GameStateRgbTrigger previewingTrigger && parameter is GameStateRgbTrigger currentTrigger)
        {
            return previewingTrigger == currentTrigger;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Provides static values for RgbEffectType enum.
/// </summary>
public static class EffectTypeValues
{
    public static IEnumerable<RgbEffectType> Values => Enum.GetValues<RgbEffectType>();
}

/// <summary>
/// Provides static values for RgbDirection enum.
/// </summary>
public static class DirectionValues
{
    public static IEnumerable<RgbDirection> Values => Enum.GetValues<RgbDirection>();
}

/// <summary>
/// Static color reference for white in XAML bindings.
/// </summary>
public static class RGBWhite
{
    public static RgbColor Value => RgbColor.White;
}
