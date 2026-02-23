using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using System.Globalization;

namespace SaveState.Presentation.Converters;

#region Animated Double Converter

/// <summary>
/// Converts a double value and animates the change.
/// 
/// Usage:
/// <code>
/// &lt;ProgressBar Value="{Binding Progress, Converter={StaticResource AnimatedDoubleConverter}}" /&gt;
/// </code>
/// </summary>
public class AnimatedDoubleConverter : IValueConverter
{
    private static readonly Dictionary<WeakReference<Control>, AnimationState> _animationStates = new();
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromMilliseconds(300);
    private static readonly CubicEaseOut DefaultEasing = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Return the value directly; animation happens in ConvertBack or via attached behavior
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }

    /// <summary>
    /// Animates a property change on a control.
    /// </summary>
    public static async Task AnimateValueAsync(
        Control control,
        AvaloniaProperty property,
        double fromValue,
        double toValue,
        TimeSpan? duration = null)
    {
        var animDuration = duration ?? DefaultDuration;
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < animDuration)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            var progress = elapsed / animDuration.TotalMilliseconds;
            var eased = DefaultEasing.Ease(progress);
            var current = fromValue + ((toValue - fromValue) * eased);

            await Dispatcher.UIThread.InvokeAsync(() => control.SetValue(property, current));
            await Task.Delay(16);
        }

        await Dispatcher.UIThread.InvokeAsync(() => control.SetValue(property, toValue));
    }

    private class AnimationState
    {
        public double CurrentValue { get; set; }
        public CancellationTokenSource? CancellationTokenSource { get; set; }
    }
}

#endregion

#region Animated Color Converter

/// <summary>
/// Converts a color value and animates the transition.
/// 
/// Usage:
/// <code>
/// &lt;Border Background="{Binding StatusColor, Converter={StaticResource AnimatedColorConverter}}" /&gt;
/// </code>
/// </summary>
public class AnimatedColorConverter : IValueConverter
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromMilliseconds(300);
    private static readonly CubicEaseOut DefaultEasing = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Color color)
        {
            return new SolidColorBrush(color);
        }
        if (value is SolidColorBrush brush)
        {
            return brush;
        }
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SolidColorBrush brush)
        {
            return brush.Color;
        }
        return value;
    }

    /// <summary>
    /// Animates a color property change on a control.
    /// </summary>
    public static async Task AnimateColorAsync(
        Control control,
        AvaloniaProperty property,
        Color fromColor,
        Color toColor,
        TimeSpan? duration = null)
    {
        var animDuration = duration ?? DefaultDuration;
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < animDuration)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            var progress = elapsed / animDuration.TotalMilliseconds;
            var eased = DefaultEasing.Ease(progress);

            var current = Color.FromArgb(
                (byte)(fromColor.A + ((toColor.A - fromColor.A) * eased)),
                (byte)(fromColor.R + ((toColor.R - fromColor.R) * eased)),
                (byte)(fromColor.G + ((toColor.G - fromColor.G) * eased)),
                (byte)(fromColor.B + ((toColor.B - fromColor.B) * eased))
            );

            await Dispatcher.UIThread.InvokeAsync(() =>
                control.SetValue(property, new SolidColorBrush(current)));
            await Task.Delay(16);
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
            control.SetValue(property, new SolidColorBrush(toColor)));
    }
}

#endregion

#region Count Up Converter

