using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SaveState.Presentation.ViewModels.Overlays;

namespace SaveState.Presentation.Views.Overlays;

/// <summary>
/// The voice visualizer overlay view.
/// </summary>
public partial class VoiceVisualizerView : UserControl
{
    /// <summary>
    /// Creates a new voice visualizer view.
    /// </summary>
    public VoiceVisualizerView()
    {
        InitializeComponent();

        // Register keyboard shortcut (Ctrl+Shift+V) to toggle visibility
        this.KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // Toggle visibility with Ctrl+Shift+V
        if (e.Key == Key.V &&
            e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift))
        {
            if (DataContext is VoiceVisualizerViewModel viewModel)
            {
                if (viewModel.IsVisible)
                {
                    viewModel.CancelCommand.Execute(null);
                }
                else
                {
                    viewModel.ShowAsync();
                }
            }
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // Initialize view model if needed
        if (DataContext is VoiceVisualizerViewModel viewModel)
        {
            viewModel.ShowAsync();
        }
    }

    /// <inheritdoc />
    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        this.KeyDown -= OnKeyDown;

        // Clean up view model
        if (DataContext is VoiceVisualizerViewModel viewModel)
        {
            viewModel.HideAsync();
        }
    }
}
