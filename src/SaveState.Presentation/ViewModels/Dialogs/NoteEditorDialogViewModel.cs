using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the note editor dialog.
/// </summary>
public partial class NoteEditorDialogViewModel : ObservableObject
{
    private readonly Guid? _noteId;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private string _category = "General";

    [ObservableProperty]
    private bool _isPinned;

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

    public bool CanSave => !string.IsNullOrWhiteSpace(Title) && !string.IsNullOrWhiteSpace(Content);

    public NoteEditorDialogViewModel(Guid? noteId = null, string? initialContent = null)
    {
        _noteId = noteId;
        if (!string.IsNullOrEmpty(initialContent))
        {
            Content = initialContent;
        }
    }

    partial void OnContentChanged(string value)
    {
        OnPropertyChanged(nameof(CharacterCount));
        OnPropertyChanged(nameof(CanSave));
    }

    partial void OnTitleChanged(string value)
    {
        OnPropertyChanged(nameof(CanSave));
    }

    [RelayCommand]
    private void Save()
    {
        if (!CanSave) return;

        var result = new NoteEditorResult(
            Title: Title,
            Content: Content,
            Category: Category,
            IsPinned: IsPinned);

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
