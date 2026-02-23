using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using SaveState.Core.Analytics.Models.GamerProfile;
using SaveState.Presentation.ViewModels;
using SaveState.Presentation.ViewModels.Library;
using SaveState.Presentation.ViewModels.Library.GameDetail;

namespace SaveState.Presentation.Converters;

// =============================================================================
// Game Library Converters
// =============================================================================
// These converters are used specifically for the game library and related views.
// They handle view mode styling, boolean-to-brush conversions, tab content
// selection, and various game-related value transformations.
// =============================================================================

/// <summary>
/// Converts ViewMode values to CSS class names for styling.
/// Used in game library view to apply different styles based on view mode.
/// </summary>
/// <remarks>
/// Example usage:
///   Button Class="{Binding CurrentViewMode, Converter={StaticResource ViewModeToClassConverter}, ConverterParameter=Grid}"
/// Returns "Primary" if the view mode matches the parameter, otherwise "Secondary".
/// </remarks>
public class ViewModeToClassConverter : IValueConverter
{
    /// <summary>
    /// Converts a ViewMode value to a CSS class name.
    /// Returns "Primary" if the view mode matches the parameter, otherwise "Secondary".
    /// </summary>
    /// <param name="value">The ViewMode value to convert.</param>
    /// <param name="targetType">The target type (string).</param>
    /// <param name="parameter">The mode parameter to compare against.</param>
    /// <param name="culture">Culture information (not used).</param>
    /// <returns>"Primary" if matched, "Secondary" otherwise.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ViewMode viewMode && parameter is string mode)
        {
            return viewMode.ToString() == mode ? "Primary" : "Secondary";
        }
        return "Secondary";
    }

    /// <summary>
    /// ConvertBack is not implemented for this converter.
    /// </summary>
    /// <param name="value">The value to convert back.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">Converter parameter.</param>
    /// <param name="culture">Culture information.</param>
    /// <returns>Not implemented.</returns>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts boolean values to CSS class names.
/// Useful for conditional styling based on boolean states.
/// </summary>
/// <remarks>
/// Example: Returns "{Parameter} Selected" if true, just "{Parameter}" if false.
/// Used for button states and selection indicators.
/// </remarks>
public class BoolToClassConverter : IValueConverter
{
    /// <summary>
    /// Converts a boolean value to a CSS class name.
    /// Returns the parameter value if true, otherwise returns "Default".
    /// </summary>
    /// <param name="value">The boolean value to convert.</param>
    /// <param name="targetType">The target type (string).</param>
    /// <param name="parameter">The class name to return when true.</param>
    /// <param name="culture">Culture information (not used).</param>
    /// <returns>The parameter value if true, "Default" if false.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string className)
        {
            return boolValue ? $"{className} Selected" : className;
        }
        return parameter;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

public class BoolToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isTrue = value as bool? == true;

        // If parameter is provided, parse "trueBrush|falseBrush" format
        if (parameter is string paramStr)
        {
            var parts = paramStr.Split('|');
            if (parts.Length == 2)
            {
                try
                {
                    var trueBrush = new SolidColorBrush(Color.Parse(parts[0].Trim()));
                    var falseBrush = new SolidColorBrush(Color.Parse(parts[1].Trim()));
                    return isTrue ? trueBrush : falseBrush;
                }
                catch
                {
                    // Fall through to defaults if parsing fails
                }
            }
        }

        // Default behavior
        return isTrue
            ? new SolidColorBrush(Color.Parse("#0078D4")) // Accent color
            : Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

public class TabContentConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 2 && values[0] is int tabIndex && values[1] is GameDetailViewModel viewModel)
        {
            return tabIndex switch
            {
                0 => viewModel.OverviewTab,
                1 => viewModel.SaveStatesTab,
                2 => viewModel.AchievementsTab,
                3 => viewModel.SessionsTab,
                4 => viewModel.NotesTab,
                5 => viewModel.ModsTab,
                6 => viewModel.MediaTab,
                7 => viewModel.PerformanceTab,
                _ => viewModel.OverviewTab
            };
        }
        return null;
    }
}

public class GreaterThanConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intValue && parameter is string paramString && int.TryParse(paramString, out var threshold))
        {
            return intValue > threshold;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts a boolean value to a color based on parameter format "trueColor|falseColor"
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string colorParam)
        {
            var colors = colorParam.Split('|');
            if (colors.Length == 2)
            {
                var colorString = boolValue ? colors[0] : colors[1];
                return Color.Parse(colorString);
            }
        }
        return Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts a boolean to a status-appropriate brush (green for success, red for failure).
/// </summary>
public class BoolToStatusBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (value as bool?) == true
            ? new SolidColorBrush(Color.Parse("#28A745")) // Green for success
            : new SolidColorBrush(Color.Parse("#DC3545")); // Red for failure
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Checks if a value equals the parameter
/// </summary>
public class EqualToConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null && parameter == null) return true;
        if (value == null || parameter == null) return false;

        var valueStr = value.ToString();
        var paramStr = parameter.ToString();

        return valueStr == paramStr;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts a numeric value to a percentage width (0-100)
