using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Controls.ApplicationLifetimes;
using SaveState.Presentation.Services;
using SaveState.Presentation.ViewModels.Shell;
using Splat;
using System.Linq;

namespace SaveState.Presentation.Views.Shell;

/// <summary>
/// The main application shell window.
/// </summary>
public partial class MainShell : Window
{
    private IOverlayService? _overlayService;
    private bool _startupInitialized;

    public MainShell()
    {
        InitializeComponent();
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (_startupInitialized)
        {
            return;
        }

        _startupInitialized = true;

        // Close secondary windows opened during bootstrap and shortly after startup.
        CloseSecondaryWindows();
        _ = Task.Delay(TimeSpan.FromMilliseconds(500))
            .ContinueWith(_ => Avalonia.Threading.Dispatcher.UIThread.Post(CloseSecondaryWindows));

        if (DataContext is MainShellViewModel viewModel)
        {
            await viewModel.EnsureStartupStateAsync();
        }
    }

    private void CloseSecondaryWindows()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        foreach (var window in desktop.Windows.ToArray())
        {
            if (window != this && window.IsVisible)
            {
                window.Close();
            }
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Handled)
        {
            return;
        }

        // Escape closes modal overlays first.
        if (e.Key == Key.Escape)
        {
            _overlayService ??= Locator.Current.GetService<IOverlayService>();
            if (_overlayService != null)
            {
                var handled = false;

                if (_overlayService.ShowModDetails)
                {
                    _overlayService.HideModDetailsOverlay();
                    handled = true;
                }

                if (_overlayService.ShowAchievementDetails)
                {
                    _overlayService.HideAchievementDetailsOverlay();
                    handled = true;
                }

                if (_overlayService.ShowSessionDetails)
                {
                    _overlayService.HideSessionDetailsOverlay();
                    handled = true;
                }

                if (_overlayService.ShowCommandPalette)
                {
                    _overlayService.HideCommandPaletteOverlay();
                    handled = true;
                }

                if (_overlayService.ShowQuickSearch)
                {
                    _overlayService.HideQuickSearchOverlay();
                    handled = true;
                }

                if (handled)
                {
                    e.Handled = true;
                    return;
                }
            }
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
