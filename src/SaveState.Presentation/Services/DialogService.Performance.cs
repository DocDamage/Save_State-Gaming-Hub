using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Presentation.ViewModels.Dialogs;
using SaveState.Presentation.ViewModels.Settings;
using SaveState.Presentation.Views.Dialogs;

namespace SaveState.Presentation.Services;

/// <summary>
/// Performance-related dialog methods for the DialogService.
/// </summary>
public partial class DialogService
{
    /// <inheritdoc />
    public async Task ShowGamePerformanceDetailAsync(GamePerformanceStats gameStats)
    {
        try
        {
            var vm = new GamePerformanceDetailViewModel(
                _timeProvider,
                gameStats,
                performanceService: null,
                systemResourceManager: null,
                performanceMonitor: null,
                errorTrackingService: null,
                notificationService: null);

            var dialog = new GamePerformanceDetailView
            {
                DataContext = vm
            };

            var mainWindow = GetMainWindow();
            if (mainWindow != null)
            {
                await dialog.ShowDialog(mainWindow);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show game performance detail dialog for {GameName}", gameStats.GameName);
        }
    }

    /// <inheritdoc />
    public async Task ShowMessageAsync(string title, string message)
    {
        await ShowInformationAsync(title, message);
    }
}
