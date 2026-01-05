using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the goal creation dialog.
/// </summary>
public partial class GoalCreationDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _goalType = "Achievement";

    [ObservableProperty]
    private DateTime? _targetDate;

    [ObservableProperty]
    private bool _trackProgress = true;

    [ObservableProperty]
    private bool _notifyOnCompletion = true;

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

    public bool CanSave => !string.IsNullOrWhiteSpace(Title) && !string.IsNullOrWhiteSpace(GoalType);

    public GoalCreationDialogViewModel()
    {
    }

    partial void OnTitleChanged(string value)
    {
        OnPropertyChanged(nameof(CanSave));
    }

    partial void OnGoalTypeChanged(string value)
    {
        OnPropertyChanged(nameof(CanSave));
    }

    [RelayCommand]
    private void Save()
    {
        if (!CanSave) return;

        var result = new GoalCreationResult(
            Title: Title,
            Description: Description,
            TargetDate: TargetDate,
            GoalType: GoalType);

        // Close dialog with result
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close(result);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        // Close dialog without result
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close(null);
        }
    }
}
