using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using SaveState.Presentation.ViewModels;

namespace SaveState.Presentation.Converters;

/// <summary>
/// Converts ViewMode values to CSS class names for styling.
/// Used in game library view to apply different styles based on view mode.
/// </summary>
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
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean values to CSS class names.
/// Useful for conditional styling based on boolean states.
/// </summary>
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
        throw new NotImplementedException();
    }
}

public class BoolToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (value as bool?) == true
            ? new SolidColorBrush(Color.Parse("#0078D4")) // Accent color
            : Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
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
                6 => viewModel.ScreenshotsTab,
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }
}
