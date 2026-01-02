using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace SaveState.Presentation.Converters;

/// <summary>
/// Converts boolean values to Avalonia FlowDirection enumeration.
/// Used for UI layout direction binding (Left-to-Right or Right-to-Left).
/// </summary>
public class BoolToFlowDirectionConverter : IValueConverter
{
    /// <summary>
    /// Converts a boolean value to FlowDirection.
    /// True returns RightToLeft, false returns LeftToRight.
    /// </summary>
    /// <param name="value">The boolean value to convert.</param>
    /// <param name="targetType">The target type (FlowDirection).</param>
    /// <param name="parameter">Converter parameter (not used).</param>
    /// <param name="culture">Culture information (not used).</param>
    /// <returns>FlowDirection.RightToLeft for true, FlowDirection.LeftToRight for false.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isRtl && isRtl)
        {
            return FlowDirection.RightToLeft;
        }
        return FlowDirection.LeftToRight;
    }

    /// <summary>
    /// Converts FlowDirection back to boolean.
    /// RightToLeft returns true, LeftToRight returns false.
    /// </summary>
    /// <param name="value">The FlowDirection value to convert.</param>
    /// <param name="targetType">The target type (bool).</param>
    /// <param name="parameter">Converter parameter (not used).</param>
    /// <param name="culture">Culture information (not used).</param>
    /// <returns>True for RightToLeft, false for LeftToRight.</returns>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is FlowDirection flowDirection)
        {
            return flowDirection == FlowDirection.RightToLeft;
        }
        return false;
    }
}
