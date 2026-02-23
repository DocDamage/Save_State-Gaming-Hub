using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace SaveState.Presentation.Converters;

/// <summary>
/// Converts a documentation section to a background brush based on selection state.
/// </summary>
public class SectionToBackgroundConverter : IValueConverter
{
    private static readonly SolidColorBrush SelectedBrush = new(Color.Parse("#2D2D2D"));
    private static readonly SolidColorBrush DefaultBrush = new(Colors.Transparent);

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // If value is not null, the section is considered selected/active
        return value is not null ? SelectedBrush : DefaultBrush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts a boolean favorite state to a color (gold for favorite, gray for not).
/// </summary>
public class BoolToFavoriteColorConverter : IValueConverter
{
    private static readonly SolidColorBrush FavoriteBrush = new(Color.Parse("#FFD700")); // Gold
    private static readonly SolidColorBrush NotFavoriteBrush = new(Color.Parse("#808080")); // Gray

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? FavoriteBrush : NotFavoriteBrush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts equality comparison to a CSS class name when values are equal.
/// Parameter format: "ClassName" or just the class name.
/// </summary>
public class EqualityToClassConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // value = SelectedProfile.Id
        // parameter = "Active" or "Active|{Id}"
        if (parameter is not string paramStr)
            return string.Empty;

        // Parse parameter - format could be "Active" or "Active|{ExpectedId}"
        var parts = paramStr.Split('|');
        var className = parts[0];
        
        if (parts.Length > 1)
        {
            // Compare value with expected ID from parameter
            var expectedId = parts[1];
            return value?.ToString() == expectedId ? className : string.Empty;
        }
        
        // Fallback - just return class name if value is not null
        return value is not null ? className : string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}
