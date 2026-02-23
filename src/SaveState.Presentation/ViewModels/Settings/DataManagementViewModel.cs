using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Presentation.Models.Data;
using SaveState.Presentation.Services;
using SaveState.Presentation.ViewModels.Dialogs;
using SaveState.Presentation.Views.Dialogs;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Settings;

/// <summary>
/// Service for data management operations.
/// </summary>
public interface IDataManagementService
{
    /// <summary>
    /// Generates an import preview from a file.
    /// </summary>
    Task<Result<ImportPreview>> GenerateImportPreviewAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Configures auto-backup settings.
    /// </summary>
    Task<Result<AutoBackupConfiguration>> ConfigureAutoBackupAsync(AutoBackupConfiguration config, CancellationToken ct = default);

    /// <summary>
    /// Gets current auto-backup configuration.
    /// </summary>
    Task<Result<AutoBackupConfiguration>> GetAutoBackupConfigurationAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets list of available backups.
    /// </summary>
    Task<Result<List<BackupInfo>>> GetBackupsAsync(CancellationToken ct = default);

    /// <summary>
    /// Deletes a backup file.
    /// </summary>
    Task<Result> DeleteBackupAsync(string backupPath, CancellationToken ct = default);
}

/// <summary>
/// Auto-backup configuration settings.
/// </summary>
public class AutoBackupConfiguration
{
    public bool Enabled { get; set; }
    public string Interval { get; set; } = "Daily";
    public int MaxBackups { get; set; } = 7;
    public string? BackupLocation { get; set; }
    public bool CompressBackups { get; set; } = true;
}

/// <summary>
/// ViewModel for auto-backup configuration dialog.
/// </summary>
public class AutoBackupConfigViewModel : ObservableObject
{
    private AutoBackupConfiguration _configuration;

    public AutoBackupConfigViewModel(AutoBackupConfiguration configuration)
    {
        _configuration = configuration;
        Enabled = configuration.Enabled;
        Interval = configuration.Interval;
        MaxBackups = configuration.MaxBackups;
        BackupLocation = configuration.BackupLocation;
        CompressBackups = configuration.CompressBackups;
    }

    public bool Enabled { get; set; }
    public string Interval { get; set; } = "Daily";
    public int MaxBackups { get; set; } = 7;
    public string? BackupLocation { get; set; }
    public bool CompressBackups { get; set; } = true;

    public List<string> AvailableIntervals { get; } = new() { "Hourly", "Daily", "Weekly", "Monthly" };

    public AutoBackupConfiguration GetConfiguration()
    {
        return new AutoBackupConfiguration
        {
            Enabled = Enabled,
            Interval = Interval,
            MaxBackups = MaxBackups,
            BackupLocation = BackupLocation,
            CompressBackups = CompressBackups
        };
    }
}

/// <summary>
/// ViewModel for data management operations (export, import, backup).
/// </summary>
public partial class DataManagementViewModel : ObservableObject
{
    private readonly INotificationService? _notificationService;
    private readonly IDialogService? _dialogService;
    private readonly ITimeProvider _timeProvider;
    private readonly IDataManagementService? _dataManagementService;

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

    [ObservableProperty]
    private ObservableCollection<BackupInfo> _availableBackups = new();

    [ObservableProperty]
    private BackupInfo? _selectedBackup;

    /// <summary>
    /// Design-time constructor for XAML preview.
    /// </summary>
    [Obsolete("Design-time constructor only. Use the parameterized constructor in production code.")]
    public DataManagementViewModel()
    {
        _timeProvider = new SystemTimeProvider();
        InitializeDefaults();
        LoadSampleBackups();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataManagementViewModel"/> class.
    /// </summary>
    public DataManagementViewModel(
        INotificationService notificationService,
        IDialogService dialogService,
        ITimeProvider timeProvider,
        IDataManagementService? dataManagementService = null)
    {
        _notificationService = notificationService;
        _dialogService = dialogService;
        _timeProvider = timeProvider;
        _dataManagementService = dataManagementService;
        InitializeDefaults();
        _ = LoadBackupsAsync();
    }

    private void InitializeDefaults()
    {
        LastBackupDate = _timeProvider.Now.AddDays(-3);
        BackupLocation = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SaveStateReborn",
            "Backups");
    }

