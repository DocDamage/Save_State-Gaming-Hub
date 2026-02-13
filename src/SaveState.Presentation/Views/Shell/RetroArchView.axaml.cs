using Avalonia.Controls;
using SaveState.Presentation.ViewModels.Shell;

namespace SaveState.Presentation.Views.Shell;

public partial class RetroArchView : UserControl
{
    public RetroArchView()
    {
        InitializeComponent();
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Fire and forget with explicit exception handling
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            if (DataContext is RetroArchViewModel viewModel)
            {
                // Load data when the view is loaded asynchronously
                await viewModel.LoadDataCommand.ExecuteAsync(null);
            }
        }
        catch (Exception ex)
        {
            // Log exception - prevents application crash
            System.Diagnostics.Debug.WriteLine($"Error loading RetroArch view: {ex}");
        }
    }
}
