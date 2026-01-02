using Avalonia.Controls;
using Avalonia.Input;

namespace SaveState.Presentation.Views.Shell;

/// <summary>
/// The header bar view containing tab navigation and global controls.
/// </summary>
public partial class HeaderBarView : UserControl
{
    public HeaderBarView()
    {
        InitializeComponent();
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        var viewModel = DataContext as ViewModels.Shell.HeaderBarViewModel;

        if (e.Key == Key.Enter && sender is TextBox)
        {
            // Execute search
            viewModel?.ExecuteSearch();
        }
        else if (e.Key == Key.Escape && sender is TextBox textBox)
        {
            // Clear search
            textBox.Text = string.Empty;
            viewModel?.ClearSearch();
        }
    }
}