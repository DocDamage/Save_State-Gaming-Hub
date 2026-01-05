using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace SaveState.Presentation.Converters;

/// <summary>
/// Converts values by comparing string equality.
/// Used for conditional UI binding in MUGEN-related views.
/// </summary>
public class StringEqualsConverter : IValueConverter
{
    /// <summary>
    /// Compares two values as strings for equality.
    /// Returns true if both values convert to the same string.
    /// </summary>
    /// <param name="value">The first value to compare.</param>
    /// <param name="targetType">The target type (bool).</param>
    /// <param name="parameter">The second value to compare.</param>
    /// <param name="culture">Culture information (not used).</param>
    /// <returns>True if the string representations are equal, false otherwise.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return false;
        return value.ToString() == parameter.ToString();
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

public class ReferenceEqualsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return false;
        return ReferenceEquals(value, parameter);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

public class NetworkStatusConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (bool)(value ?? false) ? "DISCONNECT" : "CONNECT";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}
