using Avalonia.Controls;
using Avalonia.Input;
using SaveState.Presentation.Services;
using SaveState.Presentation.ViewModels.Shell;
using Splat;

namespace SaveState.Presentation.Views.Shell;

/// <summary>
/// The main application shell window.
/// </summary>
public partial class MainShell : Window
{
    private IOverlayService? _overlayService;

    public MainShell()
    {
        InitializeComponent();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Handled)
        {
            return;
        }

        // Universal Search: Ctrl+Shift+P
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) &&
            e.KeyModifiers.HasFlag(KeyModifiers.Shift) &&
            e.Key == Key.P)
        {
            _overlayService ??= Locator.Current.GetService<IOverlayService>();
            _overlayService?.ShowUniversalSearchOverlay();
            e.Handled = true;
        }

        // Command Palette: Ctrl+Shift+K (alternative shortcut)
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) &&
            e.KeyModifiers.HasFlag(KeyModifiers.Shift) &&
            e.Key == Key.K)
        {
            _overlayService ??= Locator.Current.GetService<IOverlayService>();
            _overlayService?.ToggleCommandPaletteOverlay();
            e.Handled = true;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        // Dispose view models
        if (DataContext is MainShellViewModel viewModel)
        {
            viewModel.Dispose();
        }
    }
}
