using Avalonia.Controls;
using Avalonia.Input;
using SaveState.Presentation.ViewModels.Dialogs;

namespace SaveState.Presentation.Views.Dialogs;

/// <summary>
/// Dialog for selecting a running process to attach to.
/// </summary>
public partial class ProcessSelectorDialog : Window
{
    /// <summary>
    /// Initializes a new instance of the process selector dialog.
    /// </summary>
    public ProcessSelectorDialog()
    {
        InitializeComponent();
    }

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is ProcessSelectorDialogViewModel vm)
        {
            vm.SetCloseAction((result) => Close(result));
        }
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        // Handle double-click on process list items
        if (e.ClickCount == 2 && DataContext is ProcessSelectorDialogViewModel vm)
        {
            var point = e.GetPosition(this);
            // Check if the click was within the ListBox area
            // The AttachCommand will validate if a process is selected
            if (vm.SelectedProcess != null)
            {
                vm.AttachCommand.Execute(null);
            }
        }
    }
}
