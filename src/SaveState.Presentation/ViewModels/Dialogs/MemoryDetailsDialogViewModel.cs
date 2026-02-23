using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Ai.Context;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;
using System.Text;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// ViewModel for memory details dialog showing stored conversations.
/// </summary>
public partial class MemoryDetailsDialogViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly INotificationService _notificationService;
    private readonly ITimeProvider _timeProvider;
    private readonly IConversationContextService? _conversationContextService;

    #region Properties

    /// <summary>Search query for filtering conversations.</summary>
    [ObservableProperty]
    private string _searchQuery = string.Empty;

    /// <summary>Collection of stored conversations.</summary>
    [ObservableProperty]
    private ObservableCollection<ConversationItem> _conversations = new();

    /// <summary>Filtered collection of conversations.</summary>
    [ObservableProperty]
    private ObservableCollection<ConversationItem> _filteredConversations = new();

    /// <summary>Currently selected conversation.</summary>
    [ObservableProperty]
    private ConversationItem? _selectedConversation;

    /// <summary>Preview text of selected conversation.</summary>
    [ObservableProperty]
    private string _conversationPreview = string.Empty;

    /// <summary>Total memory usage in bytes.</summary>
    [ObservableProperty]
    private long _totalMemoryUsageBytes;

    /// <summary>Total number of messages across all conversations.</summary>
    [ObservableProperty]
    private int _totalMessageCount;

    /// <summary>Average conversation length in messages.</summary>
    [ObservableProperty]
    private double _averageConversationLength;

    /// <summary>Whether data is loading.</summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>Sort option for conversations.</summary>
    [ObservableProperty]
    private ConversationSortOption _selectedSortOption = ConversationSortOption.DateNewest;

    #endregion

    #region Computed Properties

    /// <summary>Formatted total memory usage.</summary>
    public string FormattedTotalMemory => FormatBytes(TotalMemoryUsageBytes);

    /// <summary>Number of filtered conversations.</summary>
    public int FilteredCount => FilteredConversations.Count;

    /// <summary>Number of total conversations.</summary>
    public int TotalCount => Conversations.Count;

    /// <summary>Available sort options.</summary>
    public List<ConversationSortOption> SortOptions { get; } = Enum.GetValues<ConversationSortOption>().ToList();

    #endregion

    #region Constructor

    /// <summary>
    /// Design-time constructor for XAML preview.
    /// </summary>
    [Obsolete("Design-time constructor only. Use the parameterized constructor in production code.")]
    public MemoryDetailsDialogViewModel()
    {
        _dialogService = null!;
        _notificationService = null!;
        _timeProvider = new SystemTimeProvider();
        LoadSampleData();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryDetailsDialogViewModel"/> class.
    /// </summary>
    public MemoryDetailsDialogViewModel(
        IDialogService dialogService,
        INotificationService notificationService,
        ITimeProvider timeProvider,
        IConversationContextService? conversationContextService = null)
    {
        _dialogService = dialogService;
        _notificationService = notificationService;
        _timeProvider = timeProvider;
        _conversationContextService = conversationContextService;

        LoadSampleData();
    }

    #endregion

    #region Data Loading

    private void LoadSampleData()
    {
        Conversations.Clear();

        var sampleConversations = new List<ConversationItem>
        {
            new()
            {
                Id = "conv_001",
                Title = "Game Recommendation Request",
                CreatedAt = _timeProvider.UtcNow.AddDays(-1),
                LastMessageAt = _timeProvider.UtcNow.AddHours(-2),
                MessageCount = 8,
                MemoryUsageBytes = 2450,
                Preview = "User: Can you recommend some RPGs?\nAI: Here are some great RPGs...",
                Context = new List<ConversationContextMessage>
                {
                    new() { Role = "user", Content = "Can you recommend some RPGs similar to The Witcher 3?", Timestamp = _timeProvider.UtcNow.AddDays(-1) },
                    new() { Role = "assistant", Content = "Here are some great RPGs similar to The Witcher 3:\n\n1. Dragon Age: Inquisition\n2. Elden Ring\n3. Baldur's Gate 3\n4. Cyberpunk 2077", Timestamp = _timeProvider.UtcNow.AddDays(-1).AddMinutes(1) },
                    new() { Role = "user", Content = "Which one has the best combat?", Timestamp = _timeProvider.UtcNow.AddHours(-2) },
                    new() { Role = "assistant", Content = "Elden Ring and Baldur's Gate 3 have the most engaging combat systems...", Timestamp = _timeProvider.UtcNow.AddHours(-2).AddMinutes(1) }
                }
            },
            new()
            {
                Id = "conv_002",
                Title = "Save State Help",
                CreatedAt = _timeProvider.UtcNow.AddDays(-3),
                LastMessageAt = _timeProvider.UtcNow.AddDays(-2),
                MessageCount = 5,
                MemoryUsageBytes = 1820,
                Preview = "User: How do I create a save state?\nAI: To create a save state...",
                Context = new List<ConversationContextMessage>
                {
                    new() { Role = "user", Content = "How do I create a save state?", Timestamp = _timeProvider.UtcNow.AddDays(-3) },
                    new() { Role = "assistant", Content = "To create a save state, press F5 during gameplay or use the overlay menu...", Timestamp = _timeProvider.UtcNow.AddDays(-3).AddMinutes(1) }
                }
            },
            new()
            {
                Id = "conv_003",
                Title = "MUGEN Character Help",
                CreatedAt = _timeProvider.UtcNow.AddDays(-5),
                LastMessageAt = _timeProvider.UtcNow.AddDays(-5).AddHours(-2),
                MessageCount = 12,
                MemoryUsageBytes = 4200,
                Preview = "User: How do I add a new character?\nAI: To add a character to MUGEN...",
                Context = new List<ConversationContextMessage>()
            },
            new()
            {
                Id = "conv_004",
                Title = "Cloud Sync Configuration",
                CreatedAt = _timeProvider.UtcNow.AddDays(-7),
                LastMessageAt = _timeProvider.UtcNow.AddDays(-6),
                MessageCount = 6,
                MemoryUsageBytes = 1950,
                Preview = "User: How do I setup cloud sync?\nAI: You can configure cloud sync...",
                Context = new List<ConversationContextMessage>()
            },
            new()
            {
                Id = "conv_005",
                Title = "Performance Optimization",
                CreatedAt = _timeProvider.UtcNow.AddDays(-10),
                LastMessageAt = _timeProvider.UtcNow.AddDays(-9),
                MessageCount = 15,
                MemoryUsageBytes = 5600,
                Preview = "User: My games are running slow\nAI: Let's optimize your settings...",
                Context = new List<ConversationContextMessage>()
            },
            new()
            {
                Id = "conv_006",
                Title = "ROM Import Question",
                CreatedAt = _timeProvider.UtcNow.AddDays(-14),
                LastMessageAt = _timeProvider.UtcNow.AddDays(-14),
                MessageCount = 3,
                MemoryUsageBytes = 980,
                Preview = "User: What ROM formats are supported?\nAI: SaveState supports...",
                Context = new List<ConversationContextMessage>()
            },
            new()
            {
                Id = "conv_007",
                Title = "Achievement Tracking",
                CreatedAt = _timeProvider.UtcNow.AddDays(-21),
                LastMessageAt = _timeProvider.UtcNow.AddDays(-20),
                MessageCount = 9,
                MemoryUsageBytes = 3100,
                Preview = "User: How does achievement tracking work?\nAI: Achievement tracking...",
                Context = new List<ConversationContextMessage>()
            },
            new()
            {
                Id = "conv_008",
                Title = "Plugin Development",
                CreatedAt = _timeProvider.UtcNow.AddDays(-28),
                LastMessageAt = _timeProvider.UtcNow.AddDays(-27),
                MessageCount = 20,
                MemoryUsageBytes = 7800,
                Preview = "User: How do I create a plugin?\nAI: To create a SaveState plugin...",
                Context = new List<ConversationContextMessage>()
            }
        };

        foreach (var conv in sampleConversations)
        {
            Conversations.Add(conv);
        }

        ApplyFiltersAndSort();
        CalculateStatistics();
    }

    private void CalculateStatistics()
    {
        TotalMemoryUsageBytes = Conversations.Sum(c => c.MemoryUsageBytes);
        TotalMessageCount = Conversations.Sum(c => c.MessageCount);
        AverageConversationLength = Conversations.Count > 0
            ? Conversations.Average(c => c.MessageCount)
            : 0;
    }

    #endregion

    #region Commands

    /// <summary>
    /// Applies search filter and sorting.
    /// </summary>
    [RelayCommand]
    private void ApplyFiltersAndSort()
    {
        var query = Conversations.AsEnumerable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var search = SearchQuery.ToLowerInvariant();
            query = query.Where(c =>
                c.Title.ToLowerInvariant().Contains(search) ||
                c.Preview.ToLowerInvariant().Contains(search));
        }

        // Apply sorting
        query = SelectedSortOption switch
        {
            ConversationSortOption.DateNewest => query.OrderByDescending(c => c.LastMessageAt),
            ConversationSortOption.DateOldest => query.OrderBy(c => c.LastMessageAt),
            ConversationSortOption.MessageCountHigh => query.OrderByDescending(c => c.MessageCount),
            ConversationSortOption.MessageCountLow => query.OrderBy(c => c.MessageCount),
            ConversationSortOption.SizeLarge => query.OrderByDescending(c => c.MemoryUsageBytes),
            ConversationSortOption.SizeSmall => query.OrderBy(c => c.MemoryUsageBytes),
            _ => query.OrderByDescending(c => c.LastMessageAt)
        };

        FilteredConversations.Clear();
        foreach (var conv in query)
        {
            FilteredConversations.Add(conv);
        }

        OnPropertyChanged(nameof(FilteredCount));
    }

    /// <summary>
    /// Deletes the selected conversation.
    /// </summary>
    [RelayCommand]
    private async Task DeleteConversationAsync(ConversationItem? conversation)
    {
        if (conversation is null) return;

        try
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Delete Conversation",
                $"Are you sure you want to delete the conversation '{conversation.Title}'?\n\nThis action cannot be undone.",
                confirmText: "Delete",
                cancelText: "Cancel");

            if (!confirmed) return;

            // Remove from collections
            Conversations.Remove(conversation);
            FilteredConversations.Remove(conversation);

            // Clear selection if this was selected
            if (SelectedConversation == conversation)
            {
                SelectedConversation = null;
                ConversationPreview = string.Empty;
            }

            // Recalculate statistics
            CalculateStatistics();

            OnPropertyChanged(nameof(FilteredCount));
            OnPropertyChanged(nameof(TotalCount));

            _notificationService.ShowSuccess(
                "Conversation deleted successfully.",
                "Deleted");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError(
                $"Failed to delete conversation: {ex.Message}",
                "Delete Failed");
        }
    }

    /// <summary>
    /// Deletes all conversations.
    /// </summary>
    [RelayCommand]
    private async Task DeleteAllConversationsAsync()
    {
        try
        {
            if (Conversations.Count == 0)
            {
                _notificationService.ShowNotification(
                    "No conversations to delete.",
                    "No Data");
                return;
            }

            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Delete All Conversations",
                $"Are you sure you want to delete all {Conversations.Count} conversations?\n\nThis action cannot be undone.",
                confirmText: "Delete All",
                cancelText: "Cancel");

            if (!confirmed) return;

            Conversations.Clear();
            FilteredConversations.Clear();
            SelectedConversation = null;
            ConversationPreview = string.Empty;

            CalculateStatistics();

            OnPropertyChanged(nameof(FilteredCount));
            OnPropertyChanged(nameof(TotalCount));

            _notificationService.ShowSuccess(
                "All conversations have been deleted.",
                "Deleted");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError(
                $"Failed to delete conversations: {ex.Message}",
                "Delete Failed");
        }
    }

    /// <summary>
    /// Exports selected conversation.
    /// </summary>
    [RelayCommand]
    private async Task ExportConversationAsync(ConversationItem? conversation)
    {
        if (conversation is null) return;

        try
        {
            var folderPath = await _dialogService.ShowFolderPickerAsync(
                "Select Export Location",
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

            if (string.IsNullOrEmpty(folderPath)) return;

            var fileName = $"conversation_{conversation.Id}_{_timeProvider.UtcNow:yyyyMMdd_HHmmss}.txt";
            var filePath = Path.Combine(folderPath, fileName);

            var sb = new StringBuilder();
            sb.AppendLine($"Conversation: {conversation.Title}");
            sb.AppendLine($"Created: {conversation.CreatedAt:F}");
            sb.AppendLine($"Last Message: {conversation.LastMessageAt:F}");
            sb.AppendLine($"Messages: {conversation.MessageCount}");
            sb.AppendLine(new string('-', 50));
            sb.AppendLine();

            foreach (var message in conversation.Context)
            {
                sb.AppendLine($"[{message.Timestamp:HH:mm:ss}] {message.Role.ToUpperInvariant()}:");
                sb.AppendLine(message.Content);
                sb.AppendLine();
            }

            await File.WriteAllTextAsync(filePath, sb.ToString());

            _notificationService.ShowSuccess(
                $"Conversation exported to:\n{filePath}",
                "Export Complete");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError(
                $"Failed to export conversation: {ex.Message}",
                "Export Failed");
        }
    }

    /// <summary>
    /// Refreshes the conversation list.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsLoading = true;

        try
        {
            // In a real implementation, this would fetch from the service
            await Task.Delay(1000);

            LoadSampleData();

            _notificationService.ShowSuccess(
                "Conversation list refreshed.",
                "Refreshed");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError(
                $"Failed to refresh: {ex.Message}",
                "Refresh Failed");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Closes the dialog.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        Result = true;
    }

    #endregion

    #region Partial Methods

    partial void OnSearchQueryChanged(string value)
    {
        ApplyFiltersAndSort();
    }

    partial void OnSelectedSortOptionChanged(ConversationSortOption value)
    {
        ApplyFiltersAndSort();
    }

    partial void OnSelectedConversationChanged(ConversationItem? value)
    {
        if (value != null)
        {
            var sb = new StringBuilder();
            foreach (var message in value.Context)
            {
                var role = message.Role == "user" ? "You" : "AI";
                sb.AppendLine($"[{message.Timestamp:HH:mm}] {role}:");
                sb.AppendLine(message.Content);
                sb.AppendLine();
            }
            ConversationPreview = sb.ToString();
        }
        else
        {
            ConversationPreview = string.Empty;
        }
    }

    #endregion

    #region Helper Methods

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    #endregion

    #region Result

    /// <summary>
    /// Gets the result of the dialog.
    /// </summary>
    public bool? Result { get; private set; }

    #endregion
}

