using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the tag editor dialog.
/// </summary>
public partial class TagEditorDialogViewModel : ObservableObject
{
    // Validation constants
    private const int MaxTagLength = 50;
    private const int MaxTags = 20;
    private static readonly Regex ValidTagPattern = new Regex(@"^[\w\s\-\']+$", RegexOptions.Compiled);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAddTag))]
    [NotifyPropertyChangedFor(nameof(ValidationMessage))]
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
    public bool CanAddTag => 
        !string.IsNullOrWhiteSpace(NewTagText) && 
        NewTagText.Trim().Length <= MaxTagLength &&
        ValidTagPattern.IsMatch(NewTagText.Trim()) &&
        !Tags.Any(t => t.Equals(NewTagText.Trim(), StringComparison.OrdinalIgnoreCase)) &&
        Tags.Count < MaxTags;

    /// <summary>
    /// Gets the validation message for adding tags.
    /// </summary>
    public string? ValidationMessage
    {
        get
        {
            if (Tags.Count >= MaxTags)
                return $"Maximum of {MaxTags} tags allowed.";
            if (!string.IsNullOrWhiteSpace(NewTagText))
            {
                var trimmed = NewTagText.Trim();
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

        var tag = SanitizeTag(NewTagText.Trim());
        Tags.Add(tag);
        NewTagText = string.Empty;

        UpdateSuggestedTags();
        OnPropertyChanged(nameof(TagCountText));
        OnPropertyChanged(nameof(HasTags));
        OnPropertyChanged(nameof(CanAddTag));
        OnPropertyChanged(nameof(ValidationMessage));
    }

    private static string SanitizeTag(string tag)
    {
        // Remove consecutive spaces, trim, and normalize
        var sanitized = tag.Trim();
        while (sanitized.Contains("  "))
        {
            sanitized = sanitized.Replace("  ", " ");
        }
        return sanitized;
    }

    [RelayCommand]
    private void RemoveTag(string? tag)
    {
        if (string.IsNullOrEmpty(tag)) return;

        Tags.Remove(tag);
        UpdateSuggestedTags();
        OnPropertyChanged(nameof(TagCountText));
        OnPropertyChanged(nameof(HasTags));
        OnPropertyChanged(nameof(CanAddTag));
        OnPropertyChanged(nameof(ValidationMessage));
    }

    [RelayCommand]
    private void AddSuggestedTag(string? tag)
    {
        if (string.IsNullOrEmpty(tag)) return;
        if (Tags.Count >= MaxTags) return;
        if (Tags.Any(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase))) return;

        Tags.Add(tag);
        UpdateSuggestedTags();
        OnPropertyChanged(nameof(TagCountText));
        OnPropertyChanged(nameof(HasTags));
        OnPropertyChanged(nameof(CanAddTag));
        OnPropertyChanged(nameof(ValidationMessage));
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
        CloseDialog(result);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseDialog(null);
    }

    private void CloseDialog(TagEditorResult? result)
    {
        var lifetime = Avalonia.Application.Current?.ApplicationLifetime;
        if (lifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close(result);
        }
    }
}
