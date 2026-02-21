using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using SaveState.Presentation.Models.Health;

namespace SaveState.Presentation.Converters;

/// <summary>
/// Converts HealthStatus enum values to appropriate brushes.
/// </summary>
public class HealthStatusToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is HealthStatus status)
        {
            return status switch
            {
                HealthStatus.Healthy => new SolidColorBrush(Color.Parse("#28A745")),    // Green
                HealthStatus.Degraded => new SolidColorBrush(Color.Parse("#FFC107")),   // Yellow/Orange
                HealthStatus.Unhealthy => new SolidColorBrush(Color.Parse("#DC3545")),  // Red
                HealthStatus.Unknown => new SolidColorBrush(Color.Parse("#6C757D")),    // Gray
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
        return new SolidColorBrush(Color.Parse("#6C757D"));
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
