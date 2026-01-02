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