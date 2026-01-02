using Avalonia.Controls;
using SaveState.Presentation.ViewModels.Shell;

namespace SaveState.Presentation.Views.Shell;

/// <summary>
/// The main application shell window.
/// </summary>
public partial class MainShell : Window
{
    public MainShell()
    {
        InitializeComponent();
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