using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using SaveState.Core.Common.Services;
using SettingsHealth = SaveState.Presentation.ViewModels.Settings;

namespace SaveState.Presentation.Converters;

/// <summary>
/// Converts HealthStatus enum values to appropriate brushes.
/// </summary>
public class HealthStatusToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Handle the Settings.ViewModels.Settings HealthStatus (Healthy, Warning, Critical)
        if (value is SettingsHealth.HealthStatus status)
        {
            return status switch
            {
                SettingsHealth.HealthStatus.Healthy => new SolidColorBrush(Color.Parse("#28A745")),    // Green
                SettingsHealth.HealthStatus.Warning => new SolidColorBrush(Color.Parse("#FFC107")),    // Yellow/Orange
                SettingsHealth.HealthStatus.Critical => new SolidColorBrush(Color.Parse("#DC3545")),   // Red
                _ => new SolidColorBrush(Color.Parse("#6C757D"))                                      // Gray
            };
        }

        // Also handle the Models.Health.HealthStatus enum (Healthy, Degraded, Unhealthy, Unknown)
        if (value is Models.Health.HealthStatus modelStatus)
        {
            return modelStatus switch
            {
                Models.Health.HealthStatus.Healthy => new SolidColorBrush(Color.Parse("#28A745")),
                Models.Health.HealthStatus.Degraded => new SolidColorBrush(Color.Parse("#FFC107")),
                Models.Health.HealthStatus.Unhealthy => new SolidColorBrush(Color.Parse("#DC3545")),
                Models.Health.HealthStatus.Unknown => new SolidColorBrush(Color.Parse("#6C757D")),
                _ => new SolidColorBrush(Color.Parse("#6C757D"))
            };
        }

        return new SolidColorBrush(Color.Parse("#6C757D"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts HealthStatus enum values to display text.
/// </summary>
public class HealthStatusToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SettingsHealth.HealthStatus status)
        {
            return status switch
            {
                SettingsHealth.HealthStatus.Healthy => "Healthy",
                SettingsHealth.HealthStatus.Warning => "Warning",
                SettingsHealth.HealthStatus.Critical => "Critical",
                _ => "Unknown"
            };
        }

        if (value is Models.Health.HealthStatus modelStatus)
        {
            return modelStatus switch
            {
                Models.Health.HealthStatus.Healthy => "Healthy",
                Models.Health.HealthStatus.Degraded => "Degraded",
                Models.Health.HealthStatus.Unhealthy => "Unhealthy",
                Models.Health.HealthStatus.Unknown => "Unknown",
                _ => "Unknown"
            };
        }

        return "Unknown";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts byte values to megabytes.
/// </summary>
public class ByteToMegabytesConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is long bytes)
        {
            return bytes / (1024.0 * 1024.0);
        }
        if (value is int intBytes)
        {
            return intBytes / (1024.0 * 1024.0);
        }
        return 0.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts byte values to gigabytes.
/// </summary>
public class ByteToGigabytesConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is long bytes)
        {
            return bytes / (1024.0 * 1024.0 * 1024.0);
        }
        if (value is int intBytes)
        {
            return intBytes / (1024.0 * 1024.0 * 1024.0);
        }
        return 0.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts byte values to human readable format (B, KB, MB, GB, TB).
/// </summary>
public class BytesToHumanReadableConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        long bytes = value switch
        {
            long l => l,
            int i => i,
            _ => 0
        };

        if (bytes < 0) return "0 B";
        if (bytes == 0) return "0 B";

        string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB" };
        int counter = 0;
        decimal number = bytes;

        while (Math.Round(number / 1024) >= 1 && counter < suffixes.Length - 1)
        {
            number /= 1024;
            counter++;
        }

        return $"{number:n1} {suffixes[counter]}";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts ErrorSeverity to appropriate brushes.
/// </summary>
public class ErrorSeverityToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ErrorSeverity severity)
        {
            return severity switch
            {
                ErrorSeverity.Info => new SolidColorBrush(Color.Parse("#17A2B8")),      // Cyan
                ErrorSeverity.Warning => new SolidColorBrush(Color.Parse("#FFC107")),   // Yellow
                ErrorSeverity.Error => new SolidColorBrush(Color.Parse("#DC3545")),     // Red
                ErrorSeverity.Critical => new SolidColorBrush(Color.Parse("#721C24")),  // Dark Red
                _ => new SolidColorBrush(Color.Parse("#6C757D"))
            };
        }

        // Handle string severity values
        if (value is string severityStr)
        {
            return severityStr.ToLowerInvariant() switch
            {
                "info" => new SolidColorBrush(Color.Parse("#17A2B8")),
                "warning" => new SolidColorBrush(Color.Parse("#FFC107")),
                "error" => new SolidColorBrush(Color.Parse("#DC3545")),
                "critical" => new SolidColorBrush(Color.Parse("#721C24")),
                _ => new SolidColorBrush(Color.Parse("#6C757D"))
            };
        }

        return new SolidColorBrush(Color.Parse("#6C757D"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts a DateTime to relative time (e.g., "2 hours ago").
/// </summary>
public class RelativeTimeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTime dt)
        {
            if (value is DateTimeOffset dto)
            {
                dt = dto.DateTime;
            }
            else
            {
                return "Never";
            }
        }

        var now = SystemTimeProvider.Instance.Now;
        var diff = now - dt;

        if (diff.TotalSeconds < 60)
            return "Just now";
        if (diff.TotalMinutes < 2)
            return "1 minute ago";
        if (diff.TotalMinutes < 60)
            return $"{diff.Minutes} minutes ago";
        if (diff.TotalHours < 2)
            return "1 hour ago";
        if (diff.TotalHours < 24)
            return $"{diff.Hours} hours ago";
        if (diff.TotalDays < 2)
            return "Yesterday";
        if (diff.TotalDays < 30)
            return $"{diff.Days} days ago";
        if (diff.TotalDays < 365)
            return $"{diff.Days / 30} months ago";

        return $"{diff.Days / 365} years ago";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts a percentage value to a brush (green for low, yellow for medium, red for high).
/// </summary>
public class PercentageToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush GreenBrush = new(Color.Parse("#28A745"));
    private static readonly SolidColorBrush YellowBrush = new(Color.Parse("#FFC107"));
    private static readonly SolidColorBrush RedBrush = new(Color.Parse("#DC3545"));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        double percentage = value switch
        {
            double d => d,
            float f => f,
            int i => i,
            _ => 0
        };

        return percentage switch
        {
            < 60 => GreenBrush,
            < 80 => YellowBrush,
            _ => RedBrush
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Static access to byte converters for XAML.
/// </summary>
public static class ByteConverters
{
    /// <summary>
    /// Converts bytes to megabytes.
    /// </summary>
    public static ByteToMegabytesConverter ToMegabytes { get; } = new();

    /// <summary>
    /// Converts bytes to gigabytes.
    /// </summary>
    public static ByteToGigabytesConverter ToGigabytes { get; } = new();
}
