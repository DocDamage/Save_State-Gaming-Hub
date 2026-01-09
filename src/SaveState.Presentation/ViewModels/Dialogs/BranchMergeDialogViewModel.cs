using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// View model for merging branches with conflict resolution.
/// </summary>
public partial class BranchMergeDialogViewModel : ObservableObject
{
    private readonly ILogger<BranchMergeDialogViewModel> _logger;

    [ObservableProperty]
    private string _sourceBranchName = string.Empty;

    [ObservableProperty]
    private string _targetBranchName = string.Empty;

    [ObservableProperty]
    private ObservableCollection<SaveStateDiffViewModel> _conflicts = new();

    [ObservableProperty]
    private ObservableCollection<string> _mergeStrategies = new()
    {
        "Keep Both (Create Duplicates)",
        "Prefer Source Branch",
        "Prefer Target Branch",
        "Manual Resolution"
    };

    [ObservableProperty]
    private string _selectedMergeStrategy = "Keep Both (Create Duplicates)";

    [ObservableProperty]
    private bool _hasConflicts;

    [ObservableProperty]
    private int _conflictCount;

    [ObservableProperty]
    private bool _canMerge;

    [ObservableProperty]
    private string _warningMessage = string.Empty;

    public BranchMergeDialogViewModel(ILogger<BranchMergeDialogViewModel> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initializes the merge dialog with conflict information.
    /// </summary>
    public void Initialize(
        string sourceBranchName,
        string targetBranchName,
        SaveStateDiffViewModel[] conflicts)
    {
        SourceBranchName = sourceBranchName;
        TargetBranchName = targetBranchName;

        Conflicts.Clear();
        foreach (var conflict in conflicts)
        {
            Conflicts.Add(conflict);
        }

        ConflictCount = Conflicts.Count;
        HasConflicts = ConflictCount > 0;

        UpdateWarningMessage();
        UpdateCanMerge();
    }

    partial void OnSelectedMergeStrategyChanged(string value)
    {
        UpdateCanMerge();
    }

    private void UpdateWarningMessage()
    {
        if (!HasConflicts)
        {
            WarningMessage = "✓ No conflicts detected. Merge can proceed automatically.";
        }
        else
        {
            WarningMessage = $"⚠️ {ConflictCount} conflict{(ConflictCount == 1 ? "" : "s")} detected. " +
                           "Choose a merge strategy to resolve them.";
        }
    }

    private void UpdateCanMerge()
    {
        // Can merge if no conflicts OR if a strategy is selected
        CanMerge = !HasConflicts || !string.IsNullOrEmpty(SelectedMergeStrategy);
    }

    [RelayCommand]
    private void SelectStrategy(string strategy)
    {
        SelectedMergeStrategy = strategy;
    }

    [RelayCommand]
    private void ResolveConflict(SaveStateDiffViewModel conflict)
    {
        _logger.LogInformation("Manual resolution requested for conflict: {ConflictName}", conflict.Name);
        // In a full implementation, this would open a detailed conflict resolution dialog
    }

    [RelayCommand(CanExecute = nameof(CanMerge))]
    private void ConfirmMerge()
    {
        _logger.LogInformation(
            "Merging branch '{SourceBranch}' into '{TargetBranch}' using strategy '{Strategy}'",
            SourceBranchName,
            TargetBranchName,
            SelectedMergeStrategy);
        // Dialog result will be handled by the caller
    }

    [RelayCommand]
    private void Cancel()
    {
        _logger.LogDebug("Branch merge cancelled");
        // Dialog result will be handled by the caller
    }
}