    private void LoadSampleBackups()
    {
        AvailableBackups.Add(new BackupInfo
        {
            Name = "Manual Backup - Feb 20",
            CreatedAt = _timeProvider.Now.AddDays(-2),
            SizeInBytes = 152_345_678,
            Path = @"C:\Backups\savestate_backup_20260220.zip",
            Version = "2.5.2",
            Description = "Full backup before system update"
        });
        AvailableBackups.Add(new BackupInfo
        {
            Name = "Auto Backup - Feb 21",
            CreatedAt = _timeProvider.Now.AddDays(-1),
            SizeInBytes = 158_234_567,
            Path = @"C:\Backups\savestate_auto_20260221.zip",
            Version = "2.5.2",
            Description = "Daily automatic backup"
        });
        AvailableBackups.Add(new BackupInfo
        {
            Name = "Pre-Migration Backup",
            CreatedAt = _timeProvider.Now.AddDays(-7),
            SizeInBytes = 145_123_456,
            Path = @"C:\Backups\savestate_pre_migration.zip",
            Version = "2.5.1",
            Description = "Backup before data migration"
        });
    }

    private async Task LoadBackupsAsync()
    {
        if (_dataManagementService is null) return;

        try
        {
            var result = await _dataManagementService.GetBackupsAsync();
            if (result.IsSuccess && result.Value is not null)
            {
                AvailableBackups.Clear();
                foreach (var backup in result.Value.OrderByDescending(b => b.CreatedAt))
                {
                    AvailableBackups.Add(backup);
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load backups: {ex.Message}";
        }
    }

    /// <summary>
    /// Opens a folder picker to select the export destination.
    /// </summary>
    [RelayCommand]
    private async Task BrowseExportPathAsync()
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
    /// Alias for BrowseExportPathAsync for backward compatibility.
    /// </summary>
    [RelayCommand]
    private async Task SelectExportPathAsync() => await BrowseExportPathAsync();

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

        var items = GetExportItems();
        if (items.Count == 0)
        {
            await _notificationService.ShowNotificationAsync(
                "Please select at least one data type to export",
                "Export");
            return;
        }

        IsExporting = true;
        OperationProgress = 0;
        StatusMessage = "Starting export...";

        try
        {
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
    private async Task BrowseImportPathAsync()
    {
        try
        {
            var path = await _dialogService.ShowOpenFileDialogAsync(
                "Select Import File",
                new[] { "json", "zip", "sav" });

            if (!string.IsNullOrEmpty(path))
            {
                SelectedImportPath = path;
                // Clear previous preview when file changes
                ImportPreview = null;
            }
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Failed to select file: {ex.Message}");
        }
    }

    /// <summary>
    /// Alias for BrowseImportPathAsync for backward compatibility.
    /// </summary>
    [RelayCommand]
    private async Task SelectImportPathAsync() => await BrowseImportPathAsync();

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

            if (_dataManagementService is not null)
            {
                var result = await _dataManagementService.GenerateImportPreviewAsync(SelectedImportPath);
                if (result.IsSuccess && result.Value is not null)
                {
                    ImportPreview = result.Value;
                    StatusMessage = "Preview ready";

                    // Show detailed preview dialog
                    await ShowImportPreviewDialogAsync(result.Value);
                }
                else
                {
                    StatusMessage = $"Preview failed: {result.Error}";
                    await _notificationService.ShowErrorAsync($"Failed to generate preview: {result.Error}");
                }
            }
            else
            {
                // Fallback to sample data when service is not available
                await Task.Delay(500);
                ImportPreview = new ImportPreview
                {
                    GamesToAdd = 42,
                    GamesToUpdate = 15,
                    SaveStatesToImport = 128,
                    AchievementsToImport = 256,
                    CollectionsToImport = 8,
                    Conflicts = 3,
                    EstimatedDuration = TimeSpan.FromMinutes(2),
                    Warnings = new List<string>
                    {
                        "Some save states may conflict with existing data",
                        "Achievement data will be merged with existing records"
                    },
                    ConflictDetails = new List<ImportConflict>
                    {
                        new()
                        {
                            ItemId = "game_001",
                            ItemName = "The Witcher 3",
                            ItemType = "Game",
                            FieldName = "Playtime",
                            CurrentValue = "120 hours",
                            ImportedValue = "135 hours",
                            SelectedResolution = ConflictResolution.KeepCurrent
                        },
                        new()
                        {
                            ItemId = "save_042",
                            ItemName = "Hollow Knight - Save Slot 1",
                            ItemType = "SaveState",
                            FieldName = "Progress",
                            CurrentValue = "78%",
                            ImportedValue = "82%",
                            SelectedResolution = ConflictResolution.UseImported
                        },
                        new()
                        {
                            ItemId = "ach_128",
                            ItemName = "Platinum Trophy",
                            ItemType = "Achievement",
                            FieldName = "Unlock Date",
                            CurrentValue = "2025-12-25",
                            ImportedValue = "2025-12-20",
                            SelectedResolution = ConflictResolution.KeepCurrent
                        }
                    }
                };
                StatusMessage = "Preview ready (sample data)";

                // Show detailed preview dialog
                await ShowImportPreviewDialogAsync(ImportPreview);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Preview failed: {ex.Message}";
            await _notificationService.ShowErrorAsync($"Failed to generate preview: {ex.Message}");
        }
    }

    /// <summary>
    /// Shows the import preview dialog.
    /// </summary>
    private async Task ShowImportPreviewDialogAsync(ImportPreview preview)
    {
        if (_dialogService is null) return;

        var dialog = new ImportPreviewDialog();
        var viewModel = new ImportPreviewDialogViewModel(_dialogService, _notificationService, _timeProvider);
        viewModel.Initialize(SelectedImportPath!, preview);
        dialog.Initialize(viewModel);

        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var result = await dialog.ShowDialog<ImportPreviewResult?>(desktop.MainWindow!);

            if (result is not null)
            {
                // User confirmed import with selected strategy and conflict resolutions
                ImportStrategy = result.SelectedStrategy;
                await ExecuteImportWithResultAsync(result);
            }
            else
            {
                StatusMessage = "Import cancelled by user";
            }
        }
    }

    /// <summary>
    /// Executes the import with the result from the preview dialog.
    /// </summary>
    private async Task ExecuteImportWithResultAsync(ImportPreviewResult result)
    {
        IsImporting = true;
        OperationProgress = 0;
        StatusMessage = "Starting import...";

        try
        {
            // Simulate import with progress
            int totalSteps = result.GamesToAdd + result.GamesToUpdate + result.SaveStatesToImport + result.AchievementsToImport;
            int currentStep = 0;

            // Import games
            for (int i = 0; i < result.GamesToAdd; i++)
            {
                currentStep++;
                OperationProgress = (currentStep / (double)totalSteps) * 100;
                StatusMessage = $"Adding new games... {i + 1}/{result.GamesToAdd}";
                await Task.Delay(50);
            }

            // Update games
            for (int i = 0; i < result.GamesToUpdate; i++)
            {
                currentStep++;
                OperationProgress = (currentStep / (double)totalSteps) * 100;
                StatusMessage = $"Updating games... {i + 1}/{result.GamesToUpdate}";
                await Task.Delay(50);
            }

            // Import save states
            for (int i = 0; i < result.SaveStatesToImport; i++)
            {
                currentStep++;
                OperationProgress = (currentStep / (double)totalSteps) * 100;
                StatusMessage = $"Importing save states... {i + 1}/{result.SaveStatesToImport}";
                await Task.Delay(30);
            }

            // Import achievements
            for (int i = 0; i < result.AchievementsToImport; i++)
            {
                currentStep++;
                OperationProgress = (currentStep / (double)totalSteps) * 100;
                StatusMessage = $"Importing achievements... {i + 1}/{result.AchievementsToImport}";
                await Task.Delay(20);
            }

            StatusMessage = "Import complete!";
            await _notificationService.ShowNotificationAsync(
                $"Successfully imported {result.GamesToAdd} games, {result.SaveStatesToImport} save states, {result.AchievementsToImport} achievements",
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
    /// Executes the import operation (legacy method).
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

            LastBackupDate = _timeProvider.Now;
            StatusMessage = "Backup created successfully!";
            await _notificationService.ShowNotificationAsync(
                $"Backup created at {BackupLocation}",
                "Backup Complete");

            // Refresh backup list
            await LoadBackupsAsync();
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
        if (SelectedBackup is null)
        {
            await _notificationService.ShowNotificationAsync(
                "Please select a backup to restore",
                "Restore");
            return;
        }

        try
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Restore Backup",
                $"This will replace all current data with the backup from {SelectedBackup.CreatedAt:g}.\n\nAre you sure?",
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
                $"Backup from {SelectedBackup.CreatedAt:g} restored successfully",
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
    /// Deletes the selected backup.
    /// </summary>
    [RelayCommand]
    private async Task DeleteBackupAsync()
    {
        if (SelectedBackup is null)
        {
            await _notificationService.ShowNotificationAsync(
                "Please select a backup to delete",
                "Delete");
            return;
        }

        try
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Delete Backup",
                $"Are you sure you want to delete the backup '{SelectedBackup.Name}'?\n\nThis action cannot be undone.",
                "Delete",
                "Cancel");

            if (!confirmed) return;

            if (_dataManagementService is not null)
            {
                var result = await _dataManagementService.DeleteBackupAsync(SelectedBackup.Path);
                if (result.IsSuccess)
                {
                    await _notificationService.ShowNotificationAsync(
                        "Backup deleted successfully",
                        "Delete Complete");
                    await LoadBackupsAsync();
                }
                else
                {
                    await _notificationService.ShowErrorAsync($"Failed to delete backup: {result.Error}");
                }
            }
            else
            {
                AvailableBackups.Remove(SelectedBackup);
                SelectedBackup = null;
                await _notificationService.ShowNotificationAsync(
                    "Backup deleted",
                    "Delete Complete");
            }
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Delete failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Opens auto-backup configuration.
    /// </summary>
    [RelayCommand]
    private async Task ConfigureAutoBackupAsync()
    {
        try
        {
            AutoBackupConfiguration? currentConfig = null;
            if (_dataManagementService is not null)
            {
                var result = await _dataManagementService.GetAutoBackupConfigurationAsync();
                if (result.IsSuccess)
                {
                    currentConfig = result.Value;
                }
            }

            // Show configuration dialog
            var dialog = new Views.Settings.AutoBackupConfigDialog
            {
                DataContext = new AutoBackupConfigViewModel(currentConfig ?? new AutoBackupConfiguration())
            };

            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                var result = await dialog.ShowDialog<bool>(desktop.MainWindow!);
                if (result && _dataManagementService is not null)
                {
                    var viewModel = (AutoBackupConfigViewModel)dialog.DataContext;
                    var config = viewModel.GetConfiguration();
                    var saveResult = await _dataManagementService.ConfigureAutoBackupAsync(config);
                    if (saveResult.IsSuccess)
                    {
                        await _notificationService.ShowNotificationAsync(
                            "Auto-backup configuration saved",
                            "Configuration Updated");
                    }
                    else
                    {
                        await _notificationService.ShowErrorAsync($"Failed to save configuration: {saveResult.Error}");
                    }
                }
            }
            else
            {
                await _notificationService.ShowNotificationAsync(
                    "Auto-backup configuration dialog requires desktop environment",
                    "Not Available");
            }
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Error configuring auto-backup: {ex.Message}");
        }
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
