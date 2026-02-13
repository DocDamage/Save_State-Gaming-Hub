using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Application.RomManagement.DTOs;
using SaveState.Application.RomManagement.Services;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// View model for the ROM scan progress dialog.
/// </summary>
public partial class RomScanProgressDialogViewModel : ObservableObject
{
    private readonly ILogger<RomScanProgressDialogViewModel> _logger;
    private readonly Stopwatch _stopwatch;
    private CancellationTokenSource? _cancellationTokenSource;

    public RomScanProgressDialogViewModel(ILogger<RomScanProgressDialogViewModel> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _stopwatch = new Stopwatch();

        ErrorMessages = new ObservableCollection<string>();
        Title = "Scanning ROMs";
        StatusMessage = "Initializing scan...";
        FooterMessage = "Scanning in progress...";
        ActionButtonText = "Close";
    }

    /// <summary>
    /// Initializes the scan progress tracking.
    /// </summary>
    /// <param name="directories">The directories being scanned.</param>
    /// <param name="cancellationTokenSource">The cancellation token source for the scan.</param>
    public void InitializeScan(string[] directories, CancellationTokenSource cancellationTokenSource)
    {
        _cancellationTokenSource = cancellationTokenSource;
        TotalDirectories = directories.Length;
        _stopwatch.Start();

        StatusMessage = $"Scanning {TotalDirectories} director{(TotalDirectories == 1 ? "y" : "ies")}...";
        _logger.LogInformation("Initialized ROM scan progress tracking for {Count} directories", TotalDirectories);
    }

    /// <summary>
    /// Updates the scan progress.
    /// </summary>
    /// <param name="progress">The current scan progress.</param>
    public void UpdateProgress(ScanProgress progress)
    {
        CurrentDirectoryProgress = progress.FilesScanned / (double)progress.FilesTotal;
        CurrentFileName = progress.CurrentFile;
        FilesScanned = progress.FilesScanned;
        TotalFiles = progress.FilesTotal;
        RomsFound = progress.RomsFound;

        if (progress.FilesTotal > 0)
        {
            var overallProgress = (double)progress.FilesScanned / progress.FilesTotal;
            OverallProgress = Math.Min(overallProgress, 1.0);
            OverallProgressPercent = (int)(OverallProgress * 100);
        }

        OverallStatus = $"{progress.FilesScanned}/{progress.FilesTotal} files processed, {progress.RomsFound} ROMs found";
    }

    /// <summary>
    /// Marks the scan as completed.
    /// </summary>
    /// <param name="success">Whether the scan completed successfully.</param>
    /// <param name="message">The completion message.</param>
    public void CompleteScan(bool success, string message)
    {
        _stopwatch.Stop();
        IsCompleted = true;
        CanCancel = false;

        OverallProgress = 1.0;
        OverallProgressPercent = 100;

        if (success)
        {
            StatusMessage = "Scan completed successfully";
            FooterMessage = message;
        }
        else
        {
            StatusMessage = "Scan completed with errors";
            FooterMessage = message;
            Title = "Scan Completed (With Errors)";
        }

        _logger.LogInformation("ROM scan completed: {Success}, {Message}", success, message);
    }

    /// <summary>
    /// Reports an error during scanning.
    /// </summary>
    /// <param name="error">The error message.</param>
    public void ReportError(string error)
    {
        ErrorMessages.Add(error);
        HasErrors = true;
        _logger.LogWarning("ROM scan error: {Error}", error);
    }

    /// <summary>
    /// Updates the current directory being processed.
    /// </summary>
    /// <param name="directoryName">The directory name.</param>
    public void UpdateCurrentDirectory(string directoryName)
    {
        CurrentDirectoryStatus = $"Processing: {directoryName}";
        DirectoriesProcessed++;
    }

    /// <summary>
    /// Gets or sets the dialog title.
    /// </summary>
    [ObservableProperty]
    private string title = "Scanning ROMs";

    /// <summary>
    /// Gets or sets the status message.
    /// </summary>
    [ObservableProperty]
    private string statusMessage = "Initializing...";

    /// <summary>
    /// Gets or sets the footer message.
    /// </summary>
    [ObservableProperty]
    private string footerMessage = "Please wait...";

    /// <summary>
    /// Gets or sets the action button text.
    /// </summary>
    [ObservableProperty]
    private string actionButtonText = "Close";

    /// <summary>
    /// Gets or sets whether the scan is completed.
    /// </summary>
    [ObservableProperty]
    private bool isCompleted;

    /// <summary>
    /// Gets or sets whether the scan can be cancelled.
    /// </summary>
    [ObservableProperty]
    private bool canCancel = true;

    /// <summary>
    /// Gets or sets the overall progress (0-1).
    /// </summary>
    [ObservableProperty]
    private double overallProgress;

    /// <summary>
    /// Gets or sets the overall progress percentage.
    /// </summary>
    [ObservableProperty]
    private int overallProgressPercent;

    /// <summary>
    /// Gets or sets the overall status text.
    /// </summary>
    [ObservableProperty]
    private string overallStatus = "Initializing...";

    /// <summary>
    /// Gets or sets the current directory progress (0-1).
    /// </summary>
    [ObservableProperty]
    private double currentDirectoryProgress;

    /// <summary>
    /// Gets or sets the current directory status.
    /// </summary>
    [ObservableProperty]
    private string currentDirectoryStatus = "Preparing...";

    /// <summary>
    /// Gets or sets the current file being processed.
    /// </summary>
    [ObservableProperty]
    private string currentFileName = string.Empty;

    /// <summary>
    /// Gets or sets the total number of directories.
    /// </summary>
    [ObservableProperty]
    private int totalDirectories;

    /// <summary>
    /// Gets or sets the number of directories processed.
    /// </summary>
    [ObservableProperty]
    private int directoriesProcessed;

    /// <summary>
    /// Gets or sets the number of files scanned.
    /// </summary>
    [ObservableProperty]
    private int filesScanned;

    /// <summary>
    /// Gets or sets the total number of files.
    /// </summary>
    [ObservableProperty]
    private int totalFiles;

    /// <summary>
    /// Gets or sets the number of ROMs found.
    /// </summary>
    [ObservableProperty]
    private int romsFound;

    /// <summary>
    /// Gets or sets whether there are errors.
    /// </summary>
    [ObservableProperty]
    private bool hasErrors;

    /// <summary>
    /// Gets the collection of error messages.
    /// </summary>
    public ObservableCollection<string> ErrorMessages { get; }

    /// <summary>
    /// Gets the time elapsed since scan started.
    /// </summary>
    [ObservableProperty]
    private TimeSpan timeElapsed;

    /// <summary>
    /// Gets the request close action.
    /// </summary>
    public Action? RequestClose { get; set; }

    /// <summary>
    /// Updates the elapsed time.
    /// </summary>
    public void UpdateElapsedTime()
    {
        TimeElapsed = _stopwatch.Elapsed;
    }

    [RelayCommand]
    private void Cancel()
    {
        if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
        {
            _cancellationTokenSource.Cancel();
            StatusMessage = "Cancelling scan...";
            CanCancel = false;
            _logger.LogInformation("ROM scan cancelled by user");
        }
    }

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke();
    }
}