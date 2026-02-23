using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using SaveState.Presentation.ViewModels.Overlays;

namespace SaveState.Presentation.Views.Overlays;

/// <summary>
/// Code-behind for the AchievementOverlayView.
/// </summary>
public partial class AchievementOverlayView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the AchievementOverlayView class.
    /// </summary>
    public AchievementOverlayView()
    {
        InitializeComponent();

        // Handle key events for Escape key dismissal
        KeyDown += OnKeyDown;
    }

    /// <summary>
    /// Initializes a new instance with a view model.
    /// </summary>
    public AchievementOverlayView(AchievementOverlayViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Handles the background click to dismiss.
    /// </summary>
    private void OnBackgroundClick(object? sender, PointerPressedEventArgs e)
    {
        // Only dismiss if clicking directly on the background (not on content)
        if (e.Source is Border border && border.Name == null)
        {
            if (DataContext is AchievementOverlayViewModel vm)
            {
                vm.HandleEscapeKeyCommand.Execute(null);
            }
        }
    }

    /// <summary>
    /// Handles key down events.
    /// </summary>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (DataContext is AchievementOverlayViewModel vm)
            {
                vm.HandleEscapeKeyCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
