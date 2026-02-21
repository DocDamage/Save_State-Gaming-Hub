using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.SaveStates.Entities;
using System;
using System.Text.RegularExpressions;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the branch creation dialog.
/// </summary>
public partial class BranchCreationDialogViewModel : ObservableObject
{
    private Action<BranchCreationResult?>? _closeAction;

    // Validation constants
    private const int MaxBranchNameLength = 100;
    private const int MaxDescriptionLength = 500;
    private const int MinBranchNameLength = 3;
    private static readonly Regex ValidBranchNamePattern = new Regex(@"^[\w\s\-_]+$", RegexOptions.Compiled);
    private static readonly Regex InvalidCharsPattern = new Regex(@"[<>\x00-\x08\x0B\x0C\x0E-\x1F]", RegexOptions.Compiled);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBranchNameValid))]
    [NotifyPropertyChangedFor(nameof(HasValidationErrors))]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    private string _branchName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDescriptionValid))]
    [NotifyPropertyChangedFor(nameof(HasValidationErrors))]
    private string _description = string.Empty;

    [ObservableProperty]
    private BranchType _selectedBranchType = BranchType.StoryBranch;

    [ObservableProperty]
    private string _validationError = string.Empty;

    public BranchType[] AvailableBranchTypes { get; } = Enum.GetValues<BranchType>();

    /// <summary>
    /// Gets whether the branch name is valid.
    /// </summary>
    public bool IsBranchNameValid => 
        !string.IsNullOrWhiteSpace(BranchName) && 
        BranchName.Length >= MinBranchNameLength &&
        BranchName.Length <= MaxBranchNameLength &&
        ValidBranchNamePattern.IsMatch(BranchName) &&
        !InvalidCharsPattern.IsMatch(BranchName);

    /// <summary>
    /// Gets whether the description is valid.
    /// </summary>
    public bool IsDescriptionValid => 
        string.IsNullOrEmpty(Description) || 
        (Description.Length <= MaxDescriptionLength && !InvalidCharsPattern.IsMatch(Description));

    /// <summary>
    /// Gets whether there are any validation errors.
    /// </summary>
    public bool HasValidationErrors => !IsBranchNameValid || !IsDescriptionValid;

    /// <summary>
    /// Gets whether the confirm button should be enabled.
    /// </summary>
    public bool CanConfirm => IsBranchNameValid && !HasValidationErrors;

    public void SetCloseAction(Action<BranchCreationResult?> closeAction)
    {
        _closeAction = closeAction;
    }

    partial void OnBranchNameChanged(string value)
    {
        // Auto-truncate if exceeds max length
        if (value?.Length > MaxBranchNameLength)
        {
            BranchName = value[..MaxBranchNameLength];
            return;
        }

        UpdateValidationError();
    }

    partial void OnDescriptionChanged(string value)
    {
        // Auto-truncate if exceeds max length
        if (value?.Length > MaxDescriptionLength)
        {
            Description = value[..MaxDescriptionLength];
            return;
        }

        UpdateValidationError();
    }

    private void UpdateValidationError()
    {
        if (!IsBranchNameValid)
        {
            if (string.IsNullOrWhiteSpace(BranchName))
                ValidationError = "Branch name is required.";
            else if (BranchName.Length < MinBranchNameLength)
                ValidationError = $"Branch name must be at least {MinBranchNameLength} characters.";
            else if (BranchName.Length > MaxBranchNameLength)
                ValidationError = $"Branch name must not exceed {MaxBranchNameLength} characters.";
            else
                ValidationError = "Branch name can only contain letters, numbers, spaces, hyphens, and underscores.";
        }
        else if (!IsDescriptionValid)
        {
            ValidationError = $"Description must not exceed {MaxDescriptionLength} characters.";
        }
        else
        {
            ValidationError = string.Empty;
        }
    }

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private void Confirm()
    {
        if (!CanConfirm)
        {
            UpdateValidationError();
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
