using Avalonia.Controls;
using Avalonia.Input;

namespace SaveState.Presentation.Views.Shell;

/// <summary>
/// The status bar view showing application status information.
/// </summary>
public partial class StatusBarView : UserControl
{
    public StatusBarView()
    {
        InitializeComponent();
    }

    private void OnConnectionStatusPressed(object? sender, PointerPressedEventArgs e)
    {
        var viewModel = DataContext as ViewModels.Shell.StatusBarViewModel;
        viewModel?.ShowNetworkDetails();
    }

    private void OnGameCountPressed(object? sender, PointerPressedEventArgs e)
    {
        // Navigate to Library tab
        var viewModel = DataContext as ViewModels.Shell.StatusBarViewModel;
        viewModel?.NavigateToLibraryAsync();
    }

    private void OnPlaytimePressed(object? sender, PointerPressedEventArgs e)
    {
        // Navigate to Analytics tab
        var viewModel = DataContext as ViewModels.Shell.StatusBarViewModel;
        viewModel?.NavigateToAnalyticsAsync();
    }

    private void OnSyncStatusPressed(object? sender, PointerPressedEventArgs e)
    {
        // Show sync details
        var viewModel = DataContext as ViewModels.Shell.StatusBarViewModel;
        viewModel?.ShowSyncDetails();
    }
}
