using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Presentation.Models.Data;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the import preview dialog showing what will be imported.
/// </summary>
public partial class ImportPreviewDialogViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly INotificationService _notificationService;
    private readonly ITimeProvider _timeProvider;
    private Action<ImportPreviewResult?>? _closeAction;

    [ObservableProperty]
    private string _importFilePath = string.Empty;

    [ObservableProperty]
    private string _importFileName = string.Empty;

    [ObservableProperty]
    private long _importFileSize;

    [ObservableProperty]
    private ImportStrategy _selectedStrategy = ImportStrategy.Merge;

    [ObservableProperty]
    private ImportPreview? _preview;

    [ObservableProperty]
    private ObservableCollection<ImportConflictViewModel> _conflicts = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasErrors;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isDestructive;

    [ObservableProperty]
    private bool _showConflictDetails;

    /// <summary>
    /// Design-time constructor for XAML preview.
    /// </summary>
    [Obsolete("Design-time constructor only. Use the parameterized constructor in production code.")]
    public ImportPreviewDialogViewModel()
    {
        _dialogService = null!;
        _notificationService = null!;
        _timeProvider = new SystemTimeProvider();
        InitializeSampleData();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportPreviewDialogViewModel"/> class.
    /// </summary>
    public ImportPreviewDialogViewModel(
        IDialogService dialogService,
        INotificationService notificationService,
        ITimeProvider timeProvider)
    {
        _dialogService = dialogService;
        _notificationService = notificationService;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Sets the action to invoke when closing the dialog.
    /// </summary>
    public void SetCloseAction(Action<ImportPreviewResult?> closeAction)
    {
        _closeAction = closeAction;
    }

    /// <summary>
    /// Initializes the preview with file information.
    /// </summary>
    public void Initialize(string filePath, ImportPreview preview)
    {
        ImportFilePath = filePath;
        ImportFileName = System.IO.Path.GetFileName(filePath);

        try
        {
            var fileInfo = new System.IO.FileInfo(filePath);
            ImportFileSize = fileInfo.Length;
        }
        catch
        {
            ImportFileSize = 0;
        }

        Preview = preview;

        // Check if destructive operation
        IsDestructive = SelectedStrategy == ImportStrategy.Replace ||
                        (SelectedStrategy == ImportStrategy.Merge && preview.Conflicts > 0);

        // Populate conflicts
        Conflicts.Clear();
        foreach (var conflict in preview.ConflictDetails)
        {
            Conflicts.Add(new ImportConflictViewModel(conflict));
        }

        ShowConflictDetails = Conflicts.Count > 0;
        StatusMessage = $"Found {preview.GamesToAdd} new games, {preview.GamesToUpdate} updates, {preview.Conflicts} conflicts";
    }

    /// <summary>
    /// Changes the import strategy and updates the destructive flag.
    /// </summary>
    [RelayCommand]
    private void ChangeStrategy(ImportStrategy strategy)
    {
        SelectedStrategy = strategy;

        if (Preview != null)
        {
            IsDestructive = strategy == ImportStrategy.Replace ||
                            (strategy == ImportStrategy.Merge && Preview.Conflicts > 0);
        }

        // Update conflict resolutions based on strategy
        foreach (var conflict in Conflicts)
        {
            conflict.SelectedResolution = strategy switch
            {
                ImportStrategy.Replace => ConflictResolution.UseImported,
                ImportStrategy.OnlyNew => ConflictResolution.KeepCurrent,
                _ => ConflictResolution.KeepCurrent
            };
        }
    }

    /// <summary>
    /// Resolves all conflicts with the same resolution.
    /// </summary>
    [RelayCommand]
    private void ResolveAllConflicts(ConflictResolution resolution)
    {
        foreach (var conflict in Conflicts)
        {
            conflict.SelectedResolution = resolution;
        }
    }

    /// <summary>
    /// Expands all conflict details.
    /// </summary>
    [RelayCommand]
    private void ExpandAllConflicts()
    {
        ShowConflictDetails = true;
        foreach (var conflict in Conflicts)
        {
            conflict.IsExpanded = true;
        }
    }

    /// <summary>
    /// Collapses all conflict details.
    /// </summary>
    [RelayCommand]
    private void CollapseAllConflicts()
    {
        foreach (var conflict in Conflicts)
        {
            conflict.IsExpanded = false;
        }
    }

    /// <summary>
    /// Confirms the import with the current settings.
    /// </summary>
    [RelayCommand]
    private async Task ConfirmImportAsync()
    {
        if (IsDestructive)
        {
            string warningMessage = SelectedStrategy == ImportStrategy.Replace
                ? "This will REPLACE all existing data with the imported data. This action cannot be undone."
                : $"This will affect {Preview?.GamesToUpdate ?? 0} existing items and resolve {Conflicts.Count} conflicts.";

            var confirmed = await _dialogService.ShowConfirmationAsync(
                SelectedStrategy == ImportStrategy.Replace ? "Destructive Operation" : "Confirm Import",
                warningMessage,
                "Continue",
                "Cancel");

            if (!confirmed)
            {
                return;
            }
        }

        // Build conflict resolutions dictionary
        var conflictResolutions = Conflicts.ToDictionary(
            c => c.ItemId,
            c => c.SelectedResolution);

        var result = new ImportPreviewResult(
            ImportFilePath,
            SelectedStrategy,
            conflictResolutions,
            Preview?.GamesToAdd ?? 0,
            Preview?.GamesToUpdate ?? 0,
            Preview?.SaveStatesToImport ?? 0,
            Preview?.AchievementsToImport ?? 0);

        _closeAction?.Invoke(result);
    }

    /// <summary>
    /// Cancels the import operation.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        _closeAction?.Invoke(null);
    }

    /// <summary>
    /// Opens help documentation about import strategies.
    /// </summary>
    [RelayCommand]
    private async Task ShowHelpAsync()
    {
        await _dialogService.ShowMessageAsync(
            "Import Strategy Help",
            "Merge: Combines imported data with existing data, updating matching items.\n\n" +
            "Replace: Completely replaces all existing data with imported data.\n\n" +
            "Add Only: Only imports new items, skipping any that already exist.");
    }

    partial void OnSelectedStrategyChanged(ImportStrategy value)
    {
        if (Preview != null)
        {
            IsDestructive = value == ImportStrategy.Replace ||
                            (value == ImportStrategy.Merge && Preview.Conflicts > 0);
        }
    }

    private void InitializeSampleData()
    {
        ImportFileName = "backup_2026_02_22.json";
        ImportFileSize = 15_728_320; // ~15 MB
        SelectedStrategy = ImportStrategy.Merge;

        Preview = new ImportPreview
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
                "3 games have conflicting metadata that requires resolution"
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
                    CurrentModifiedDate = _timeProvider.Now.AddDays(-5),
                    ImportedModifiedDate = _timeProvider.Now.AddDays(-2),
                    SelectedResolution = ConflictResolution.KeepCurrent
                },
                new()
                {
                    ItemId = "save_042",
                    ItemName = "Hollow Knight - Save Slot 1",
                    ItemType = "SaveState",
                    FieldName = "Progress",
                    CurrentValue = "78% completion",
                    ImportedValue = "82% completion",
                    CurrentModifiedDate = _timeProvider.Now.AddDays(-1),
                    ImportedModifiedDate = _timeProvider.Now.AddDays(-3),
                    SelectedResolution = ConflictResolution.UseImported
                },
                new()
                {
                    ItemId = "ach_128",
                    ItemName = "Platinum Trophy - Elden Ring",
                    ItemType = "Achievement",
                    FieldName = "Unlock Date",
                    CurrentValue = "2025-12-25",
                    ImportedValue = "2025-12-20",
                    CurrentModifiedDate = _timeProvider.Now.AddDays(-30),
                    ImportedModifiedDate = _timeProvider.Now.AddDays(-35),
                    SelectedResolution = ConflictResolution.KeepCurrent
                }
            }
        };

        foreach (var conflict in Preview.ConflictDetails)
        {
            Conflicts.Add(new ImportConflictViewModel(conflict));
        }

        IsDestructive = true;
        ShowConflictDetails = true;
        StatusMessage = "Ready to import";
    }
}

