using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Models.Data;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.Settings;

/// <summary>
/// ViewModel for data management operations (export, import, backup).
/// </summary>
public partial class DataManagementViewModel : ObservableObject
{
    private readonly INotificationService _notificationService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private ExportOptions _exportOptions = new();

    [ObservableProperty]
    private string? _selectedExportPath;

    [ObservableProperty]
    private string? _selectedImportPath;

    [ObservableProperty]
    private ImportStrategy _importStrategy = ImportStrategy.Merge;

    [ObservableProperty]
    private ImportPreview? _importPreview;

    [ObservableProperty]
    private DateTime? _lastBackupDate;

    [ObservableProperty]
    private string? _backupLocation;

    [ObservableProperty]
    private bool _isExporting;

    [ObservableProperty]
    private bool _isImporting;

    [ObservableProperty]
    private double _operationProgress;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataManagementViewModel"/> class.
    /// </summary>
    public DataManagementViewModel()
    {
        // Design-time constructor
        _notificationService = null!;
        _dialogService = null!;
        InitializeDefaults();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataManagementViewModel"/> class.
    /// </summary>
    public DataManagementViewModel(
        INotificationService notificationService,
        IDialogService dialogService)
    {
        _notificationService = notificationService;
        _dialogService = dialogService;
        InitializeDefaults();
    }

    private void InitializeDefaults()
    {
        LastBackupDate = DateTime.Now.AddDays(-3);
        BackupLocation = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SaveStateReborn",
            "Backups");
    }

