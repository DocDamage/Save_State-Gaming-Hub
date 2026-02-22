using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Services;
using SaveState.Core.Common.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the goal creation dialog.
/// </summary>
public partial class GoalCreationDialogViewModel : ObservableObject
{
    private readonly ITimeProvider _timeProvider;

    // Validation constants
    private const int MaxTitleLength = 100;
    private const int MaxDescriptionLength = 1000;
    private static readonly Regex InvalidCharsPattern = new Regex(@"[<>\x00-\x08\x0B\x0C\x0E-\x1F]", RegexOptions.Compiled);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTitleValid))]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(ValidationError))]
    private string _title = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDescriptionValid))]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(ValidationError))]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _goalType = "Achievement";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTargetDateValid))]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(ValidationError))]
    private DateTime? _targetDate;

    [ObservableProperty]
    private bool _trackProgress = true;

    [ObservableProperty]
    private bool _notifyOnCompletion = true;

    [ObservableProperty]
    private string _validationError = string.Empty;

    public ObservableCollection<string> GoalTypes { get; } = new()
    {
        "Achievement",
        "Completion",
        "Playtime",
        "Skill Improvement",
        "Collection",
        "Speedrun",
        "Challenge",
        "Custom"
    };

    /// <summary>
    /// Gets whether the title is valid.
    /// </summary>
    public bool IsTitleValid => 
        !string.IsNullOrWhiteSpace(Title) && 
        Title.Length <= MaxTitleLength &&
        !InvalidCharsPattern.IsMatch(Title);

    /// <summary>
    /// Gets whether the description is valid.
    /// </summary>
    public bool IsDescriptionValid => 
        Description.Length <= MaxDescriptionLength &&
        !InvalidCharsPattern.IsMatch(Description);

    /// <summary>
    /// Gets whether the target date is valid (not in the past).
    /// </summary>
    public bool IsTargetDateValid => 
        !TargetDate.HasValue || TargetDate.Value.Date >= _timeProvider.Today;

    /// <summary>
    /// Gets whether there are any validation errors.
    /// </summary>
    public bool HasValidationErrors => 
        !IsTitleValid || !IsDescriptionValid || !IsTargetDateValid;

    /// <summary>
    /// Gets whether the save button should be enabled.
    /// </summary>
    public bool CanSave => 
        !string.IsNullOrWhiteSpace(Title) && 
        !string.IsNullOrWhiteSpace(GoalType) &&
        !HasValidationErrors;

    public GoalCreationDialogViewModel(ITimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    partial void OnTitleChanged(string value)
    {
        // Auto-truncate if exceeds max length
        if (value?.Length > MaxTitleLength)
        {
            Title = value[..MaxTitleLength];
            return;
        }

        // Update validation error message
        if (!IsTitleValid)
        {
            if (string.IsNullOrWhiteSpace(value))
                ValidationError = "Title is required.";
            else if (value?.Length > MaxTitleLength)
                ValidationError = $"Title must not exceed {MaxTitleLength} characters.";
            else
                ValidationError = "Title contains invalid characters.";
        }
        else
        {
            ValidationError = string.Empty;
        }

        OnPropertyChanged(nameof(CanSave));
    }

    partial void OnDescriptionChanged(string value)
    {
        // Auto-truncate if exceeds max length
        if (value?.Length > MaxDescriptionLength)
        {
            Description = value[..MaxDescriptionLength];
            return;
        }

        OnPropertyChanged(nameof(CanSave));
    }

    partial void OnGoalTypeChanged(string value)
    {
        OnPropertyChanged(nameof(CanSave));
    }

    partial void OnTargetDateChanged(DateTime? value)
    {
        if (!IsTargetDateValid)
        {
            ValidationError = "Target date cannot be in the past.";
        }
        else
        {
            ValidationError = string.Empty;
        }
        OnPropertyChanged(nameof(CanSave));
    }

    [RelayCommand]
    private void Save()
    {
        if (!CanSave) return;

        var result = new GoalCreationResult(
            Title: Title.Trim(),
            Description: Description.Trim(),
            TargetDate: TargetDate,
            GoalType: GoalType);

        CloseDialog(result);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseDialog(null);
    }

    private void CloseDialog(GoalCreationResult? result)
    {
        var lifetime = Avalonia.Application.Current?.ApplicationLifetime;
        if (lifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close(result);
        }
    }
}
