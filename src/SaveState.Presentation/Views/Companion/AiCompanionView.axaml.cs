using Avalonia.Controls;
using Avalonia.Input;
using SaveState.Presentation.ViewModels.Companion;

namespace SaveState.Presentation.Views.Companion;

/// <summary>
/// Code-behind for the AI Companion View.
/// </summary>
public partial class AiCompanionView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AiCompanionView"/> class.
    /// </summary>
    public AiCompanionView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles the KeyDown event for the message input box.
    /// Sends the message when Enter is pressed (without Shift).
    /// </summary>
    private void OnMessageKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            e.Handled = true;
            if (DataContext is AiCompanionViewModel viewModel && 
                viewModel.SendMessageCommand.CanExecute(null))
            {
                viewModel.SendMessageCommand.Execute(null);
            }
        }
    }
}
