using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using SaveState.Core.Mugen.ValueObjects;

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

public class NullOrEmptyToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isEmpty = value == null || string.IsNullOrWhiteSpace(value.ToString());
        var invert = parameter?.ToString()?.Equals("Invert", StringComparison.OrdinalIgnoreCase) == true;
        return invert ? !isEmpty : isEmpty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

public class FilePathToBitmapConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path))
            return null;

        if (!File.Exists(path))
            return null;

        try
        {
            return new Bitmap(path);
        }
        catch
        {
            return null;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

public class IntToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var targetValue = 0;
        if (parameter != null && int.TryParse(parameter.ToString(), out var parsed))
        {
            targetValue = parsed;
        }

        if (value is int intValue)
        {
            return intValue == targetValue;
        }

        if (value is System.Collections.ICollection collection)
        {
            return collection.Count == targetValue;
        }

        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

public class StringContainsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;

        return value.ToString()?.Contains(parameter.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase) == true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

public class BoolToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var boolValue = value is bool b && b;
        var options = parameter?.ToString()?.Split('|');

        if (options == null || options.Length < 2) return boolValue.ToString();

        // Format: TrueText|FalseText
        return boolValue ? options[0] : options[1];
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts enum values to a collection for use in ComboBoxes.
/// </summary>
public class EnumValuesConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return Array.Empty<object>();

        var enumType = value.GetType();
        if (!enumType.IsEnum) return Array.Empty<object>();

        return Enum.GetValues(enumType).Cast<object>().ToArray();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts difficulty level to appropriate brush color.
/// </summary>
public class DifficultyToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DifficultyLevel difficulty)
        {
            return difficulty switch
            {
                DifficultyLevel.Beginner => Brushes.Green,
                DifficultyLevel.Intermediate => Brushes.Yellow,
                DifficultyLevel.Advanced => Brushes.Orange,
                DifficultyLevel.Expert => Brushes.Red,
                _ => Brushes.Gray
            };
        }

        return Brushes.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts string values to tab indices for TabControl binding.
/// </summary>
public class StringToIndexConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string currentView && parameter is string options)
        {
            var optionArray = options.Split('|');
            for (int i = 0; i < optionArray.Length; i++)
            {
                if (optionArray[i].Equals(currentView, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
        }

        return 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int index && parameter is string options)
        {
            var optionArray = options.Split('|');
            if (index >= 0 && index < optionArray.Length)
            {
                return optionArray[index];
            }
        }

        return "Templates";
    }
}
