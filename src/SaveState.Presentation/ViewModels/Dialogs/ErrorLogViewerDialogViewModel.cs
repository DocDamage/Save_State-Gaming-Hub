using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Common.Services;
using SaveState.Presentation.Models.Health;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
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
/// Provides filtering, exporting, and detailed viewing of error logs.
/// </summary>
public partial class ErrorLogViewerDialogViewModel : ObservableObject
{
    private readonly ObservableCollection<ErrorLogEntry> _allErrors = new();
    private readonly IErrorLogService? _errorLogService;
    private readonly ITimeProvider _timeProvider;

    /// <summary>Collection of filtered error log entries.</summary>
    [ObservableProperty]
    private ObservableCollection<ErrorLogEntry> _errors = new();

    /// <summary>Currently selected error entry.</summary>
    [ObservableProperty]
    private ErrorLogEntry? _selectedError;

    /// <summary>Severity filter (All, Error, Warning, Info).</summary>
    [ObservableProperty]
    private string _severityFilter = "All";

    /// <summary>Start date filter.</summary>
    [ObservableProperty]
    private DateTime? _startDate;

    /// <summary>End date filter.</summary>
    [ObservableProperty]
    private DateTime? _endDate;

    /// <summary>Component filter.</summary>
    [ObservableProperty]
    private string _componentFilter = string.Empty;

    /// <summary>Available severity filters.</summary>
    public List<string> SeverityFilters { get; } = new() { "All", "Critical", "Error", "Warning", "Info" };

    /// <summary>Available components for filtering.</summary>
    public ObservableCollection<string> AvailableComponents { get; } = new();

    /// <summary>Whether the detail panel is visible.</summary>
    public bool IsDetailVisible => SelectedError is not null;

    /// <summary>
    /// Design-time constructor for XAML preview.
    /// </summary>
    [Obsolete("Design-time constructor only. Use the parameterized constructor in production code.")]
    public ErrorLogViewerDialogViewModel()
    {
        InitializeSampleData();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorLogViewerDialogViewModel"/> class.
    /// </summary>
    public ErrorLogViewerDialogViewModel(IErrorLogService? errorLogService = null, ITimeProvider? timeProvider = null)
    {
        _errorLogService = errorLogService;
        _timeProvider = timeProvider ?? SystemTimeProvider.Instance;
        InitializeSampleData();
        _ = LoadErrorsAsync();
    }

    private void InitializeSampleData()
    {
        var sampleErrors = new List<ErrorLogEntry>
        {
            new()
            {
                Timestamp = _timeProvider.Now.AddHours(-1),
                Component = "Steam API",
                Message = "Connection timeout during sync",
                Severity = ErrorSeverity.Warning,
                StackTrace = "at SteamAPI.Connect()\n   at SyncService.RunSync()"
            },
            new()
            {
                Timestamp = _timeProvider.Now.AddHours(-2),
                Component = "Database",
                Message = "Query took longer than expected (>5s)",
                Severity = ErrorSeverity.Info
            },
            new()
            {
                Timestamp = _timeProvider.Now.AddHours(-3),
                Component = "Cover Downloader",
                Message = "Image decode failed for game ID 12345",
                Severity = ErrorSeverity.Error,
                StackTrace = "System.InvalidOperationException: Invalid image format\n   at ImageDecoder.Decode()"
            },
            new()
            {
                Timestamp = _timeProvider.Now.AddHours(-4),
                Component = "Sync Service",
                Message = "Cloud sync rate limit exceeded",
                Severity = ErrorSeverity.Warning
            },
            new()
            {
                Timestamp = _timeProvider.Now.AddHours(-5),
                Component = "Database",
                Message = "Backup completed successfully",
                Severity = ErrorSeverity.Info
            },
            new()
            {
                Timestamp = _timeProvider.Now.AddDays(-1),
                Component = "Discord RPC",
                Message = "Failed to initialize Discord connection",
                Severity = ErrorSeverity.Error
            }
        };

        foreach (var error in sampleErrors)
        {
            _allErrors.Add(error);
        }

        UpdateAvailableComponents();
        ApplyFilters();
    }

    private void UpdateAvailableComponents()
    {
        AvailableComponents.Clear();
        AvailableComponents.Add("All");
        foreach (var component in _allErrors.Select(e => e.Component).Distinct().OrderBy(c => c))
        {
            AvailableComponents.Add(component);
        }
        if (string.IsNullOrEmpty(ComponentFilter))
        {
            ComponentFilter = "All";
        }
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
            UpdateAvailableComponents();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load error logs: {ex.Message}");
        }
    }

