using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.SaveStates.Entities;
using System;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the branch creation dialog.
/// </summary>
public partial class BranchCreationDialogViewModel : ObservableObject
{
    private Action<BranchCreationResult?>? _closeAction;

    [ObservableProperty]
    private string _branchName = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private BranchType _selectedBranchType = BranchType.StoryBranch;

    [ObservableProperty]
    private string _validationError = string.Empty;

    public BranchType[] AvailableBranchTypes { get; } = Enum.GetValues<BranchType>();

    public void SetCloseAction(Action<BranchCreationResult?> closeAction)
    {
        _closeAction = closeAction;
    }

    [RelayCommand]
    private void Confirm()
    {
        // Validate
        if (string.IsNullOrWhiteSpace(BranchName))
        {
            ValidationError = "Branch name is required.";
            return;
        }

        if (BranchName.Length < 3)
        {
            ValidationError = "Branch name must be at least 3 characters.";
            return;
        }

        var result = new BranchCreationResult(
            BranchName.Trim(),
            Description?.Trim() ?? string.Empty,
            SelectedBranchType);

        _closeAction?.Invoke(result);
    }

    [RelayCommand]
    private void Cancel()
    {
        _closeAction?.Invoke(null);
    }
}

/// <summary>
/// Result from the branch creation dialog.
/// </summary>
public record BranchCreationResult(
    string BranchName,
    string Description,
    BranchType BranchType);