/// </summary>
public class PercentageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double doubleValue)
        {
            // Normalize to 0-100 range (assuming max is around 10 hours)
            var percentage = Math.Min(100, (doubleValue / 10.0) * 100);
            return percentage;
        }
        return 0.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts a string to a boolean indicating if it's not empty
/// </summary>
public class StringNotEmptyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return !string.IsNullOrEmpty(value as string);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts a hex color string to a SolidColorBrush.
/// </summary>
public class HexToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hexColor)
        {
            try
            {
                var color = Color.Parse(hexColor);
                return new SolidColorBrush(color);
            }
            catch
            {
                return new SolidColorBrush(Colors.Gray);
            }
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts an integer to a boolean indicating if it's greater than zero.
/// </summary>
public class GreaterThanZeroConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is int intValue && intValue > 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts a ProfileCardTheme to a background brush.
/// </summary>
public class ThemeToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ProfileCardTheme theme)
        {
            var color = theme switch
            {
                ProfileCardTheme.Cyberpunk => Color.Parse("#1a1a2e"),
                ProfileCardTheme.Minimal => Color.Parse("#f8f9fa"),
                ProfileCardTheme.Retro => Color.Parse("#2d3436"),
                ProfileCardTheme.Arcade => Color.Parse("#6c5ce7"),
                ProfileCardTheme.Esports => Color.Parse("#0a0a0a"),
                _ => Color.Parse("#1a1a2e")
            };
            return new SolidColorBrush(color);
        }
        return new SolidColorBrush(Color.Parse("#1a1a2e"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

// =============================================================================
// AI Companion Converters
// =============================================================================

/// <summary>
/// Converts player health (0.0-1.0) to an appropriate color brush.
/// </summary>
public class HealthToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double health || value is float healthFloat)
        {
            var h = value is double ? (double)value : (double)(float)value;
            var color = h switch
            {
                > 0.7 => Color.Parse("#10B981"), // Green
                > 0.3 => Color.Parse("#F59E0B"), // Orange
                _ => Color.Parse("#EF4444")      // Red
            };
            return new SolidColorBrush(color);
        }
        return new SolidColorBrush(Color.Parse("#10B981"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts sender type to a background brush color.
/// </summary>
public class SenderToBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var sender = value?.ToString();
        var color = sender?.ToLowerInvariant() switch
        {
            "user" => Color.Parse("#10B981"),   // User messages - green
            "companion" => Color.Parse("#3B82F6"), // Companion messages - blue
            "system" => Color.Parse("#6B7280"), // System messages - gray
            _ => Color.Parse("#3B82F6")
        };
        return new SolidColorBrush(color);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts sender type to an avatar background brush.
/// </summary>
public class SenderToAvatarBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var sender = value?.ToString();
        var color = sender?.ToLowerInvariant() switch
        {
            "user" => Color.Parse("#059669"),   // Darker green for user
            "companion" => Color.Parse("#2563EB"), // Darker blue for companion
            "system" => Color.Parse("#4B5563"), // Darker gray for system
            _ => Color.Parse("#2563EB")
        };
        return new SolidColorBrush(color);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts sender type to an icon string.
/// </summary>
public class SenderToIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var sender = value?.ToString();
        return sender?.ToLowerInvariant() switch
        {
            "user" => "👤",
            "companion" => "🤖",
            "system" => "ℹ️",
            _ => "💬"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts personality enum to its description string.
/// </summary>
public class PersonalityToDescriptionConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var personality = value?.ToString();
        return personality switch
        {
            "Friendly" => "A warm and supportive companion that encourages you during gameplay.",
            "Competitive" => "A fierce companion that pushes you to be your best and celebrates victories.",
            "Analytical" => "A strategic companion that focuses on data and optimization.",
            "Humorous" => "A fun-loving companion that keeps things light with jokes and banter.",
            "Silent" => "A quiet companion that only speaks when absolutely necessary.",
            _ => "Choose how your AI companion will interact with you."
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts skill level enum to its description string.
/// </summary>
public class SkillLevelToDescriptionConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var level = value?.ToString();
        return level switch
        {
            "Beginner" => "Basic tips and guidance suitable for new players.",
            "Intermediate" => "Moderate assistance with some advanced strategies.",
            "Advanced" => "Deep insights and complex tactics for experienced players.",
            "Expert" => "Professional-level analysis and optimization suggestions.",
            _ => "Set the expertise level of your AI companion."
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts boolean to a status color brush (green for active, red for inactive).
/// </summary>
public class BoolToStatusColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive)
        {
            return isActive
                ? new SolidColorBrush(Color.Parse("#10B981")) // Green for active
                : new SolidColorBrush(Color.Parse("#EF4444")); // Red for inactive
        }
        return new SolidColorBrush(Color.Parse("#EF4444"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}
