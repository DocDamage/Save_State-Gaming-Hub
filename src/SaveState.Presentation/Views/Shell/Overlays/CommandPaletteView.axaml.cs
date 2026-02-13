using Avalonia.Controls;
using Avalonia.Input;

namespace SaveState.Presentation.Views.Shell.Overlays;

/// <summary>
/// The command palette overlay view.
/// </summary>
public partial class CommandPaletteView : UserControl
{
    public CommandPaletteView()
    {
        InitializeComponent();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is ViewModels.Shell.CommandPaletteViewModel viewModel)
        {
            e.Handled = viewModel.HandleKey(e.Key);
        }
    }
}