/// <summary>
/// Animates a number counting up or down to the target value.
/// 
/// Usage:
/// <code>
/// &lt;TextBlock Text="{Binding Score, Converter={StaticResource CountUpConverter}, ConverterParameter='N0'}" /&gt;
/// </code>
/// </summary>
public class CountUpConverter : IValueConverter
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromMilliseconds(800);
    private static readonly CubicEaseOut DefaultEasing = new();
    private static readonly Dictionary<WeakReference<TextBlock>, CountUpState> _states = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IConvertible convertible) return value;

        var format = parameter as string ?? "F0";
        var targetValue = convertible.ToDouble(CultureInfo.InvariantCulture);

        // Get or create state
        var state = GetOrCreateState();

        // Cancel previous animation
        state.CancellationTokenSource?.Cancel();
        state.CancellationTokenSource = new CancellationTokenSource();

        // Start new animation
        _ = AnimateCountAsync(state, targetValue, format, culture, state.CancellationTokenSource.Token);

        return state.CurrentValue.ToString(format, culture);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }

    private CountUpState GetOrCreateState()
    {
        // Clean up dead references
        var deadKeys = _states.Keys.Where(k => !k.TryGetTarget(out _)).ToList();
        foreach (var key in deadKeys)
        {
            _states.Remove(key);
        }

        var state = new CountUpState();
        return state;
    }

    private async Task AnimateCountAsync(
        CountUpState state,
        double targetValue,
        string format,
        CultureInfo culture,
        CancellationToken cancellationToken)
    {
        var startValue = state.CurrentValue;
        var startTime = DateTime.UtcNow;
        var duration = DefaultDuration;

        // Adjust duration based on distance
        var distance = Math.Abs(targetValue - startValue);
        if (distance > 1000)
        {
            duration = TimeSpan.FromMilliseconds(1000);
        }
        else if (distance < 10)
        {
            duration = TimeSpan.FromMilliseconds(200);
        }

        try
        {
            while (DateTime.UtcNow - startTime < duration)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                var progress = elapsed / duration.TotalMilliseconds;
                var eased = DefaultEasing.Ease(progress);

                state.CurrentValue = startValue + ((targetValue - startValue) * eased);

                await Task.Delay(16, cancellationToken);
            }

            state.CurrentValue = targetValue;
        }
        catch (OperationCanceledException)
        {
            // Animation was cancelled
        }
    }

    private class CountUpState
    {
        public double CurrentValue { get; set; }
        public CancellationTokenSource? CancellationTokenSource { get; set; }
    }
}

#endregion

#region Animated Visibility Converter

/// <summary>
/// Converts a boolean to visibility with a fade animation.
/// 
/// Usage:
/// <code>
/// &lt;Border IsVisible="{Binding IsExpanded, Converter={StaticResource AnimatedVisibilityConverter}}" /&gt;
/// </code>
/// </summary>
public class AnimatedVisibilityConverter : IValueConverter
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromMilliseconds(250);

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue;
        }
        return value ?? false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue;
        }
        return false;
    }

    /// <summary>
    /// Animates a control's visibility with fade.
    /// </summary>
    public static async Task AnimateVisibilityAsync(
        Control control,
        bool isVisible,
        TimeSpan? duration = null)
    {
        var animDuration = duration ?? DefaultDuration;

        if (isVisible)
        {
            control.Opacity = 0;
            control.IsVisible = true;

            var animation = new Animation
            {
                Duration = animDuration,
                Easing = new CubicEaseOut(),
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0.0),
                        Setters = { new Setter(Visual.OpacityProperty, 0.0) }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1.0),
                        Setters = { new Setter(Visual.OpacityProperty, 1.0) }
                    }
                }
            };

            await animation.RunAsync(control, System.Threading.CancellationToken.None);
        }
        else
        {
            var animation = new Animation
            {
                Duration = animDuration,
                Easing = new CubicEaseIn(),
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0.0),
                        Setters = { new Setter(Visual.OpacityProperty, control.Opacity) }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1.0),
                        Setters = { new Setter(Visual.OpacityProperty, 0.0) }
                    }
                }
            };

            await animation.RunAsync(control, System.Threading.CancellationToken.None);
            control.IsVisible = false;
            control.Opacity = 1; // Reset for next time
        }
    }
}

#endregion

#region Animated Thickness Converter

/// <summary>
/// Animates changes to margin or padding (Thickness values).
/// 
/// Usage:
/// <code>
/// &lt;Border Margin="{Binding ExpandedMargin, Converter={StaticResource AnimatedThicknessConverter}}" /&gt;
/// </code>
/// </summary>
public class AnimatedThicknessConverter : IValueConverter
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromMilliseconds(300);
    private static readonly CubicEaseOut DefaultEasing = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }

    /// <summary>
    /// Animates a thickness property change on a control.
    /// </summary>
    public static async Task AnimateThicknessAsync(
        Control control,
        AvaloniaProperty property,
        Thickness fromThickness,
        Thickness toThickness,
        TimeSpan? duration = null)
    {
        var animDuration = duration ?? DefaultDuration;
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < animDuration)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            var progress = elapsed / animDuration.TotalMilliseconds;
            var eased = DefaultEasing.Ease(progress);

            var current = new Thickness(
                fromThickness.Left + ((toThickness.Left - fromThickness.Left) * eased),
                fromThickness.Top + ((toThickness.Top - fromThickness.Top) * eased),
                fromThickness.Right + ((toThickness.Right - fromThickness.Right) * eased),
                fromThickness.Bottom + ((toThickness.Bottom - fromThickness.Bottom) * eased)
            );

            await Dispatcher.UIThread.InvokeAsync(() => control.SetValue(property, current));
            await Task.Delay(16);
        }

        await Dispatcher.UIThread.InvokeAsync(() => control.SetValue(property, toThickness));
    }
}

