using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Models.Health;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// Service for managing error logs.
/// </summary>
public interface IErrorLogService
{
    /// <summary>
    /// Gets all error log entries.
    /// </summary>
    Task<IReadOnlyList<ErrorLogEntry>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Clears all error logs.
    /// </summary>
    Task ClearAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Adds a new error log entry.
    /// </summary>
    Task AddAsync(ErrorLogEntry entry, CancellationToken ct = default);
}

/// <summary>
/// ViewModel for the Error Log Viewer dialog.
/// </summary>
public partial class ErrorLogViewerDialogViewModel : ObservableObject
{
    private readonly ObservableCollection<ErrorLogEntry> _allErrors = new();
    private readonly IErrorLogService? _errorLogService;

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
    /// Design-time constructor for XAML preview.
    /// </summary>
    [Obsolete("Design-time constructor only. Use the parameterized constructor in production code.")]
    public ErrorLogViewerDialogViewModel()
    {
        // Sample data
        _allErrors = new ObservableCollection<ErrorLogEntry>
        {
            new()
            {
                Timestamp = DateTimeOffset.UtcNow.AddHours(-1).DateTime,
                Component = "Steam API",
                Message = "Connection timeout",
                Severity = ErrorSeverity.Warning
            },
            new()
            {
                Timestamp = DateTimeOffset.UtcNow.AddHours(-2).DateTime,
                Component = "Database",
                Message = "Query took longer than expected",
                Severity = ErrorSeverity.Info
            },
            new()
            {
                Timestamp = DateTimeOffset.UtcNow.AddHours(-3).DateTime,
                Component = "Cover Downloader",
                Message = "Image decode failed",
                Severity = ErrorSeverity.Error
            },
            new()
            {
                Timestamp = DateTimeOffset.UtcNow.AddHours(-4).DateTime,
                Component = "Sync Service",
                Message = "Cloud sync rate limit exceeded",
                Severity = ErrorSeverity.Warning
            },
            new()
            {
                Timestamp = DateTimeOffset.UtcNow.AddHours(-5).DateTime,
                Component = "Database",
                Message = "Backup completed successfully",
                Severity = ErrorSeverity.Info
            }
        };
        Errors = new ObservableCollection<ErrorLogEntry>(_allErrors);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorLogViewerDialogViewModel"/> class.
    /// </summary>
    public ErrorLogViewerDialogViewModel(IErrorLogService errorLogService)
    {
        _errorLogService = errorLogService;
        _ = LoadErrorsAsync();
    }

    /// <summary>
    /// Loads error logs from the service.
    /// </summary>
    private async Task LoadErrorsAsync()
    {
        if (_errorLogService is null) return;

        try
        {
            var entries = await _errorLogService.GetAllAsync();
            _allErrors.Clear();
            foreach (var entry in entries)
            {
                _allErrors.Add(entry);
            }
            Filter();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load error logs: {ex.Message}");
        }
    }

    /// <summary>
    /// Applies filters to the error list.
    /// </summary>
    [RelayCommand]
    private void Filter()
    {
        var filtered = _allErrors.AsEnumerable();

        // Apply search query filter
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var query = SearchQuery.ToLowerInvariant();
            filtered = filtered.Where(e =>
                e.Message.ToLowerInvariant().Contains(query) ||
                e.Component.ToLowerInvariant().Contains(query));
        }

        // Apply severity filter
        if (MinSeverity.HasValue)
        {
            filtered = filtered.Where(e => e.Severity >= MinSeverity.Value);
        }

        // Apply component filter
        if (!string.IsNullOrEmpty(SelectedComponent) && SelectedComponent != "All")
        {
            filtered = filtered.Where(e => e.Component == SelectedComponent);
        }

        // Apply date range filters
        if (StartDate.HasValue)
        {
            filtered = filtered.Where(e => e.Timestamp >= StartDate.Value);
        }

        if (EndDate.HasValue)
        {
            filtered = filtered.Where(e => e.Timestamp <= EndDate.Value.Date.AddDays(1).AddTicks(-1));
        }

        Errors = new ObservableCollection<ErrorLogEntry>(filtered.ToList());
    }

    /// <summary>
    /// Exports the error log to a file.
    /// </summary>
    [RelayCommand]
    private async Task ExportLogAsync()
    {
        try
        {
            // Get the top-level window for the file picker
            var window = Avalonia.Application.Current?.ApplicationLifetime
                is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (window is null) return;

            // Create file picker options
            var filePickerOptions = new FilePickerSaveOptions
            {
                Title = "Export Error Log",
                DefaultExtension = ".txt",
                SuggestedFileName = $"error_log_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}",
                FileTypeChoices = new List<FilePickerFileType>
                {
                    new FilePickerFileType("Text Files") { Patterns = new[] { "*.txt" } },
                    new FilePickerFileType("CSV Files") { Patterns = new[] { "*.csv" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                }
            };

            var result = await window.StorageProvider.SaveFilePickerAsync(filePickerOptions);

            if (result is not null)
            {
                var path = result.Path.LocalPath;
                var sb = new StringBuilder();
                sb.AppendLine($"Error Log Export - {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine(new string('=', 80));
                sb.AppendLine();

                foreach (var error in _allErrors.OrderByDescending(e => e.Timestamp))
                {
                    sb.AppendLine($"[{error.Timestamp:yyyy-MM-dd HH:mm:ss}] [{error.Severity}] [{error.Component}]");
                    sb.AppendLine($"  {error.Message}");
                    if (!string.IsNullOrEmpty(error.StackTrace))
                    {
                        sb.AppendLine($"  StackTrace: {error.StackTrace}");
                    }
                    sb.AppendLine();
                }

                await File.WriteAllTextAsync(path, sb.ToString());
            }
        }
        catch (Exception ex)
        {
            // Log the error or show to user
            System.Diagnostics.Debug.WriteLine($"Failed to export log: {ex.Message}");
        }
    }

    /// <summary>
    /// Clears all errors from the log.
    /// </summary>
    [RelayCommand]
    private async Task ClearLogAsync()
    {
        try
        {
            // Clear through service if available
            if (_errorLogService is not null)
            {
                await _errorLogService.ClearAllAsync();
            }

            // Clear local collections
            _allErrors.Clear();
            Errors.Clear();
            SelectedError = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to clear log: {ex.Message}");
        }
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