    /// <summary>
    /// Refreshes the error log list.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadErrorsAsync();
    }

    /// <summary>
    /// Applies filters to the error list.
    /// </summary>
    [RelayCommand]
    private void ApplyFilters()
    {
        var filtered = _allErrors.AsEnumerable();

        // Apply severity filter
        if (!string.IsNullOrEmpty(SeverityFilter) && SeverityFilter != "All")
        {
            if (Enum.TryParse<ErrorSeverity>(SeverityFilter, out var severity))
            {
                filtered = filtered.Where(e => e.Severity == severity);
            }
        }

        // Apply component filter
        if (!string.IsNullOrEmpty(ComponentFilter) && ComponentFilter != "All")
        {
            filtered = filtered.Where(e => e.Component == ComponentFilter);
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

        Errors = new ObservableCollection<ErrorLogEntry>(filtered.OrderByDescending(e => e.Timestamp).ToList());
    }

    /// <summary>
    /// Clears all filters.
    /// </summary>
    [RelayCommand]
    private void ClearFilters()
    {
        SeverityFilter = "All";
        ComponentFilter = "All";
        StartDate = null;
        EndDate = null;
        ApplyFilters();
    }

    /// <summary>
    /// Exports the error log to a file.
    /// </summary>
    [RelayCommand]
    private async Task ExportAsync()
    {
        try
        {
            var window = Avalonia.Application.Current?.ApplicationLifetime
                is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (window is null) return;

            var filePickerOptions = new FilePickerSaveOptions
            {
                Title = "Export Error Log",
                DefaultExtension = ".txt",
                SuggestedFileName = $"error_log_{_timeProvider.Now:yyyyMMdd_HHmmss}",
                FileTypeChoices = new List<FilePickerFileType>
                {
                    new FilePickerFileType("Text Files") { Patterns = new[] { "*.txt" } },
                    new FilePickerFileType("CSV Files") { Patterns = new[] { "*.csv" } },
                    new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } }
                }
            };

            var result = await window.StorageProvider.SaveFilePickerAsync(filePickerOptions);

            if (result is not null)
            {
                var path = result.Path.LocalPath;
                var extension = Path.GetExtension(path).ToLowerInvariant();

                string content = extension switch
                {
                    ".csv" => ExportAsCsv(),
                    ".json" => ExportAsJson(),
                    _ => ExportAsText()
                };

                await File.WriteAllTextAsync(path, content);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to export log: {ex.Message}");
        }
    }

    private string ExportAsText()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Error Log Export - {_timeProvider.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine(new string('=', 80));
        sb.AppendLine();

        foreach (var error in _allErrors.OrderByDescending(e => e.Timestamp))
        {
            sb.AppendLine($"[{error.Timestamp:yyyy-MM-dd HH:mm:ss}] [{error.Severity}] [{error.Component}]");
            sb.AppendLine($"  Message: {error.Message}");
            if (!string.IsNullOrEmpty(error.StackTrace))
            {
                sb.AppendLine($"  StackTrace:");
                sb.AppendLine($"    {error.StackTrace.Replace("\n", "\n    ")}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string ExportAsCsv()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Timestamp,Severity,Component,Message,StackTrace");

        foreach (var error in _allErrors.OrderByDescending(e => e.Timestamp))
        {
            var message = EscapeCsvField(error.Message);
            var stackTrace = EscapeCsvField(error.StackTrace ?? "");
            sb.AppendLine($"{error.Timestamp:yyyy-MM-dd HH:mm:ss},{error.Severity},{error.Component},{message},{stackTrace}");
        }

        return sb.ToString();
    }

    private string ExportAsJson()
    {
        var entries = _allErrors.OrderByDescending(e => e.Timestamp).Select(e => new
        {
            e.Timestamp,
            e.Severity,
            e.Component,
            e.Message,
            e.StackTrace
        });

        return System.Text.Json.JsonSerializer.Serialize(entries, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private static string EscapeCsvField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
        {
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        }
        return field;
    }

    /// <summary>
    /// Clears all errors from the log.
    /// </summary>
    [RelayCommand]
    private async Task ClearAsync()
    {
        try
        {
            if (_errorLogService is not null)
            {
                await _errorLogService.ClearAllAsync();
            }

            _allErrors.Clear();
            Errors.Clear();
            SelectedError = null;
            UpdateAvailableComponents();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to clear log: {ex.Message}");
        }
    }

    /// <summary>
    /// Closes the dialog.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        // Dialog close is handled by the view
    }
}
