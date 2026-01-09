using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the save state settings dialog.
/// </summary>
public partial class SaveStateSettingsDialogViewModel : ObservableObject
{
    private readonly Guid _saveStateId;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _branchName = "main";

    [ObservableProperty]
    private bool _isCurrent;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _tags = new();

    [ObservableProperty]
    private string _newTag = string.Empty;

    public ObservableCollection<string> AvailableBranches { get; } = new()
    {
        "main",
        "boss",
        "secret",
        "speedrun",
        "100%",
        "custom"
    };

    public int CharacterCount => Notes?.Length ?? 0;

    public bool CanSave => !string.IsNullOrWhiteSpace(Description);

    public SaveStateSettingsDialogViewModel(
        Guid saveStateId,
        string description = "",
        string branchName = "main",
        bool isCurrent = false,
        string notes = "")
    {
        _saveStateId = saveStateId;
        Description = description;
        BranchName = branchName;
        IsCurrent = isCurrent;
        Notes = notes;
    }

    partial void OnDescriptionChanged(string value)
    {
        OnPropertyChanged(nameof(CanSave));
    }

    partial void OnNotesChanged(string value)
    {
        OnPropertyChanged(nameof(CharacterCount));
    }

    [RelayCommand]
    private void AddTag()
    {
        if (!string.IsNullOrWhiteSpace(NewTag) && !Tags.Contains(NewTag))
        {
            Tags.Add(NewTag);
            NewTag = string.Empty;
        }
    }

    [RelayCommand]
    private void RemoveTag(string tag)
    {
        Tags.Remove(tag);
    }

    [RelayCommand]
    private void Save()
    {
        if (!CanSave) return;

        var result = new SaveStateSettingsResult(
            SaveStateId: _saveStateId,
            Description: Description,
            BranchName: BranchName,
            IsCurrent: IsCurrent,
            Notes: Notes,
            Tags: Tags.ToArray());

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

/// <summary>
/// Result from the save state settings dialog.
/// </summary>
public record SaveStateSettingsResult(
    Guid SaveStateId,
    string Description,
    string BranchName,
    bool IsCurrent,
    string Notes,
    string[] Tags);
