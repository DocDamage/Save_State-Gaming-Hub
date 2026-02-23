using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using SaveState.Presentation.ViewModels.Overlays;

namespace SaveState.Presentation.Views.Overlays;

/// <summary>
/// Cinematic launch experience overlay with AI briefings, animations, and immersive visuals.
/// </summary>
public partial class LaunchExperienceOverlay : UserControl
{
    public LaunchExperienceOverlay()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles keyboard input for skipping or cancelling the launch experience.
    /// </summary>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not LaunchExperienceViewModel viewModel)
            return;

        switch (e.Key)
        {
            case Key.Space:
            case Key.Enter:
                // Skip the launch experience if allowed
                if (viewModel.CanSkip && viewModel.SkipCommand.CanExecute(null))
                {
                    viewModel.SkipCommand.Execute(null);
                    e.Handled = true;
                }
                break;

            case Key.Escape:
                // Cancel the launch
                if (viewModel.CancelLaunchCommand.CanExecute(null))
                {
                    viewModel.CancelLaunchCommand.Execute(null);
                    e.Handled = true;
                }
                break;
        }
    }
}
