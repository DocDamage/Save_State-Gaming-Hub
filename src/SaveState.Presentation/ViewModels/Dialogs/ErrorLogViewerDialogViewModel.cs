using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Models.Health;
using System.Collections.ObjectModel;
using System.Linq;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the Error Log Viewer dialog.
/// </summary>
public partial class ErrorLogViewerDialogViewModel : ObservableObject
{
    /// <summary>Collection of error log entries.</summary>
    [ObservableProperty]
    private ObservableCollection<ErrorLogEntry> _errors = new();

    /// <summary>Currently selected error entry.</summary>
    [ObservableProperty]
    private ErrorLogEntry? _selectedError;

    /// <summary>Search query for filtering errors.</summary>
    [ObservableProperty]
    private string _searchQuery = string.Empty;

    /// <summary>Minimum severity filter.</summary>
    [ObservableProperty]
    private ErrorSeverity? _minSeverity;

    /// <summary>Start date filter.</summary>
    [ObservableProperty]
    private DateTime? _startDate;

    /// <summary>End date filter.</summary>
    [ObservableProperty]
    private DateTime? _endDate;

    /// <summary>Selected component filter.</summary>
    [ObservableProperty]
    private string _selectedComponent = "All";

    /// <summary>Available component filters.</summary>
    public List<string> Components { get; } = new()
    {
        "All",
        "Database",
        "Steam API",
        "Cover Downloader",
        "Sync Service",
        "Cloud Sync",
        "RetroAchievements",
        "Discord RPC"
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorLogViewerDialogViewModel"/> class.
    /// </summary>
    public ErrorLogViewerDialogViewModel()
    {
        // Sample data
        Errors = new ObservableCollection<ErrorLogEntry>
        {
            new()
            {
                Timestamp = DateTime.Now.AddHours(-1),
                Component = "Steam API",
                Message = "Connection timeout",
                Severity = ErrorSeverity.Warning
            },
            new()
            {
                Timestamp = DateTime.Now.AddHours(-2),
                Component = "Database",
                Message = "Query took longer than expected",
                Severity = ErrorSeverity.Info
            },
            new()
            {
                Timestamp = DateTime.Now.AddHours(-3),
                Component = "Cover Downloader",
                Message = "Image decode failed",
                Severity = ErrorSeverity.Error
            },
            new()
            {
                Timestamp = DateTime.Now.AddHours(-4),
                Component = "Sync Service",
                Message = "Cloud sync rate limit exceeded",
                Severity = ErrorSeverity.Warning
            },
            new()
            {
                Timestamp = DateTime.Now.AddHours(-5),
                Component = "Database",
                Message = "Backup completed successfully",
                Severity = ErrorSeverity.Info
            }
        };
    }

    /// <summary>
    /// Applies filters to the error list.
    /// </summary>
    [RelayCommand]
    private void Filter()
    {
        // TODO: Apply filters and refresh error list from service
    }

    /// <summary>
    /// Exports the error log to a file.
    /// </summary>
    [RelayCommand]
    private async Task ExportLogAsync()
    {
        // TODO: Export to file
        await Task.CompletedTask;
    }

    /// <summary>
    /// Clears all errors from the log.
    /// </summary>
    [RelayCommand]
    private async Task ClearLogAsync()
    {
        // TODO: Clear all errors through service
        Errors.Clear();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Views details of the selected error.
    /// </summary>
    /// <param name="entry">The error entry to view.</param>
    [RelayCommand]
    private void ViewDetails(ErrorLogEntry? entry)
    {
        if (entry is null) return;
        SelectedError = entry;
    }

    /// <summary>
    /// Closes the dialog.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        // Close dialog
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close();
        }
    }
}
