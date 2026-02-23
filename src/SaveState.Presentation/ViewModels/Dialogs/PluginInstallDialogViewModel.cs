using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Models.PluginStore;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the Plugin Install dialog.
/// </summary>
public partial class PluginInstallDialogViewModel : ObservableObject
{
    private IProgress<double>? _progressReporter;
    private CancellationTokenSource? _cancellationTokenSource;

    [ObservableProperty]
    private PluginListing? _plugin;

    [ObservableProperty]
    private double _overallProgress;

    [ObservableProperty]
    private string _currentAction = "Starting installation...";

    [ObservableProperty]
    private bool _isDownloading;

    [ObservableProperty]
    private bool _isDownloadComplete;

    [ObservableProperty]
    private bool _isVerifying;

    [ObservableProperty]
    private bool _isVerifyComplete;

    [ObservableProperty]
    private bool _isInstalling;

    [ObservableProperty]
    private bool _isInstallComplete;

    [ObservableProperty]
    private bool _isActivating;

    [ObservableProperty]
    private bool _isActivateComplete;

    [ObservableProperty]
    private bool _isComplete;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private double _bytesDownloaded;

    [ObservableProperty]
    private double _totalBytes;

    /// <summary>
    /// Gets the title for the dialog.
    /// </summary>
    public string Title => Plugin != null ? $"Installing: {Plugin.Name}" : "Installing Plugin";

    /// <summary>
    /// Gets the plugin name.
    /// </summary>
    public string PluginName => Plugin?.Name ?? "Unknown Plugin";

    /// <summary>
    /// Gets the plugin version.
    /// </summary>
    public string PluginVersion => Plugin?.Version ?? "1.0.0";

    /// <summary>
    /// Gets whether currently downloading and not complete.
    /// </summary>
    public bool IsDownloadingAndNotComplete => IsDownloading && !IsDownloadComplete;

    /// <summary>
    /// Gets whether currently verifying and not complete.
    /// </summary>
    public bool IsVerifyingAndNotComplete => IsVerifying && !IsVerifyComplete;

    /// <summary>
    /// Gets whether currently installing and not complete.
    /// </summary>
    public bool IsInstallingAndNotComplete => IsInstalling && !IsInstallComplete;

    /// <summary>
    /// Gets whether currently activating and not complete.
    /// </summary>
    public bool IsActivatingAndNotComplete => IsActivating && !IsActivateComplete;

    /// <summary>
    /// Gets the icon for verify step.
    /// </summary>
    public string VerifyIcon => IsVerifyComplete ? "✓" : (IsVerifying ? "→" : "○");

    /// <summary>
    /// Gets the icon for install step.
    /// </summary>
    public string InstallIcon => IsInstallComplete ? "✓" : (IsInstalling ? "→" : "○");

    /// <summary>
    /// Gets the icon for activate step.
    /// </summary>
    public string ActivateIcon => IsActivateComplete ? "✓" : (IsActivating ? "→" : "○");

    /// <summary>
    /// Gets the download progress text.
    /// </summary>
    public string DownloadProgress => $"{BytesDownloaded / 1048576:F1} MB / {TotalBytes / 1048576:F1} MB";

    /// <summary>
    /// Sets up the installation parameters.
    /// </summary>
    public void Setup(PluginListing plugin, IProgress<double>? progress)
    {
        Plugin = plugin;
        _progressReporter = progress;
        _cancellationTokenSource = new CancellationTokenSource();

        _ = StartInstallationAsync();
    }

    /// <summary>
    /// Starts the installation process.
    /// </summary>
    private async Task StartInstallationAsync()
    {
        if (Plugin == null) return;

        try
        {
            // Step 1: Downloading
            await ExecuteStepAsync(
                () => IsDownloading = true,
                () => IsDownloadComplete = true,
                "Downloading plugin files...",
                25);

            // Step 2: Verifying
            await ExecuteStepAsync(
                () => IsVerifying = true,
                () => IsVerifyComplete = true,
                "Verifying package integrity...",
                50);

            // Step 3: Installing
            await ExecuteStepAsync(
                () => IsInstalling = true,
                () => IsInstallComplete = true,
                "Installing plugin files...",
                75);

            // Step 4: Activating
            await ExecuteStepAsync(
                () => IsActivating = true,
                () => IsActivateComplete = true,
                "Activating plugin...",
                100);

            // Complete
            IsComplete = true;
            CurrentAction = "Installation complete!";
            _progressReporter?.Report(1.0);
        }
        catch (OperationCanceledException)
        {
            CurrentAction = "Installation cancelled";
            HasError = true;
            ErrorMessage = "Installation was cancelled by user";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Installation failed: {ex.Message}";
            CurrentAction = "Installation failed";
        }
        finally
        {
            IsDownloading = false;
            IsVerifying = false;
            IsInstalling = false;
            IsActivating = false;
        }
    }

    private async Task ExecuteStepAsync(
        Action startAction,
        Action completeAction,
        string actionText,
        double progressValue)
    {
        CurrentAction = actionText;
        startAction();

        // Simulate step duration (in real implementation, this would be actual work)
        await Task.Delay(800, _cancellationTokenSource?.Token ?? CancellationToken.None);

        completeAction();
        OverallProgress = progressValue;
        _progressReporter?.Report(progressValue / 100.0);
    }

    /// <summary>
    /// Cancels the installation.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        _cancellationTokenSource?.Cancel();
        CloseDialog(false);
    }

    /// <summary>
    /// Closes the dialog.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        CloseDialog(IsComplete);
    }

    private void CloseDialog(bool success)
    {
        // In a real implementation, this would close the dialog window
        // and return the result to the caller
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        // Update dependent properties
        if (e.PropertyName == nameof(IsDownloading) || e.PropertyName == nameof(IsDownloadComplete))
        {
            OnPropertyChanged(nameof(IsDownloadingAndNotComplete));
        }
        else if (e.PropertyName == nameof(IsVerifying) || e.PropertyName == nameof(IsVerifyComplete))
        {
            OnPropertyChanged(nameof(IsVerifyingAndNotComplete));
            OnPropertyChanged(nameof(VerifyIcon));
        }
        else if (e.PropertyName == nameof(IsInstalling) || e.PropertyName == nameof(IsInstallComplete))
        {
            OnPropertyChanged(nameof(IsInstallingAndNotComplete));
            OnPropertyChanged(nameof(InstallIcon));
        }
        else if (e.PropertyName == nameof(IsActivating) || e.PropertyName == nameof(IsActivateComplete))
        {
            OnPropertyChanged(nameof(IsActivatingAndNotComplete));
            OnPropertyChanged(nameof(ActivateIcon));
        }
    }
}

/// <summary>
/// Result of the plugin installation dialog.
/// </summary>
public class PluginInstallDialogResult
{
    /// <summary>
    /// Whether the installation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Whether the installation was cancelled.
    /// </summary>
    public bool Cancelled { get; set; }
}
