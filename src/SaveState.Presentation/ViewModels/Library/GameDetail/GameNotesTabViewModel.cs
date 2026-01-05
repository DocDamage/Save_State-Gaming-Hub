using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.GameLibrary.Commands;
using SaveState.Application.GameLibrary.Queries;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.UserManagement.Services;
using SaveState.Presentation.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.Presentation.ViewModels.Library.GameDetail;

/// <summary>
/// View model for the Game Notes tab.
/// </summary>
public partial class GameNotesTabViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly IUserContextService _userContextService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<GameNotesTabViewModel> _logger;
    internal GameId? _currentGameId;

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
        ILogger<GameNotesTabViewModel> logger)
    {
        _mediator = mediator;
        _userContextService = userContextService;
        _dialogService = dialogService;
        _logger = logger;
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

            // Populate notes collection
            Notes.Clear();
            foreach (var note in notes.OrderByDescending(n => n.IsPinned).ThenByDescending(n => n.UpdatedAt))
            {
                var wordCount = note.Content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

                Notes.Add(new GameNoteViewModel(_mediator, _userContextService, _dialogService, _logger, this)
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
            var categoryGroups = notes.GroupBy(n => n.Category ?? "General");
            foreach (var group in categoryGroups.OrderByDescending(g => g.Count()))
            {
                Categories.Add(new GameNoteCategoryViewModel(
                    group.Key,
                    GetCategoryColor(group.Key),
                    group.Count()
                ));
            }

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
                _currentGameId.Value,
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
        // TODO: Toggle search functionality
        _logger.LogInformation("Toggle search requested");
    }

    [RelayCommand]
    private void CreateTemplate()
    {
        // TODO: Create note template
        _logger.LogInformation("Create template requested");
    }

    [RelayCommand]
    private void ExportNotes()
    {
        // TODO: Export all notes
        _logger.LogInformation("Export notes requested");
    }

    [RelayCommand]
    private void ImportNotes()
    {
        // TODO: Import notes
        _logger.LogInformation("Import notes requested");
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

        // Note: NoteEditorDialog doesn't support loading existing note data yet
        // This would need to be enhanced to pass initial data
        var result = await _dialogService.ShowNoteEditorAsync(Id);
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
    private void Copy()
    {
        // TODO: Copy note content to clipboard
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
    public GameNoteCategoryViewModel(string name, string color, int count)
    {
        Name = name;
        Color = color;
        Count = count;
    }

    public string Name { get; }
    public string Color { get; }
    public int Count { get; }

    [RelayCommand]
    private void Select()
    {
        // TODO: Filter by this category
    }
}