/// <summary>
/// Sort options for conversation list.
/// </summary>
public enum ConversationSortOption
{
    DateNewest,
    DateOldest,
    MessageCountHigh,
    MessageCountLow,
    SizeLarge,
    SizeSmall
}

/// <summary>
/// Represents a stored conversation.
/// </summary>
public class ConversationItem
{
    /// <summary>Conversation ID.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Conversation title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Last message timestamp.</summary>
    public DateTime LastMessageAt { get; set; }

    /// <summary>Number of messages in conversation.</summary>
    public int MessageCount { get; set; }

    /// <summary>Memory usage in bytes.</summary>
    public long MemoryUsageBytes { get; set; }

    /// <summary>Preview text.</summary>
    public string Preview { get; set; } = string.Empty;

    /// <summary>Full conversation context.</summary>
    public List<ConversationContextMessage> Context { get; set; } = new();

    /// <summary>Formatted memory usage.</summary>
    public string FormattedMemoryUsage
    {
        get
        {
            string[] sizes = { "B", "KB", "MB" };
            double len = MemoryUsageBytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.#} {sizes[order]}";
        }
    }
}

/// <summary>
/// A message in a conversation context.
/// </summary>
public class ConversationContextMessage
{
    /// <summary>Message role (user or assistant).</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>Message content.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Message timestamp.</summary>
    public DateTime Timestamp { get; set; }
}
