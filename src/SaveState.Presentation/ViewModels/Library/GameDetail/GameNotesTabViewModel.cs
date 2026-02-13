using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.GameLibrary.Commands;
using SaveState.Application.GameLibrary.Queries;
using SaveState.Core.Common.Services;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.UserManagement.Services;
using SaveState.Presentation.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;

namespace SaveState.Presentation.ViewModels.Library.GameDetail;

/// <summary>
/// View model for the Game Notes tab.
/// </summary>
public partial class GameNotesTabViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly IUserContextService _userContextService;
    private readonly IDialogService _dialogService;
    private readonly IClipboardService _clipboardService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<GameNotesTabViewModel> _logger;
    private readonly ITimeProvider _timeProvider;
    internal GameId? _currentGameId;

    private List<GameNoteViewModel> _allNotes = new();
    private string _selectedCategory = "All";

    [ObservableProperty]
    private string _notesCountText = "0 notes";

    [ObservableProperty]
    private int _totalNotes;

    [ObservableProperty]
    private int _totalWords;

    [ObservableProperty]
    private string _averageNoteLength = "0 words";

    [ObservableProperty]
    private string _lastUpdatedText = "Never";

    [ObservableProperty]
    private ObservableCollection<GameNoteViewModel> _notes = new();

    [ObservableProperty]
    private ObservableCollection<string> _sortOptions = new() { "Newest", "Oldest", "Title", "Category" };

    [ObservableProperty]
    private string _selectedSort = "Newest";

    [ObservableProperty]
    private ObservableCollection<GameNoteCategoryViewModel> _categories = new();

    [ObservableProperty]
    private ObservableCollection<string> _recentTags = new();

    [ObservableProperty]
    private bool _autoSaveEnabled = true;

    [ObservableProperty]
    private bool _richTextEnabled;

    [ObservableProperty]
    private bool _showWordCount = true;

    public GameNotesTabViewModel(
        IMediator mediator,
        IUserContextService userContextService,
        IDialogService dialogService,
        IClipboardService clipboardService,
        INotificationService notificationService,
        ILogger<GameNotesTabViewModel> logger,
        ITimeProvider timeProvider)
    {
        _mediator = mediator;
        _userContextService = userContextService;
        _dialogService = dialogService;
        _clipboardService = clipboardService;
        _notificationService = notificationService;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task LoadDataAsync(GameId gameId)
    {
        _currentGameId = gameId; // Store for later use
        try
        {
            var userId = _userContextService.GetCurrentUserId();
            if (!userId.HasValue)
            {
                _logger.LogWarning("No current user context - cannot load notes");
                TotalNotes = 0;
                NotesCountText = "0 notes";
                return;
            }

            var query = new GetGameNotesQuery(gameId.Value, userId.Value);
            var notes = await _mediator.Send(query).ConfigureAwait(false);

            TotalNotes = notes.Count;
            NotesCountText = $"{TotalNotes} note{(TotalNotes == 1 ? "" : "s")}";

            // Calculate total words across all notes
            TotalWords = 0;
            foreach (var note in notes)
            {
                var wordCount = note.Content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
                TotalWords += wordCount;
            }

            AverageNoteLength = TotalNotes > 0 ? $"{TotalWords / TotalNotes} words" : "0 words";

            // Find most recently updated note
            var mostRecent = notes.OrderByDescending(n => n.UpdatedAt).FirstOrDefault();
            LastUpdatedText = mostRecent != null ? FormatDateTime(mostRecent.UpdatedAt) : "Never";

            // Populate all notes
            _allNotes.Clear();
            foreach (var note in notes.OrderByDescending(n => n.IsPinned).ThenByDescending(n => n.UpdatedAt))
            {
                var wordCount = note.Content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

                _allNotes.Add(new GameNoteViewModel(_mediator, _userContextService, _dialogService, _logger, this)
                {
                    Id = note.Id,
                    Title = note.Title,
                    Content = note.Content,
                    IsPinned = note.IsPinned,
                    Category = note.Category,
                    CreatedText = FormatDateTime(note.CreatedAt),
                    LastModifiedText = FormatDateTime(note.UpdatedAt),
                    CategoryText = note.Category ?? "General",
                    CategoryColor = GetCategoryColor(note.Category),
                    WordCountText = $"{wordCount} word{(wordCount == 1 ? "" : "s")}",
                    Tags = new ObservableCollection<string>(note.Tags),
                    BackgroundBrush = note.IsPinned ? "#1A4A90FF" : "Transparent",
                    BorderBrush = note.IsPinned ? "#4A90FF" : "Transparent"
                });
            }

            // Populate categories
            Categories.Clear();
            Categories.Add(new GameNoteCategoryViewModel("All", "#6B7280", TotalNotes, OnSelectCategory)); // Add All option

            var categoryGroups = notes.GroupBy(n => n.Category ?? "General");
            foreach (var group in categoryGroups.OrderByDescending(g => g.Count()))
            {
                Categories.Add(new GameNoteCategoryViewModel(
                    group.Key,
                    GetCategoryColor(group.Key),
                    group.Count(),
                    OnSelectCategory
                ));
            }

            // Re-apply filters
            FilterNotes();

            // Populate recent tags
            RecentTags.Clear();
            var allTags = notes.SelectMany(n => n.Tags).Distinct().Take(10);
            foreach (var tag in allTags)
            {
                RecentTags.Add(tag);
            }

            _logger.LogInformation("Loaded {Count} notes for game {GameId}", notes.Count, gameId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load notes for game {GameId}", gameId);
        }
    }

    private void OnSelectCategory(string category)
    {
        _selectedCategory = category;
        FilterNotes();
        if (category != "All")
        {
            _notificationService.ShowInfo($"Filtered by {category}", "Category Filter");
        }
    }

    private void FilterNotes()
    {
        var filtered = _allNotes.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(n =>
                n.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                n.Content.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        if (_selectedCategory != "All")
        {
            filtered = filtered.Where(n => (n.Category ?? "General") == _selectedCategory);
        }

        Notes.Clear();
        foreach (var note in filtered)
        {
            Notes.Add(note);
        }

        NotesCountText = $"{Notes.Count} note{(Notes.Count == 1 ? "" : "s")}";
    }

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isSearchVisible;

    partial void OnSearchTextChanged(string value)
    {
        FilterNotes();
    }

    internal async Task CopyToClipboard(string content)
    {
        try
        {
             await _clipboardService.SetTextAsync(content);
             _logger.LogInformation("Copied to clipboard: {Content}", content.Substring(0, Math.Min(content.Length, 20)) + "...");
             _notificationService.ShowSuccess("Note content copied to clipboard", "Copied");
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Failed to copy to clipboard");
             _notificationService.ShowError("Clipboard access failed");
        }
    }

    private static string FormatDateTime(DateTime dateTime)
    {
        var now = DateTime.UtcNow;
        var diff = now - dateTime;

        if (diff.TotalMinutes < 1)
            return "Just now";
        if (diff.TotalMinutes < 60)
            return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24)
            return $"{(int)diff.TotalHours}h ago";
        if (diff.TotalDays < 7)
            return $"{(int)diff.TotalDays}d ago";

        return dateTime.ToString("MMM d, yyyy");
    }

    private static string GetCategoryColor(string? category)
    {
        return category switch
        {
            "Walkthrough" => "#4A90FF",
            "Tips" => "#10B981",
            "Bugs" => "#EF4444",
            "Reminders" => "#F59E0B",
            "Strategy" => "#8B5CF6",
            _ => "#6B7280"
        };
    }

    [RelayCommand]
    private async Task AddNote()
    {
        if (_currentGameId == null)
        {
            _logger.LogWarning("Cannot add note - no game ID set");
            return;
        }

        var userId = _userContextService.GetCurrentUserId();
        if (!userId.HasValue)
        {
            _logger.LogWarning("Cannot add note - no current user");
            return;
        }

        var result = await _dialogService.ShowNoteEditorAsync();
        if (result == null)
        {
            _logger.LogInformation("Note creation cancelled");
            return;
        }

        try
        {
            var command = new CreateGameNoteCommand(
                _currentGameId!,
                userId.Value,
                result.Title,
                result.Content,
                result.Category,
                new List<string>(),
                result.IsPinned);

            var createResult = await _mediator.Send(command);
            if (createResult.IsSuccess)
            {
                _logger.LogInformation("Note created successfully");
                await LoadDataAsync(_currentGameId); // Reload notes
            }
            else
            {
                _logger.LogError("Failed to create note: {Error}", createResult.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating note");
        }
    }

    [RelayCommand]
    private void ToggleSearch()
    {
        IsSearchVisible = !IsSearchVisible;
        if (!IsSearchVisible)
        {
            SearchText = string.Empty; // Clear search when hiding
        }
        else
        {
             _notificationService.ShowInfo("Search bar enabled", "Search");
        }
    }

    [RelayCommand]
    private async Task CreateTemplate()
    {
        if (_currentGameId == null) return;

        var options = new[] { "Walkthrough", "Checklist", "Boss Strategy", "Review" };
        var selection = await _dialogService.ShowInputDialogAsync("Select Template",
            $"Available templates:\n{string.Join("\n", options)}\n\nType template name:",
            "Walkthrough");

        if (string.IsNullOrEmpty(selection)) return;

        string content = selection.ToLower() switch
        {
            "checklist" => "## Checklist\n- [ ] Item 1\n- [ ] Item 2",
            "boss strategy" => "## Boss Name\n**Weakness:** \n\n### Phases\n1. \n2. ",
            "review" => "## Review\n**Rating:** /10\n\n**Pros:**\n\n**Cons:**",
            _ => "## New Note\n"
        };

        // Open editor with this content
        var result = await _dialogService.ShowNoteEditorAsync(null, content);
        if (result != null)
        {
            // Create the note
             var userId = _userContextService.GetCurrentUserId();
             if (userId.HasValue)
             {
                 var command = new CreateGameNoteCommand(
                    _currentGameId!,
                    userId.Value,
                    result.Title,
                    result.Content,
                    result.Category,
                    new List<string>(),
                    result.IsPinned);
                 await _mediator.Send(command);
                 await LoadDataAsync(_currentGameId!);
             }
        }
    }

    [RelayCommand]
    private async Task ExportNotes()
    {
        if (!Notes.Any())
        {
            _notificationService.ShowWarning("No notes to export", "Export");
            return;
        }

        var folder = await _dialogService.ShowFolderPickerAsync("Select Export Location");
        if (string.IsNullOrEmpty(folder)) return;

        try
        {
             var timestamp = _timeProvider.Now.ToString("yyyyMMdd_HHmmss");
             var filename = $"Notes_{_currentGameId}_{timestamp}.json";
             var path = Path.Combine(folder, filename);

             var exportData = Notes.Select(n => new
             {
                 n.Title,
                 n.Content,
                 n.Category,
                 n.IsPinned,
                 Tags = n.Tags
             });

             var json = System.Text.Json.JsonSerializer.Serialize(exportData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
             await File.WriteAllTextAsync(path, json);

             _notificationService.ShowSuccess($"Exported {Notes.Count} notes to {filename}", "Export Successful");
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Failed to export notes");
             _notificationService.ShowError("Failed to export notes");
        }
    }

    [RelayCommand]
    private async Task ImportNotes()
    {
        if (_currentGameId == null) return;
        var userId = _userContextService.GetCurrentUserId();
        if (!userId.HasValue) return;

        var file = await _dialogService.ShowFilePickerAsync("Import Notes", new[] { "json" });
        if (string.IsNullOrEmpty(file) || !File.Exists(file)) return;

        try
        {
            var json = await File.ReadAllTextAsync(file);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                 _notificationService.ShowError("Invalid file format. Expected JSON array.", "Import Failed");
                 return;
            }

            int count = 0;
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (element.TryGetProperty("Title", out var titleProp) &&
                    element.TryGetProperty("Content", out var contentProp))
                {
                    var title = titleProp.GetString() ?? "Imported Note";
                    var content = contentProp.GetString() ?? "";
                    string? category = null;
                    if(element.TryGetProperty("Category", out var catProp)) category = catProp.GetString();
                    bool isPinned = false;
                    if(element.TryGetProperty("IsPinned", out var pinProp)) isPinned = pinProp.GetBoolean();

                    var tags = new List<string>();
                    if(element.TryGetProperty("Tags", out var tagsProp) && tagsProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach(var tag in tagsProp.EnumerateArray())
                        {
                            var t = tag.GetString();
                            if(!string.IsNullOrEmpty(t)) tags.Add(t);
                        }
                    }

                    var command = new CreateGameNoteCommand(
                        _currentGameId!,
                        userId.Value,
                        title,
                        content,
                        category,
                        tags,
                        isPinned);

                    await _mediator.Send(command);
                    count++;
                }
            }

            if (count > 0)
            {
                _notificationService.ShowSuccess($"Imported {count} notes.", "Import Successful");
                await LoadDataAsync(_currentGameId!);
            }
            else
            {
                _notificationService.ShowWarning("No valid notes found in file.", "Import");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import notes");
            _notificationService.ShowError("Failed to import notes.");
        }
    }
}

/// <summary>
/// View model for individual notes.
/// </summary>
public partial class GameNoteViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly IUserContextService _userContextService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<GameNotesTabViewModel> _logger;
    private readonly GameNotesTabViewModel _parent;

    public Guid Id { get; set; }
    public bool IsPinned { get; set; }
    public string? Category { get; set; }

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private string _createdText = string.Empty;

    [ObservableProperty]
    private string _lastModifiedText = string.Empty;

    [ObservableProperty]
    private string _categoryText = string.Empty;

    [ObservableProperty]
    private string _categoryColor = "#666666";

    [ObservableProperty]
    private string _wordCountText = "0 words";

    [ObservableProperty]
    private ObservableCollection<string> _tags = new();

    [ObservableProperty]
    private string _backgroundBrush = "Transparent";

    [ObservableProperty]
    private string _borderBrush = "Transparent";

    public GameNoteViewModel(
        IMediator mediator,
        IUserContextService userContextService,
        IDialogService dialogService,
        ILogger<GameNotesTabViewModel> logger,
        GameNotesTabViewModel parent)
    {
        _mediator = mediator;
        _userContextService = userContextService;
        _dialogService = dialogService;
        _logger = logger;
        _parent = parent;
    }

    [RelayCommand]
    private async Task Edit()
    {
        var userId = _userContextService.GetCurrentUserId();
        if (!userId.HasValue)
        {
            _logger.LogWarning("Cannot edit note - no current user");
            return;
        }

        // Pass current data to editor
        var result = await _dialogService.ShowNoteEditorAsync(Id, Content, Title, Category, IsPinned);
        if (result == null)
        {
            _logger.LogInformation("Note edit cancelled");
            return;
        }

        try
        {
            var command = new UpdateGameNoteCommand(
                Id,
                userId.Value,
                result.Title,
                result.Content,
                result.Category,
                Tags.ToList(),
                result.IsPinned);

            var updateResult = await _mediator.Send(command);
            if (updateResult.IsSuccess)
            {
                _logger.LogInformation("Note updated successfully");
                await _parent.LoadDataAsync(_parent._currentGameId!); // Reload notes
            }
            else
            {
                _logger.LogError("Failed to update note: {Error}", updateResult.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating note");
        }
    }

    [RelayCommand]
    private async Task Copy()
    {
        await _parent.CopyToClipboard(Content);
    }

    [RelayCommand]
    private async Task Delete()
    {
        var userId = _userContextService.GetCurrentUserId();
        if (!userId.HasValue)
        {
            _logger.LogWarning("Cannot delete note - no current user");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Delete Note",
            $"Are you sure you want to delete '{Title}'?",
            "Delete",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        try
        {
            var command = new DeleteGameNoteCommand(Id, userId.Value);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Note deleted successfully");
                await _parent.LoadDataAsync(_parent._currentGameId!); // Reload notes
            }
            else
            {
                _logger.LogError("Failed to delete note: {Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting note");
        }
    }
}

/// <summary>
/// View model for note categories.
/// </summary>
public partial class GameNoteCategoryViewModel : ObservableObject
{
    private readonly Action<string> _selectAction;

    public GameNoteCategoryViewModel(string name, string color, int count, Action<string> selectAction)
    {
        Name = name;
        Color = color;
        Count = count;
        _selectAction = selectAction;
    }

    public string Name { get; }
    public string Color { get; }
    public int Count { get; }

    [RelayCommand]
    private void Select()
    {
        _selectAction(Name);
    }
}
