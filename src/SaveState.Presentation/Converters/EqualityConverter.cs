using Avalonia.Data;
using Avalonia.Data.Converters;
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