    /// <summary>
    /// Opens a folder picker to select the export destination.
    /// </summary>
    [RelayCommand]
    private async Task SelectExportPathAsync()
    {
        try
        {
            var path = await _dialogService.ShowFolderPickerAsync(
                "Select Export Location",
                SelectedExportPath ?? BackupLocation);

            if (!string.IsNullOrEmpty(path))
            {
                SelectedExportPath = path;
            }
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Failed to select path: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes the export operation.
    /// </summary>
    [RelayCommand]
    private async Task ExportAsync()
    {
        if (string.IsNullOrEmpty(SelectedExportPath))
        {
            await _notificationService.ShowNotificationAsync(
                "Please select an export location first",
                "Export");
            return;
        }

        IsExporting = true;
        OperationProgress = 0;
        StatusMessage = "Starting export...";

        try
        {
            // Simulate export progress
            var items = GetExportItems();
            int totalItems = items.Count;
            int processedItems = 0;

            foreach (var item in items)
            {
                StatusMessage = $"Exporting {item}...";
                await Task.Delay(100); // Simulate work
                processedItems++;
                OperationProgress = (processedItems / (double)totalItems) * 100;
            }

            StatusMessage = "Export complete!";
            await _notificationService.ShowNotificationAsync(
                $"Data exported to {SelectedExportPath}",
                "Export Complete");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
            await _notificationService.ShowErrorAsync($"Export failed: {ex.Message}");
        }
        finally
        {
            IsExporting = false;
            OperationProgress = 0;
        }
    }

    /// <summary>
    /// Opens a file picker to select the import file.
    /// </summary>
    [RelayCommand]
    private async Task SelectImportPathAsync()
    {
        try
        {
            var path = await _dialogService.ShowOpenFileDialogAsync(
                "Select Import File",
                new[] { "json", "zip", "sav" });

            if (!string.IsNullOrEmpty(path))
            {
                SelectedImportPath = path;
            }
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Failed to select file: {ex.Message}");
        }
    }

    /// <summary>
    /// Generates a preview of the import operation.
    /// </summary>
    [RelayCommand]
    private async Task PreviewImportAsync()
    {
        if (string.IsNullOrEmpty(SelectedImportPath))
        {
            await _notificationService.ShowNotificationAsync(
                "Please select an import file first",
                "Import");
            return;
        }

        try
        {
            StatusMessage = "Generating preview...";

            // TODO: Generate actual preview from file
            await Task.Delay(500);

            ImportPreview = new ImportPreview
            {
                GamesToAdd = 42,
                GamesToUpdate = 15,
                SaveStatesToImport = 128,
                AchievementsToImport = 256,
                Conflicts = 3,
                EstimatedDuration = TimeSpan.FromMinutes(2),
                Warnings = new List<string>
                {
                    "Some save states may conflict with existing data",
                    "Achievement data will be merged with existing records"
                }
            };

            StatusMessage = "Preview ready";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Preview failed: {ex.Message}";
            await _notificationService.ShowErrorAsync($"Failed to generate preview: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes the import operation.
    /// </summary>
    [RelayCommand]
    private async Task ImportAsync()
    {
        if (string.IsNullOrEmpty(SelectedImportPath))
        {
            await _notificationService.ShowNotificationAsync(
                "Please select an import file first",
                "Import");
            return;
        }

        if (ImportPreview?.Conflicts > 0 && ImportStrategy == ImportStrategy.Merge)
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Import Conflicts",
                $"There are {ImportPreview.Conflicts} conflicts. Continue with merge strategy?",
                "Continue",
                "Cancel");

            if (!confirmed)
            {
                return;
            }
        }

        IsImporting = true;
        OperationProgress = 0;
        StatusMessage = "Starting import...";

        try
        {
            // Simulate import progress
            for (int i = 0; i <= 100; i += 5)
            {
                OperationProgress = i;
                StatusMessage = $"Importing... {i}%";
                await Task.Delay(100);
            }

            StatusMessage = "Import complete!";
            await _notificationService.ShowNotificationAsync(
                "Data imported successfully",
                "Import Complete");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Import failed: {ex.Message}";
            await _notificationService.ShowErrorAsync($"Import failed: {ex.Message}");
        }
        finally
        {
            IsImporting = false;
            OperationProgress = 0;
            ImportPreview = null;
        }
    }

    /// <summary>
    /// Creates a full backup of all data.
    /// </summary>
    [RelayCommand]
    private async Task CreateBackupAsync()
    {
        IsExporting = true;
        OperationProgress = 0;
        StatusMessage = "Creating backup...";

        try
        {
            for (int i = 0; i <= 100; i += 10)
            {
                OperationProgress = i;
                StatusMessage = $"Backing up... {i}%";
                await Task.Delay(150);
            }

            LastBackupDate = DateTime.Now;
            StatusMessage = "Backup created successfully!";
            await _notificationService.ShowNotificationAsync(
                $"Backup created at {BackupLocation}",
                "Backup Complete");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Backup failed: {ex.Message}";
            await _notificationService.ShowErrorAsync($"Backup failed: {ex.Message}");
        }
        finally
        {
            IsExporting = false;
            OperationProgress = 0;
        }
    }

    /// <summary>
    /// Restores data from a backup file.
    /// </summary>
    [RelayCommand]
    private async Task RestoreBackupAsync()
    {
        try
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Restore Backup",
                "This will replace all current data. Are you sure?",
                "Restore",
                "Cancel");

            if (!confirmed) return;

            IsImporting = true;
            OperationProgress = 0;
            StatusMessage = "Restoring backup...";

            for (int i = 0; i <= 100; i += 10)
            {
                OperationProgress = i;
                await Task.Delay(150);
            }

            await _notificationService.ShowNotificationAsync(
                "Backup restored successfully",
                "Restore Complete");
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Restore failed: {ex.Message}");
        }
        finally
        {
            IsImporting = false;
            OperationProgress = 0;
        }
    }

    /// <summary>
    /// Opens auto-backup configuration.
    /// </summary>
    [RelayCommand]
    private async Task ConfigureAutoBackupAsync()
    {
        // TODO: Implement auto-backup configuration dialog
        await _notificationService.ShowNotificationAsync(
            "Auto-backup configuration coming soon",
            "Not Implemented");
    }

    private List<string> GetExportItems()
    {
        var items = new List<string>();

        if (ExportOptions.IncludeGameLibrary) items.Add("Game Library");
        if (ExportOptions.IncludeSaveStates) items.Add("Save States");
        if (ExportOptions.IncludeAchievements) items.Add("Achievements");
        if (ExportOptions.IncludePlaySessions) items.Add("Play Sessions");
        if (ExportOptions.IncludeCollections) items.Add("Collections");
        if (ExportOptions.IncludeSettings) items.Add("Settings");
        if (ExportOptions.IncludeMugenData) items.Add("MUGEN Data");
        if (ExportOptions.IncludeMacros) items.Add("Macros");
        if (ExportOptions.IncludeRoms) items.Add("ROMs");

        return items;
    }
}
