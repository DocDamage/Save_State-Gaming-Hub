using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using SaveState.Presentation.Models.Accounts;
using System;
using System.Globalization;

namespace SaveState.Presentation.Converters;

/// <summary>
/// Converts a value to true if it equals the parameter, otherwise false.
/// </summary>
public class EqualityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null && parameter == null)
            return true;

        if (value == null || parameter == null)
            return false;

        return value.ToString() == parameter.ToString();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts a value to true if it does NOT equal the parameter, otherwise false.
/// </summary>
public class InequalityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null && parameter == null)
            return false;

        if (value == null || parameter == null)
            return true;

        return value.ToString() != parameter.ToString();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts a value to Bold FontWeight if it equals the parameter, otherwise Normal.
/// </summary>
public class EqualityToFontWeightConverter : IValueConverter
{
    public static readonly EqualityToFontWeightConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null && parameter == null)
            return Avalonia.Media.FontWeight.Bold;

        if (value == null || parameter == null)
            return Avalonia.Media.FontWeight.Normal;

        bool isEqual = value.ToString() == parameter.ToString();
        return isEqual ? Avalonia.Media.FontWeight.Bold : Avalonia.Media.FontWeight.Normal;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts a value to PrimaryBrush if it equals the parameter, otherwise Transparent.
/// </summary>
public class EqualityToBrushConverter : IValueConverter
{
    public static readonly EqualityToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null && parameter == null)
            return new SolidColorBrush(Color.Parse("#007ACC")); // Primary brush color

        if (value == null || parameter == null)
            return new SolidColorBrush(Color.Parse("Transparent"));

        bool isEqual = value.ToString() == parameter.ToString();
        return isEqual
            ? new SolidColorBrush(Color.Parse("#007ACC"))  // Primary brush color
            : new SolidColorBrush(Color.Parse("Transparent"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts ConnectionStatus to an appropriate brush color.
/// </summary>
public class ConnectionStatusToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ConnectionStatus status)
        {
            return status switch
            {
                ConnectionStatus.Connected => new SolidColorBrush(Color.Parse("#28A745")),      // Green
                ConnectionStatus.Disconnected => new SolidColorBrush(Color.Parse("#6C757D")),   // Gray
                ConnectionStatus.Connecting => new SolidColorBrush(Color.Parse("#FFC107")),     // Yellow/Amber
                ConnectionStatus.Error => new SolidColorBrush(Color.Parse("#DC3545")),          // Red
                ConnectionStatus.NotAvailable => new SolidColorBrush(Color.Parse("#6C757D")),   // Gray
                _ => new SolidColorBrush(Color.Parse("#6C757D"))
            };
        }
        return new SolidColorBrush(Color.Parse("#6C757D"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts ConnectionStatus to true if status is Connected.
/// </summary>
public class IsConnectedConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is ConnectionStatus status && status == ConnectionStatus.Connected;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts ConnectionStatus to true if status is Disconnected.
/// </summary>
public class IsDisconnectedConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is ConnectionStatus status && status == ConnectionStatus.Disconnected;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts an enum to a collection of items for ComboBox binding.
/// </summary>
public class EnumToItemsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return Array.Empty<string>();

        var enumType = value.GetType();
        if (!enumType.IsEnum)
            return Array.Empty<string>();

        return Enum.GetNames(enumType);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts an integer to a status brush (red for > 0, green for 0).
/// </summary>
public class IntToStatusBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intValue)
        {
            return intValue > 0
                ? new SolidColorBrush(Color.Parse("#FFC107"))  // Yellow/Warning for conflicts
                : new SolidColorBrush(Color.Parse("#28A745")); // Green for no conflicts
        }
        return new SolidColorBrush(Color.Parse("#28A745"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts two boolean values using OR logic.
/// </summary>
public class OrConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        foreach (var value in values)
        {
            if (value is true)
                return true;
        }
        return false;
    }
}

/// <summary>
/// Converts a provider ID to an emoji icon.
/// </summary>
public class ProviderToIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var provider = value?.ToString()?.ToLowerInvariant();
        return provider switch
        {
            "steam" => "🎮",
            "gog" => "🎲",
            "epic" or "epic games" => "🎯",
            "retroachievements" => "🏆",
            "discord" => "💬",
            "xbox" => "🎮",
            _ => "🔗"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts a provider ID to a display name.
/// </summary>
public class ProviderToDisplayNameConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var provider = value?.ToString()?.ToLowerInvariant();
        return provider switch
        {
            "steam" => "Steam",
            "gog" => "GOG",
            "epic" => "Epic Games",
            "retroachievements" => "RetroAchievements",
            "discord" => "Discord",
            "xbox" => "Xbox",
            _ => provider ?? "Unknown"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts a platform name to an emoji icon.
/// </summary>
public class PlatformToIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var platform = value?.ToString()?.ToLowerInvariant();
        return platform switch
        {
            "steam" => "🎮",
            "gog" => "🎲",
            "epic games" or "epic" => "🎯",
            "retroachievements" => "🏆",
            "discord" => "💬",
            "xbox" => "🎮",
            "origin" => "📦",
            "ea app" => "📦",
            "playstation" => "🎮",
            "nintendo" => "🎮",
            "battle.net" => "⚔️",
            "ubisoft" => "🛡️",
            _ => "🎮"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts a boolean value to "Yes" or "No" string.
/// </summary>
public class BoolToYesNoConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (value as bool?) == true ? "Yes" : "No";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}
