using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Services;
using SaveState.Infrastructure.GameLibrary.Models;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the Cheat Engine table import dialog.
/// </summary>
public partial class ImportCheatTableViewModel : ObservableObject
{
    private readonly ICheatEngineImporter _importer;
    private readonly IMemoryPatternDatabase? _patternDatabase;
    private readonly IDialogService _dialogService;
    private Action<ImportCheatTableResult?>? _closeAction;

    [ObservableProperty]
    private string _selectedFilePath = string.Empty;

    [ObservableProperty]
    private string _gameTitle = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Drop a .CT file or click Browse to select";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasPreview;

    [ObservableProperty]
    private bool _hasErrors;

    [ObservableProperty]
    private bool _isImporting;

    [ObservableProperty]
    private double _importProgress;

    [ObservableProperty]
    private ObservableCollection<CheatEntryViewModel> _entries = new();

    [ObservableProperty]
    private bool _skipDuplicates = true;

    [ObservableProperty]
    private bool _overwriteExisting;

    [ObservableProperty]
    private bool _includeScripts;

    [ObservableProperty]
    private string _validationError = string.Empty;

    [ObservableProperty]
    private int _selectedEntryCount;

    [ObservableProperty]
    private int _totalEntryCount;

    [ObservableProperty]
    private bool _showResults;

    [ObservableProperty]
    private CheatEngineImportResult? _cheatEngineImportResult;

    public ImportCheatTableViewModel(
        ICheatEngineImporter importer,
        IDialogService dialogService,
        IMemoryPatternDatabase? patternDatabase = null)
    {
        _importer = importer;
        _dialogService = dialogService;
        _patternDatabase = patternDatabase;

        // Subscribe to collection changes to update selected count
        Entries.CollectionChanged += (s, e) => UpdateSelectedCount();
    }

    public void SetCloseAction(Action<ImportCheatTableResult?> closeAction)
    {
        _closeAction = closeAction;
    }

    /// <summary>
    /// Handles file drop events for drag-and-drop support.
    /// </summary>
    public async Task HandleFileDropAsync(string[] filePaths)
    {
        if (filePaths.Length == 0) return;

        var ctFile = filePaths.FirstOrDefault(f => 
            f.EndsWith(".ct", StringComparison.OrdinalIgnoreCase));

        if (ctFile == null)
        {
            ValidationError = "Please drop a .CT (Cheat Engine Table) file.";
            return;
        }

        await LoadFileAsync(ctFile);
    }

    [RelayCommand]
    private async Task BrowseFileAsync()
    {
        var filePath = await _dialogService.ShowOpenFileDialogAsync(
            "Select Cheat Engine Table",
            new[] { "ct" });

        if (!string.IsNullOrEmpty(filePath))
        {
            await LoadFileAsync(filePath);
        }
    }