/// <summary>
/// ViewModel for a single import conflict item.
/// </summary>
public partial class ImportConflictViewModel : ObservableObject
{
    [ObservableProperty]
    private ConflictResolution _selectedResolution;

    [ObservableProperty]
    private bool _isExpanded = true;

    public ImportConflictViewModel(ImportConflict conflict)
    {
        ItemId = conflict.ItemId;
        ItemName = conflict.ItemName;
        ItemType = conflict.ItemType;
        FieldName = conflict.FieldName;
        CurrentValue = conflict.CurrentValue;
        ImportedValue = conflict.ImportedValue;
        CurrentModifiedDate = conflict.CurrentModifiedDate;
        ImportedModifiedDate = conflict.ImportedModifiedDate;
        _selectedResolution = conflict.SelectedResolution;
    }

    /// <summary>Unique identifier for the item.</summary>
    public string ItemId { get; }

    /// <summary>Display name of the item.</summary>
    public string ItemName { get; }

    /// <summary>Type of item (Game, SaveState, etc.).</summary>
    public string ItemType { get; }

    /// <summary>Field name that conflicts.</summary>
    public string FieldName { get; }

    /// <summary>Current/existing value.</summary>
    public string CurrentValue { get; }

    /// <summary>Imported value.</summary>
    public string ImportedValue { get; }

    /// <summary>Current modification date.</summary>
    public DateTime? CurrentModifiedDate { get; }

    /// <summary>Imported modification date.</summary>
    public DateTime? ImportedModifiedDate { get; }

    /// <summary>Formatted string showing both values.</summary>
    public string ComparisonText => $"Current: {CurrentValue} → Import: {ImportedValue}";

    /// <summary>Resolution options as strings for binding.</summary>
    public List<string> ResolutionOptions { get; } = new()
    {
        "Keep Current",
        "Use Imported",
        "Keep Both",
        "Skip Item"
    };
}

/// <summary>
/// Result from the import preview dialog.
/// </summary>
public record ImportPreviewResult(
    string ImportFilePath,
    ImportStrategy SelectedStrategy,
    Dictionary<string, ConflictResolution> ConflictResolutions,
    int GamesToAdd,
    int GamesToUpdate,
    int SaveStatesToImport,
    int AchievementsToImport);
