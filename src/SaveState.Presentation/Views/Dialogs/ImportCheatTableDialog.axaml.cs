using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using SaveState.Presentation.ViewModels.Dialogs;

namespace SaveState.Presentation.Views.Dialogs;

/// <summary>
/// Dialog for importing Cheat Engine table files (.CT).
/// </summary>
public partial class ImportCheatTableDialog : Window
{
    private ImportCheatTableViewModel? _viewModel;

    public ImportCheatTableDialog()
    {
        InitializeComponent();

        // Add drag-and-drop handlers
        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        _viewModel = DataContext as ImportCheatTableViewModel;
        if (_viewModel != null)
        {
            _viewModel.SetCloseAction(result =>
            {
                Close(result);
            });
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        // Only allow if the data contains files
        if (e.Data.Contains(DataFormats.Files))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        if (_viewModel == null) return;

        if (e.Data.Contains(DataFormats.Files))
        {
            var files = e.Data.GetFiles();
            if (files != null)
            {
                var filePaths = files.Select(f => f.Path.LocalPath).ToArray();
                await _viewModel.HandleFileDropAsync(filePaths);
            }
        }

        e.Handled = true;
    }
}