    [RelayCommand]
    private async Task LoadFileAsync(string? filePath = null)
    {
        var path = filePath ?? SelectedFilePath;
        if (string.IsNullOrEmpty(path)) return;

        IsLoading = true;
        StatusMessage = "Loading file...";
        ValidationError = string.Empty;

        try
        {
            if (!_importer.CanParseFile(path))
            {
                ValidationError = "The selected file does not appear to be a valid Cheat Engine table.";
                IsLoading = false;
                return;
            }

            // Extract game title from filename
            var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            GameTitle = CleanGameTitle(fileName);

            // Get preview
            var previewResult = _importer.PreviewFile(path);
            if (previewResult.IsFailure)
            {
                ValidationError = previewResult.Error ?? "Failed to preview file.";
                IsLoading = false;
                return;
            }

            SelectedFilePath = path;
            var preview = previewResult.Value!;

            // Populate entries
            Entries.Clear();
            foreach (var entry in preview.Entries)
            {
                Entries.Add(new CheatEntryViewModel(entry)
                {
                    IsSelected = entry.CanImport
                });
            }

            TotalEntryCount = Entries.Count;
            HasPreview = true;
            StatusMessage = $"Found {Entries.Count} entries" + 
                (preview.HasScripts ? $" ({preview.ScriptCount} scripts)" : "");

            UpdateSelectedCount();
        }
        catch (Exception ex)
        {
            ValidationError = $"Error loading file: {ex.Message}";
            StatusMessage = "Failed to load file";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var entry in Entries)
            entry.IsSelected = true;
        UpdateSelectedCount();
    }

    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var entry in Entries)
            entry.IsSelected = false;
        UpdateSelectedCount();
    }

    [RelayCommand]
    private void SelectOnlyImportable()
    {
        foreach (var entry in Entries)
            entry.IsSelected = entry.CanImport;
        UpdateSelectedCount();
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        if (string.IsNullOrEmpty(SelectedFilePath))
        {
            ValidationError = "Please select a file to import.";
            return;
        }

        if (SelectedEntryCount == 0)
        {
            ValidationError = "Please select at least one entry to import.";
            return;
        }

        if (string.IsNullOrWhiteSpace(GameTitle))
        {
            ValidationError = "Please enter a game title.";
            return;
        }

        IsImporting = true;
        StatusMessage = "Importing...";
        ValidationError = string.Empty;

        try
        {
            var options = new CheatEngineImportOptions
            {
                GameTitle = GameTitle.Trim(),
                SkipDuplicates = SkipDuplicates,
                OverwriteExisting = OverwriteExisting,
                IncludeScripts = IncludeScripts,
                ProgressCallback = progress =>
                {
                    ImportProgress = progress.PercentComplete;
                    StatusMessage = $"Processing {progress.CurrentFileIndex} of {progress.TotalFiles}: {progress.StatusMessage}";
                }
            };

            // Get selected entry names for filtering
            var selectedNames = Entries
                .Where(e => e.IsSelected)
                .Select(e => e.Description)
                .ToHashSet();

            var result = _importer.ImportFromFile(SelectedFilePath, options);

            if (result.IsFailure)
            {
                ValidationError = result.Error ?? "Import failed.";
                IsImporting = false;
                return;
            }

            CheatEngineImportResult = result.Value;
            ShowResults = true;

            // Filter to only show selected entries in results
            if (CheatEngineImportResult != null)
            {
                CheatEngineImportResult.ImportedSignatures = CheatEngineImportResult.ImportedSignatures
                    .Where(s => selectedNames.Contains(s.Name))
                    .ToList();
            }

            StatusMessage = $"Import complete: {CheatEngineImportResult?.GetSummary()}";
        }
        catch (Exception ex)
        {
            ValidationError = $"Import error: {ex.Message}";
            StatusMessage = "Import failed";
        }
        finally
        {
            IsImporting = false;
        }
    }

    [RelayCommand]
    private void Confirm()
    {
        if (CheatEngineImportResult == null)
        {
            _closeAction?.Invoke(null);
            return;
        }

        var result = new ImportCheatTableResult(
            CheatEngineImportResult.ImportedSignatures.ToList(),
            CheatEngineImportResult.SuccessfullyImported,
            CheatEngineImportResult.Skipped,
            CheatEngineImportResult.Failed,
            CheatEngineImportResult.Errors.Select(e => $"{e.EntryName}: {e.Message}").ToList()
        );

        _closeAction?.Invoke(result);
    }

    [RelayCommand]
    private void Cancel()
    {
        _closeAction?.Invoke(null);
    }

    [RelayCommand]
    private void CloseResults()
    {
        ShowResults = false;
    }

    partial void OnSkipDuplicatesChanged(bool value)
    {
        if (value)
            OverwriteExisting = false;
    }

    partial void OnOverwriteExistingChanged(bool value)
    {
        if (value)
            SkipDuplicates = false;
    }

    private void UpdateSelectedCount()
    {
        SelectedEntryCount = Entries.Count(e => e.IsSelected);
    }

    private static string CleanGameTitle(string fileName)
    {
        // Remove common suffixes like "_Cheats", "_Table", version numbers
        var title = System.Text.RegularExpressions.Regex.Replace(
            fileName, 
            @"_+(cheats?|table|v?\d+\.?\d*).*$", 
            "", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Replace underscores and dots with spaces
        title = title.Replace('_', ' ').Replace('.', ' ');

        // Title case
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(title.ToLower());
    }
}

/// <summary>
/// ViewModel for a single cheat entry in the import list.
/// </summary>
public partial class CheatEntryViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private string _description = "";

    [ObservableProperty]
    private string _address = "";

    [ObservableProperty]
    private string _variableType = "";

    [ObservableProperty]
    private bool _isPointer;

    [ObservableProperty]
    private bool _isScript;

    [ObservableProperty]
    private bool _canImport;

    [ObservableProperty]
    private string? _importRestriction;

    [ObservableProperty]
    private string? _convertedValueType;

    public CheatEntryViewModel(CheatEngineEntryPreview preview)
    {
        Description = preview.Description;
        Address = preview.Address;
        VariableType = preview.VariableType;
        IsPointer = preview.IsPointer;
        IsScript = preview.IsScript;
        CanImport = preview.CanImport;
        ImportRestriction = preview.ImportRestriction;
        ConvertedValueType = preview.ConvertedValueType;
        IsSelected = preview.CanImport;
    }
}

/// <summary>
/// Result from the Cheat Engine table import dialog.
/// </summary>
public record ImportCheatTableResult(
    List<GameMemorySignature> ImportedSignatures,
    int SuccessfullyImported,
    int Skipped,
    int Failed,
    List<string> ErrorMessages);
