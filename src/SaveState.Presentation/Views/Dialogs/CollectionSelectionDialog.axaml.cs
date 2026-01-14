using Avalonia.Controls;
using SaveState.Presentation.ViewModels.Dialogs;
using System;

namespace SaveState.Presentation.Views.Dialogs;

public partial class CollectionSelectionDialog : Window
{
    public CollectionSelectionDialog()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is CollectionSelectionDialogViewModel vm)
        {
            vm.SetCloseAction(result => Close(result));
        }
    }
}
