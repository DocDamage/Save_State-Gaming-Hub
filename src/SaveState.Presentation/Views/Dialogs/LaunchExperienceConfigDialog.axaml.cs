using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SaveState.Core.GameLibrary.Services.DTOs;
using SaveState.Presentation.ViewModels.Dialogs;

namespace SaveState.Presentation.Views.Dialogs;

/// <summary>
/// Configuration dialog for launch experience settings with cinematic options.
/// </summary>
public partial class LaunchExperienceConfigDialog : Window
{
    private LaunchExperienceSettings? _result;

    public LaunchExperienceConfigDialog()
    {
        InitializeComponent();
        SubscribeToViewModelEvents();
    }

    /// <summary>
    /// Gets the configured settings after the dialog is closed.
    /// </summary>
    public LaunchExperienceSettings? Result => _result;

    private void SubscribeToViewModelEvents()
    {
        if (DataContext is LaunchExperienceConfigDialogViewModel viewModel)
        {
            viewModel.SaveRequested += OnSaveRequested;
            viewModel.CancelRequested += OnCancelRequested;
        }
    }

    private void OnSaveRequested(object? sender, LaunchExperienceSettings settings)
    {
        _result = settings;
        Close(true);
    }

    private void OnCancelRequested(object? sender, EventArgs e)
    {
        _result = null;
        Close(false);
    }

    /// <summary>
    /// Shows the dialog and returns the configured settings.
    /// </summary>
    public async Task<LaunchExperienceSettings?> ShowDialogAsync(Window owner)
    {
        var result = await ShowDialog<bool>(owner);
        return result ? _result : null;
    }
}

/// <summary>
/// Converter to find AnimationDurationOption matching the enum value.
/// </summary>
public class DurationOptionConverter : Avalonia.Data.Converters.IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is AnimationDuration duration && parameter is IEnumerable<AnimationDurationOption> options)
        {
            return options.FirstOrDefault(o => o.Value == duration);
        }
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is AnimationDurationOption option)
        {
            return option.Value;
        }
        return value;
    }
}

/// <summary>
/// Converter to find BackgroundStyleOption matching the enum value.
/// </summary>
public class BackgroundStyleOptionConverter : Avalonia.Data.Converters.IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is BackgroundStyle style && parameter is IEnumerable<BackgroundStyleOption> options)
        {
            return options.FirstOrDefault(o => o.Value == style);
        }
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is BackgroundStyleOption option)
        {
            return option.Value;
        }
        return value;
    }
}
