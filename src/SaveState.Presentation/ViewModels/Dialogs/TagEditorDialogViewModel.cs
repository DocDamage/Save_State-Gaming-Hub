using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the tag editor dialog.
/// </summary>
public partial class TagEditorDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _newTagText = string.Empty;

    public ObservableCollection<string> Tags { get; } = new();

    public ObservableCollection<string> SuggestedTags { get; } = new()
    {
        "RPG",
        "Action",
        "Adventure",
        "Strategy",
        "Indie",
        "Multiplayer",
        "Singleplayer",
        "Co-op",
        "Competitive",
        "Story-Rich",
        "Open World",
        "Sandbox",
        "Survival",
        "Horror",
        "Puzzle",
        "Platformer",
        "Shooter",
        "Fighting",
        "Racing",
        "Simulation"
    };

    public string TagCountText => Tags.Count == 0 ? "No tags" : Tags.Count == 1 ? "1 tag" : $"{Tags.Count} tags";
    public bool HasTags => Tags.Count > 0;
    public bool CanAddTag => !string.IsNullOrWhiteSpace(NewTagText) && !Tags.Contains(NewTagText.Trim());

    public TagEditorDialogViewModel(string[] currentTags)
    {
        if (currentTags != null)
        {
            foreach (var tag in currentTags)
            {
                Tags.Add(tag);
            }
        }

        // Remove suggested tags that are already added
        UpdateSuggestedTags();
    }

    partial void OnNewTagTextChanged(string value)
    {
        OnPropertyChanged(nameof(CanAddTag));
    }

    [RelayCommand]
    private void AddTag()
    {
        if (!CanAddTag) return;

        var tag = NewTagText.Trim();
        Tags.Add(tag);
        NewTagText = string.Empty;

        UpdateSuggestedTags();
        OnPropertyChanged(nameof(TagCountText));
        OnPropertyChanged(nameof(HasTags));
    }

    [RelayCommand]
    private void RemoveTag(string tag)
    {
        Tags.Remove(tag);
        UpdateSuggestedTags();
        OnPropertyChanged(nameof(TagCountText));
        OnPropertyChanged(nameof(HasTags));
    }

    [RelayCommand]
    private void AddSuggestedTag(string tag)
    {
        if (Tags.Contains(tag)) return;

        Tags.Add(tag);
        UpdateSuggestedTags();
        OnPropertyChanged(nameof(TagCountText));
        OnPropertyChanged(nameof(HasTags));
    }

    private void UpdateSuggestedTags()
    {
        // Remove tags that are already added from suggested list
        var toRemove = SuggestedTags.Where(t => Tags.Contains(t)).ToList();
        foreach (var tag in toRemove)
        {
            SuggestedTags.Remove(tag);
        }
    }

    [RelayCommand]
    private void Save()
    {
        var result = new TagEditorResult(Tags.ToArray());

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
