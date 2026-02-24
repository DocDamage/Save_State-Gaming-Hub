using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using SaveState.Presentation;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.Views.Overlays;

/// <summary>
/// Mod details overlay showing mod info, changelog, and conflicts.
/// </summary>
public partial class ModDetailsOverlay : UserControl
{
    public ModDetailsOverlay()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        RequestClose();
        e.Handled = true;
    }

    private void OnBackdropPressed(object? sender, PointerPressedEventArgs e)
    {
        RequestClose();
        e.Handled = true;
    }

    private void OnContentPressed(object? sender, PointerPressedEventArgs e)
    {
        // Prevent clicks inside the content panel from bubbling to the backdrop close handler.
        e.Handled = true;
    }

    private void OnOverlayKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            RequestClose();
            e.Handled = true;
        }
    }

    private void RequestClose()
    {
        if (DataContext is ViewModels.Overlays.ModDetailsOverlayViewModel viewModel)
        {
            if (viewModel.CloseCommand.CanExecute(null))
            {
                viewModel.CloseCommand.Execute(null);
                return;
            }
        }

        // Fallback path in case DataContext wiring is stale.
        var overlayService = Locator.Current.GetService<IOverlayService>();
        overlayService.HideModDetailsOverlay();
    }
}
