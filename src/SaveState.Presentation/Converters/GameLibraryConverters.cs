using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using SaveState.Presentation.ViewModels;

namespace SaveState.Presentation.Converters;

public class ViewModeToClassConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ViewMode viewMode && parameter is string mode)
        {
            return viewMode.ToString() == mode ? "Primary" : "Secondary";
        }
        return "Secondary";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToClassConverter : IValueConverter
{
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
