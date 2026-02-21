using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the save state settings dialog.
/// </summary>
public partial class SaveStateSettingsDialogViewModel : ObservableObject
{
    private readonly Guid _saveStateId;

    // Validation constants
    private const int MaxDescriptionLength = 200;
    private const int MaxNotesLength = 1000;
    private const int MaxTagLength = 50;
    private const int MaxTags = 10;
    private static readonly Regex ValidTagPattern = new Regex(@"^[\w\s\-\']+$", RegexOptions.Compiled);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDescriptionValid))]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _branchName = "main";

    [ObservableProperty]
    private bool _isCurrent;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotesValid))]
    private string _notes = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _tags = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAddTag))]
    [NotifyPropertyChangedFor(nameof(TagValidationMessage))]
    private string _newTag = string.Empty;

    [ObservableProperty]
    private string _validationError = string.Empty;

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

    /// <summary>
    /// Gets whether the description is valid.
    /// </summary>
    public bool IsDescriptionValid => 
        !string.IsNullOrWhiteSpace(Description) && 
        Description.Length <= MaxDescriptionLength;

    /// <summary>
    /// Gets whether the notes are valid.
    /// </summary>
    public bool IsNotesValid => Notes.Length <= MaxNotesLength;

    /// <summary>
    /// Gets whether the save button should be enabled.
    /// </summary>
    public bool CanSave => IsDescriptionValid && IsNotesValid;

    /// <summary>
    /// Gets whether a new tag can be added.
    /// </summary>
    public bool CanAddTag => 
        !string.IsNullOrWhiteSpace(NewTag) &&
        NewTag.Trim().Length <= MaxTagLength &&
        ValidTagPattern.IsMatch(NewTag.Trim()) &&
        !Tags.Any(t => t.Equals(NewTag.Trim(), StringComparison.OrdinalIgnoreCase)) &&
        Tags.Count < MaxTags;

    /// <summary>
    /// Gets the tag validation message.
    /// </summary>
    public string? TagValidationMessage
    {
        get
        {
            if (Tags.Count >= MaxTags)
                return $"Maximum of {MaxTags} tags allowed.";
            if (!string.IsNullOrWhiteSpace(NewTag))
            {
                var trimmed = NewTag.Trim();
                if (trimmed.Length > MaxTagLength)
                    return $"Tag must not exceed {MaxTagLength} characters.";
                if (!ValidTagPattern.IsMatch(trimmed))
                    return "Tag can only contain letters, numbers, spaces, hyphens, and apostrophes.";
                if (Tags.Any(t => t.Equals(trimmed, StringComparison.OrdinalIgnoreCase)))
                    return "This tag already exists.";
            }
            return null;
        }
    }

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
        // Auto-truncate if exceeds max length
        if (value?.Length > MaxDescriptionLength)
        {
            Description = value[..MaxDescriptionLength];
            return;
        }

        UpdateValidationError();
        OnPropertyChanged(nameof(CanSave));
    }

    partial void OnNotesChanged(string value)
    {
        // Auto-truncate if exceeds max length
        if (value?.Length > MaxNotesLength)
        {
            Notes = value[..MaxNotesLength];
            return;
        }

        UpdateValidationError();
        OnPropertyChanged(nameof(CharacterCount));
    }

    private void UpdateValidationError()
    {
        if (!IsDescriptionValid)
        {
            if (string.IsNullOrWhiteSpace(Description))
                ValidationError = "Description is required.";
            else
                ValidationError = $"Description must not exceed {MaxDescriptionLength} characters.";
        }
        else if (!IsNotesValid)
        {
            ValidationError = $"Notes must not exceed {MaxNotesLength} characters.";
        }
        else
        {
            ValidationError = string.Empty;
        }
    }

    partial void OnNewTagChanged(string value)
    {
        // Auto-truncate if exceeds max length
        if (value?.Length > MaxTagLength)
        {
            NewTag = value[..MaxTagLength];
        }
    }

    [RelayCommand]
    private void AddTag()
    {
        if (!CanAddTag) return;

        var tag = NewTag.Trim();
        // Normalize consecutive spaces
        while (tag.Contains("  "))
        {
            tag = tag.Replace("  ", " ");
        }

        Tags.Add(tag);
        NewTag = string.Empty;
        OnPropertyChanged(nameof(CanAddTag));
        OnPropertyChanged(nameof(TagValidationMessage));
    }

    [RelayCommand]
    private void RemoveTag(string? tag)
    {
        if (string.IsNullOrEmpty(tag)) return;

        Tags.Remove(tag);
        OnPropertyChanged(nameof(CanAddTag));
        OnPropertyChanged(nameof(TagValidationMessage));
    }

    [RelayCommand]
    private void Save()
    {
        if (!CanSave) return;

        var result = new SaveStateSettingsResult(
            SaveStateId: _saveStateId,
            Description: Description.Trim(),
            BranchName: BranchName,
            IsCurrent: IsCurrent,
            Notes: Notes.Trim(),
            Tags: Tags.ToArray());

        CloseDialog(result);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseDialog(null);
    }

    private void CloseDialog(SaveStateSettingsResult? result)
    {
        var lifetime = Avalonia.Application.Current?.ApplicationLifetime;
        if (lifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close(result);
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
