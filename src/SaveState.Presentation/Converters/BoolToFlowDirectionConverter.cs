using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace SaveState.Presentation.Converters;

public class BoolToFlowDirectionConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isRtl && isRtl)
        {
            return FlowDirection.RightToLeft;
        }
        return FlowDirection.LeftToRight;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is FlowDirection flowDirection)
        {
            return flowDirection == FlowDirection.RightToLeft;
        }
        return false;
    }
}
