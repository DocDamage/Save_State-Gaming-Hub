using Avalonia.Controls;
using Avalonia.Input;
using SaveState.Core.Automation.Services.DTOs;
using SaveState.Presentation.ViewModels.Shell;

namespace SaveState.Presentation.Views.Shell;

/// <summary>
/// View for macro recorder and playback functionality.
/// </summary>
public partial class MacroRecorderView : UserControl
{
    public MacroRecorderView()
    {
        InitializeComponent();
    }

    private void MacroItem_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is Macro macro)
        {
            if (DataContext is MacroRecorderViewModel viewModel)
            {
                viewModel.SelectedMacro = macro;
            }
        }
    }
}
