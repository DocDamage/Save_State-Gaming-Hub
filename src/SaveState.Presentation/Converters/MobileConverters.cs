using Avalonia.Data.Converters;
using Avalonia.Media;
using SaveState.Core.Common.Services;
using SaveState.Presentation.Models.Mobile;
using System.Globalization;

namespace SaveState.Presentation.Converters;

/// <summary>
/// Converts device type to emoji icon
/// </summary>
public class DeviceTypeToEmojiConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string deviceType)
            return "📱";

        return deviceType.ToLowerInvariant() switch
        {
            var t when t.Contains("iphone") => "📱",
            var t when t.Contains("ipad") => "📱",
            var t when t.Contains("tablet") => "📱",
            var t when t.Contains("android") => "📱",
            var t when t.Contains("pixel") => "📱",
            var t when t.Contains("samsung") => "📱",
            var t when t.Contains("watch") => "⌚",
            var t when t.Contains("tv") => "📺",
            _ => "📱"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}


/// <summary>
/// Converts DateTime to "time ago" string
/// </summary>
public class TimeAgoConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTime dateTime)
            return "Unknown";

        var now = SystemTimeProvider.Instance.Now;
        var diff = now - dateTime;

        return diff.TotalSeconds switch
        {
            < 60 => "Just now",
            < 120 => "1 minute ago",
            < 3600 => $"{(int)diff.TotalMinutes} minutes ago",
            < 7200 => "1 hour ago",
            < 86400 => $"{(int)diff.TotalHours} hours ago",
            < 172800 => "Yesterday",
            < 604800 => $"{(int)diff.TotalDays} days ago",
            < 2592000 => $"{(int)(diff.TotalDays / 7)} weeks ago",
            < 31536000 => $"{(int)(diff.TotalDays / 30)} months ago",
            _ => $"{(int)(diff.TotalDays / 365)} years ago"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts file size in bytes to human-readable string
/// </summary>
public class FileSizeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not long bytes)
            return "0 B";

        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        var counter = 0;
        decimal number = bytes;

        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }

        return $"{number:n1} {suffixes[counter]}";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts notification type to emoji icon
/// </summary>
public class NotificationTypeToEmojiConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string type)
            return "🔔";

        return type.ToLowerInvariant() switch
        {
            "achievement" => "🏆",
            "savestate" => "💾",
            "game" => "🎮",
            "system" => "⚙️",
            "update" => "⬆️",
            "friend" => "👤",
            "message" => "💬",
            "error" => "⚠️",
            "warning" => "⚡",
            _ => "🔔"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts enum value to boolean for visibility
/// </summary>
public class EnumToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null || parameter is null)
            return false;

        return value.ToString()?.Equals(parameter.ToString(), StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not true || parameter is null)
            return null;

        // This would need to know the enum type to convert back
        // For now, return the parameter as-is
        return parameter;
    }
}

/// <summary>
/// Converts CPU usage percentage to color brush
/// </summary>
public class CpuUsageToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double usage)
            return new SolidColorBrush(Colors.Gray);

        return usage switch
        {
            < 50 => new SolidColorBrush(Color.Parse("#10B981")), // Green
            < 80 => new SolidColorBrush(Color.Parse("#F59E0B")), // Yellow
            _ => new SolidColorBrush(Color.Parse("#EF4444"))      // Red
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts RAM usage percentage to color brush
/// </summary>
public class RamUsageToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double usage)
            return new SolidColorBrush(Colors.Gray);

        return usage switch
        {
            < 60 => new SolidColorBrush(Color.Parse("#10B981")), // Green
            < 85 => new SolidColorBrush(Color.Parse("#F59E0B")), // Yellow
            _ => new SolidColorBrush(Color.Parse("#EF4444"))      // Red
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts temperature to color brush
/// </summary>
public class TemperatureToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double temp)
            return new SolidColorBrush(Colors.Gray);

        return temp switch
        {
            < 60 => new SolidColorBrush(Color.Parse("#10B981")), // Green
            < 80 => new SolidColorBrush(Color.Parse("#F59E0B")), // Yellow
            _ => new SolidColorBrush(Color.Parse("#EF4444"))      // Red
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}


