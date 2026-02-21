using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the note editor dialog.
/// </summary>
public partial class NoteEditorDialogViewModel : ObservableObject
{
    private readonly Guid? _noteId;

    // Validation constants
    private const int MaxTitleLength = 100;
    private const int MaxContentLength = 5000;
    private static readonly Regex InvalidCharsPattern = new Regex(@"[<>\x00-\x08\x0B\x0C\x0E-\x1F]", RegexOptions.Compiled);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTitleValid))]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(ValidationError))]
    private string _title = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsContentValid))]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(ValidationError))]
    private string _content = string.Empty;

    [ObservableProperty]
    private string _category = "General";

    [ObservableProperty]
    private bool _isPinned;

    [ObservableProperty]
    private string _validationError = string.Empty;

    public ObservableCollection<string> AvailableCategories { get; } = new()
    {
        "General",
        "Strategy",
        "Tips & Tricks",
        "Walkthrough",
        "Secrets",
        "Achievements",
        "Bugs & Issues",
        "Mods",
        "Personal"
    };

    public int CharacterCount => Content?.Length ?? 0;

    /// <summary>
    /// Gets whether the title is valid.
    /// </summary>
    public bool IsTitleValid => 
        !string.IsNullOrWhiteSpace(Title) && 
        Title.Length <= MaxTitleLength &&
        !InvalidCharsPattern.IsMatch(Title);

    /// <summary>
    /// Gets whether the content is valid.
    /// </summary>
    public bool IsContentValid => 
        !string.IsNullOrWhiteSpace(Content) && 
        Content.Length <= MaxContentLength &&
        !InvalidCharsPattern.IsMatch(Content);

    /// <summary>
    /// Gets whether there are any validation errors.
    /// </summary>
    public bool HasValidationErrors => !IsTitleValid || !IsContentValid;

    /// <summary>
    /// Gets whether the save button should be enabled.
    /// </summary>
    public bool CanSave => IsTitleValid && IsContentValid;

    public NoteEditorDialogViewModel(
        Guid? noteId = null,
        string? initialContent = null,
        string? title = null,
        string? category = null,
        bool isPinned = false)
    {
        _noteId = noteId;
        if (!string.IsNullOrEmpty(initialContent))
        {
            Content = initialContent;
        }
        if (!string.IsNullOrEmpty(title))
        {
            Title = title;
        }
        if (!string.IsNullOrEmpty(category))
        {
            Category = category;
        }
        IsPinned = isPinned;
    }

    partial void OnContentChanged(string value)
    {
        // Auto-truncate if exceeds max length
        if (value?.Length > MaxContentLength)
        {
            Content = value[..MaxContentLength];
            return;
        }

        UpdateValidationError();
        OnPropertyChanged(nameof(CharacterCount));
        OnPropertyChanged(nameof(CanSave));
    }

    partial void OnTitleChanged(string value)
    {
        // Auto-truncate if exceeds max length
        if (value?.Length > MaxTitleLength)
        {
            Title = value[..MaxTitleLength];
            return;
        }

        UpdateValidationError();
        OnPropertyChanged(nameof(CanSave));
    }

    private void UpdateValidationError()
    {
        if (!IsTitleValid)
        {
            if (string.IsNullOrWhiteSpace(Title))
                ValidationError = "Title is required.";
            else if (Title.Length > MaxTitleLength)
                ValidationError = $"Title must not exceed {MaxTitleLength} characters.";
            else
                ValidationError = "Title contains invalid characters.";
        }
        else if (!IsContentValid)
        {
            if (string.IsNullOrWhiteSpace(Content))
                ValidationError = "Content is required.";
            else if (Content.Length > MaxContentLength)
                ValidationError = $"Content must not exceed {MaxContentLength} characters.";
            else
                ValidationError = "Content contains invalid characters.";
        }
        else
        {
            ValidationError = string.Empty;
        }
    }

    [RelayCommand]
    private void Save()
    {
        if (!CanSave) return;

        var result = new NoteEditorResult(
            Title: Title.Trim(),
            Content: Content.Trim(),
            Category: Category,
            IsPinned: IsPinned);

        CloseDialog(result);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseDialog(null);
    }

    private void CloseDialog(NoteEditorResult? result)
    {
        var lifetime = Avalonia.Application.Current?.ApplicationLifetime;
        if (lifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close(result);
        }
    }
}
