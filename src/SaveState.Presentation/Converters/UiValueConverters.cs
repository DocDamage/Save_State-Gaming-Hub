using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using SaveState.Core.Health.Models;
using SaveState.Presentation.Models.Data;
using SaveState.Presentation.Models.Security;

namespace SaveState.Presentation.Converters;

/// <summary>
/// Converts a nullable object to a boolean indicating whether it exists.
/// </summary>
public class ObjectToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Inverts a boolean value.
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool boolValue && !boolValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts selection state to a subtle background brush.
/// </summary>
public class SelectedBackgroundConverter : IValueConverter
{
    private static readonly SolidColorBrush SelectedBrush = new(Color.Parse("#1F4CAF50"));
    private static readonly SolidColorBrush DefaultBrush = new(Colors.Transparent);

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? SelectedBrush : DefaultBrush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts enabled state to a status brush.
/// </summary>
public class BoolToEnabledBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush EnabledBrush = new(Color.Parse("#4CAF50"));
    private static readonly SolidColorBrush DisabledBrush = new(Color.Parse("#6C757D"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? EnabledBrush : DisabledBrush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts a boolean to one of two strings based on a "true|false" parameter.
/// </summary>
public class BoolToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is string pair)
        {
            var parts = pair.Split('|');
            if (parts.Length == 2)
            {
                return value is true ? parts[0] : parts[1];
            }
        }

        return value is true ? "True" : "False";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts a boolean to one of two integers based on a "true|false" parameter.
/// </summary>
public class BoolToIntConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is string pair)
        {
            var parts = pair.Split('|');
            if (parts.Length == 2
                && int.TryParse(parts[0], out var trueValue)
                && int.TryParse(parts[1], out var falseValue))
            {
                return value is true ? trueValue : falseValue;
            }
        }

        return value is true ? 1 : 0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts enum values to their numeric ordinal.
/// </summary>
public class EnumToIntConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is Enum enumValue ? System.Convert.ToInt32(enumValue) : 0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Compares an enum value to a target enum name.
/// </summary>
public class EnumEqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Enum enumValue && parameter is not null)
        {
            return string.Equals(enumValue.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts health alert severity to a border brush color.
/// </summary>
public class AlertSeverityToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush InfoBrush = new(Color.Parse("#03A9F4"));
    private static readonly SolidColorBrush WarningBrush = new(Color.Parse("#FFC107"));
    private static readonly SolidColorBrush CriticalBrush = new(Color.Parse("#F44336"));
    private static readonly SolidColorBrush DefaultBrush = new(Color.Parse("#9E9E9E"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            AlertSeverity.Info => InfoBrush,
            AlertSeverity.Warning => WarningBrush,
            AlertSeverity.Critical => CriticalBrush,
            _ => DefaultBrush
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts null check to a brush for selection highlighting.
/// </summary>
public class NullToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush SelectedBrush = new(Color.Parse("#1A4CAF50"));
    private static readonly SolidColorBrush TransparentBrush = new(Colors.Transparent);

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Compare the current item (value) with the selected item (parameter)
        if (value is null || parameter is null)
            return TransparentBrush;

        return value.Equals(parameter) ? SelectedBrush : TransparentBrush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts item type to an icon emoji for display.
/// </summary>
public class ItemTypeToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString()?.ToLowerInvariant() switch
        {
            "game" => "🎮",
            "savestate" or "save state" => "💾",
            "achievement" => "🏆",
            "collection" => "📚",
            "setting" or "settings" => "⚙️",
            "mugen" or "mugen data" => "🥊",
            "macro" or "macros" => "⌨️",
            "rom" or "roms" => "🎲",
            "playsession" or "play session" => "⏱️",
            _ => "📄"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts ConflictResolution enum to ComboBox SelectedIndex.
/// </summary>
public class ConflictResolutionToIndexConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            ConflictResolution.KeepCurrent => 0,
            ConflictResolution.UseImported => 1,
            ConflictResolution.KeepBoth => 2,
            ConflictResolution.Skip => 3,
            _ => 0
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            0 => ConflictResolution.KeepCurrent,
            1 => ConflictResolution.UseImported,
            2 => ConflictResolution.KeepBoth,
            3 => ConflictResolution.Skip,
            _ => ConflictResolution.KeepCurrent
        };
    }
}

/// <summary>
/// Converts a string to its first character (for avatar generation).
/// </summary>
public class FirstCharConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str && str.Length > 0)
        {
            return str[..1].ToUpperInvariant();
        }
        return "?";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Checks if a role ID exists in a collection of roles.
/// </summary>
public class RoleInCollectionConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string roleId && parameter is ObservableCollection<Role> roles)
        {
            return roles.Any(r => r.Id == roleId);
        }
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Checks if a scope exists in a collection of scopes.
/// </summary>
public class ScopeInCollectionConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string scope && parameter is ObservableCollection<string> scopes)
        {
            return scopes.Contains(scope);
        }
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts null to button class style (Primary if null, Secondary if set).
/// </summary>
public class NullToButtonClassConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is null ? "Primary" : "Secondary";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Compares two values for equality.
/// </summary>
public class EqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null && parameter is null)
            return true;
        if (value is null || parameter is null)
            return false;
        return value.Equals(parameter);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts an integer to a brush with a threshold comparison.
/// Parameter format: "threshold|aboveBrush|belowBrush"
/// Example: "5|RedBrush|GreenBrush"
/// </summary>
public class IntThresholdToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush DefaultAboveBrush = new(Color.Parse("#ef4444"));
    private static readonly SolidColorBrush DefaultBelowBrush = new(Color.Parse("#10b981"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int intValue)
            return DefaultBelowBrush;

        int threshold = 0;
        if (parameter is string paramStr)
        {
            var parts = paramStr.Split('|');
            if (parts.Length >= 1 && int.TryParse(parts[0], out var parsedThreshold))
            {
                threshold = parsedThreshold;
            }
        }

        return intValue > threshold ? DefaultAboveBrush : DefaultBelowBrush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Checks if a permission ID exists in a collection of permissions.
/// </summary>
public class PermissionInCollectionConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string permissionId && parameter is ObservableCollection<string> permissions)
        {
            return permissions.Contains(permissionId);
        }
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}