#endregion

#region Progress To Color Converter

/// <summary>
/// Converts a progress value (0-100) to a color gradient.
/// 
/// Usage:
/// <code>
/// &lt;Border Background="{Binding Progress, Converter={StaticResource ProgressToColorConverter}}" /&gt;
/// </code>
/// </summary>
public class ProgressToColorConverter : IValueConverter
{
    // Colors for different progress ranges
    private static readonly Color LowColor = Color.Parse("#F44336");    // Red
    private static readonly Color MediumColor = Color.Parse("#FFC107"); // Yellow
    private static readonly Color HighColor = Color.Parse("#4CAF50");   // Green

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IConvertible convertible) return new SolidColorBrush(LowColor);

        var progress = convertible.ToDouble(CultureInfo.InvariantCulture);
        progress = Math.Clamp(progress, 0, 100);

        Color color;
        if (progress < 50)
        {
            // Interpolate between low and medium
            var t = progress / 50;
            color = InterpolateColor(LowColor, MediumColor, t);
        }
        else
        {
            // Interpolate between medium and high
            var t = (progress - 50) / 50;
            color = InterpolateColor(MediumColor, HighColor, t);
        }

        return new SolidColorBrush(color);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return 0;
    }

    private static Color InterpolateColor(Color from, Color to, double t)
    {
        return Color.FromArgb(
            (byte)(from.A + ((to.A - from.A) * t)),
            (byte)(from.R + ((to.R - from.R) * t)),
            (byte)(from.G + ((to.G - from.G) * t)),
            (byte)(from.B + ((to.B - from.B) * t))
        );
    }
}

#endregion

#region Boolean To Scale Converter

/// <summary>
/// Converts a boolean to a scale value with optional animation.
/// 
/// Usage:
/// <code>
/// &lt;Border RenderTransform="{Binding IsExpanded, Converter={StaticResource BoolToScaleConverter}, ConverterParameter='1,1.2'}" /&gt;
/// </code>
/// </summary>
public class BoolToScaleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isTrue = value is true;

        // Parse parameter as "falseValue,trueValue" or use defaults
        var (falseScale, trueScale) = ParseParameter(parameter);

        var scale = isTrue ? trueScale : falseScale;
        return new ScaleTransform(scale, scale);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }

    private (double falseScale, double trueScale) ParseParameter(object? parameter)
    {
        if (parameter is string str)
        {
            var parts = str.Split(',');
            if (parts.Length == 2 &&
                double.TryParse(parts[0], out var falseScale) &&
                double.TryParse(parts[1], out var trueScale))
            {
                return (falseScale, trueScale);
            }
        }
        return (1.0, 1.1); // Default values
    }
}

#endregion

#region Smooth Value Converter

/// <summary>
/// Provides smooth interpolation between values using exponential smoothing.
/// Useful for real-time data like FPS counters or network speed.
/// 
/// Usage:
/// <code>
/// &lt;TextBlock Text="{Binding Fps, Converter={StaticResource SmoothValueConverter}, ConverterParameter='0.3'}" /&gt;
/// </code>
/// </summary>
public class SmoothValueConverter : IValueConverter
{
    private static readonly Dictionary<WeakReference<object>, SmoothState> _states = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IConvertible convertible) return value;

        // Parse smoothing factor (0-1, lower = smoother)
        var smoothing = 0.3;
        if (parameter is string str && double.TryParse(str, out var parsed))
        {
            smoothing = Math.Clamp(parsed, 0.01, 1.0);
        }

        var targetValue = convertible.ToDouble(CultureInfo.InvariantCulture);
        var state = GetOrCreateState();

        // Apply exponential smoothing
        state.CurrentValue = state.CurrentValue + (smoothing * (targetValue - state.CurrentValue));

        // Update state
        state.LastValue = targetValue;

        // Return formatted value
        if (targetValue is >= 100 or < 0.1)
        {
            return state.CurrentValue.ToString("F0", culture);
        }
        else if (targetValue >= 10)
        {
            return state.CurrentValue.ToString("F1", culture);
        }
        else
        {
            return state.CurrentValue.ToString("F2", culture);
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }

    private SmoothState GetOrCreateState()
    {
        // Clean up dead references
        var deadKeys = _states.Keys.Where(k => !k.TryGetTarget(out _)).ToList();
        foreach (var key in deadKeys)
        {
            _states.Remove(key);
        }
        return new SmoothState();
    }

    private class SmoothState
    {
        public double CurrentValue { get; set; }
        public double LastValue { get; set; }
    }
}

#endregion
