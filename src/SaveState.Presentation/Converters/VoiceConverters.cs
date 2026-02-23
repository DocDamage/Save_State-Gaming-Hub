using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using SaveState.Presentation.Models.Voice;

namespace SaveState.Presentation.Converters;

/// <summary>
/// Converts VoiceVisualizerState to a color brush.
/// </summary>
public class VoiceStateToColorConverter : IValueConverter
{
    private static readonly SolidColorBrush IdleBrush = new(Color.Parse("#9E9E9E"));
    private static readonly SolidColorBrush ListeningBrush = new(Color.Parse("#2196F3"));
    private static readonly SolidColorBrush ProcessingBrush = new(Color.Parse("#FFC107"));
    private static readonly SolidColorBrush ExecutingBrush = new(Color.Parse("#FF9800"));
    private static readonly SolidColorBrush SuccessBrush = new(Color.Parse("#4CAF50"));
    private static readonly SolidColorBrush ErrorBrush = new(Color.Parse("#F44336"));
    private static readonly SolidColorBrush MutedBrush = new(Color.Parse("#616161"));
    private static readonly SolidColorBrush DefaultBrush = new(Color.Parse("#9E9E9E"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is VoiceVisualizerState state)
        {
            return state switch
            {
                VoiceVisualizerState.Idle => IdleBrush,
                VoiceVisualizerState.Listening => ListeningBrush,
                VoiceVisualizerState.Processing => ProcessingBrush,
                VoiceVisualizerState.Executing => ExecutingBrush,
                VoiceVisualizerState.Success => SuccessBrush,
                VoiceVisualizerState.Error => ErrorBrush,
                VoiceVisualizerState.Muted => MutedBrush,
                _ => DefaultBrush
            };
        }

        return DefaultBrush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts VoiceVisualizerState to an icon character.
/// </summary>
public class VoiceStateToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is VoiceVisualizerState state)
        {
            return state switch
            {
                VoiceVisualizerState.Idle => "🎤",
                VoiceVisualizerState.Listening => "🎙️",
                VoiceVisualizerState.Processing => "⚙️",
                VoiceVisualizerState.Executing => "▶️",
                VoiceVisualizerState.Success => "✅",
                VoiceVisualizerState.Error => "❌",
                VoiceVisualizerState.Muted => "🔇",
                _ => "🎤"
            };
        }

        return "🎤";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts VoiceVisualizerState to a display text.
/// </summary>
public class VoiceStateToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is VoiceVisualizerState state)
        {
            return state switch
            {
                VoiceVisualizerState.Idle => "Say 'Hey SaveState' to start",
                VoiceVisualizerState.Listening => "Listening...",
                VoiceVisualizerState.Processing => "Processing...",
                VoiceVisualizerState.Executing => "Executing command...",
                VoiceVisualizerState.Success => "Command executed",
                VoiceVisualizerState.Error => "Command failed",
                VoiceVisualizerState.Muted => "Microphone muted",
                _ => "Voice Command"
            };
        }

        return "Voice Command";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts VoiceVisualizerState to an animation intensity (0.0 to 1.0).
/// </summary>
public class VoiceStateToAnimationIntensityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is VoiceVisualizerState state)
        {
            return state switch
            {
                VoiceVisualizerState.Idle => 0.1,
                VoiceVisualizerState.Listening => 1.0,
                VoiceVisualizerState.Processing => 0.7,
                VoiceVisualizerState.Executing => 0.5,
                VoiceVisualizerState.Success => 0.3,
                VoiceVisualizerState.Error => 0.3,
                VoiceVisualizerState.Muted => 0.0,
                _ => 0.1
            };
        }

        return 0.1;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts VoiceVisualizerState to a pulse animation speed.
/// </summary>
public class VoiceStateToPulseSpeedConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is VoiceVisualizerState state)
        {
            return state switch
            {
                VoiceVisualizerState.Idle => 2.0,
                VoiceVisualizerState.Listening => 0.5,
                VoiceVisualizerState.Processing => 0.8,
                VoiceVisualizerState.Executing => 1.0,
                VoiceVisualizerState.Success => 1.5,
                VoiceVisualizerState.Error => 1.5,
                VoiceVisualizerState.Muted => 0.0,
                _ => 2.0
            };
        }

        return 2.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts VoiceVisualizerState to visibility (true for visible states).
/// </summary>
public class VoiceStateToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is VoiceVisualizerState state)
        {
            return state switch
            {
                VoiceVisualizerState.Idle => true,
                VoiceVisualizerState.Listening => true,
                VoiceVisualizerState.Processing => true,
                VoiceVisualizerState.Executing => true,
                VoiceVisualizerState.Success => true,
                VoiceVisualizerState.Error => true,
                _ => false
            };
        }

        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts confidence level (0.0-1.0) to a color brush.
/// </summary>
public class ConfidenceToColorConverter : IValueConverter
{
    private static readonly SolidColorBrush LowBrush = new(Color.Parse("#F44336"));
    private static readonly SolidColorBrush MediumBrush = new(Color.Parse("#FFC107"));
    private static readonly SolidColorBrush HighBrush = new(Color.Parse("#4CAF50"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is float confidence || value is double doubleConfidence)
        {
            var conf = value is float f ? f : (float)doubleConfidence;
            return conf switch
            {
                >= 0.8f => HighBrush,
                >= 0.5f => MediumBrush,
                _ => LowBrush
            };
        }

        return LowBrush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts confidence level (0.0-1.0) to a percentage string.
/// </summary>
public class ConfidenceToPercentageConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is float confidence || value is double doubleConfidence)
        {
            var conf = value is float f ? f : (float)doubleConfidence;
            return $"{conf * 100:F0}%";
        }

        return "0%";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts audio level (0.0-1.0) to a bar height.
/// </summary>
public class AudioLevelToBarHeightConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is float level || value is double doubleLevel)
        {
            var lvl = value is float f ? f : (float)doubleLevel;
            var maxHeight = parameter is string param && double.TryParse(param, out var max) ? max : 100.0;
            return Math.Clamp(lvl * maxHeight, 2, maxHeight);
        }

        return 2.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts a boolean indicating if state is active to opacity.
/// </summary>
public class VoiceActiveToOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive)
        {
            return isActive ? 1.0 : 0.5;
        }

        return 0.5;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts listening duration to a formatted string.
/// </summary>
public class ListeningDurationToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TimeSpan duration)
        {
            if (duration.TotalSeconds < 60)
            {
                return $"{duration.TotalSeconds:F0}s";
            }
            return $"{duration.TotalMinutes:F0}m {duration.Seconds}s";
        }

        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}
