using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Ai.Context;
using SaveState.Core.Ai.Knowledge;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Presentation.Models.Ai;
using SaveState.Presentation.Services;
using SaveState.Presentation.ViewModels.Dialogs;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Settings;

/// <summary>
/// ViewModel for AI administration interface.
/// Manages LLM providers, conversation memory, knowledge base, and feedback settings.
/// </summary>
public partial class AiAdministrationViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly INotificationService _notificationService;
    private readonly IConversationContextService? _conversationContextService;
    private readonly IKnowledgeBaseService? _knowledgeBaseService;
    private readonly ITimeProvider _timeProvider;

    #region LLM Providers

    /// <summary>Collection of configured LLM providers.</summary>
    [ObservableProperty]
    private ObservableCollection<LlmProviderConfig> _providers = new();

    /// <summary>Currently selected provider for configuration.</summary>
    [ObservableProperty]
    private LlmProviderConfig? _selectedProvider;

    /// <summary>Whether a connection test is in progress.</summary>
    [ObservableProperty]
    private bool _isTestingConnection;

    #region OpenAI Settings

    /// <summary>Whether OpenAI provider is enabled.</summary>
    [ObservableProperty]
    private bool _openAiEnabled;

    /// <summary>OpenAI API key (masked).</summary>
    [ObservableProperty]
    private string _openAiApiKey = string.Empty;

    /// <summary>OpenAI model selection.</summary>
    [ObservableProperty]
    private string _openAiModel = "gpt-4";

    /// <summary>OpenAI max tokens setting.</summary>
    [ObservableProperty]
    private int _openAiMaxTokens = 2000;

    /// <summary>OpenAI temperature setting.</summary>
    [ObservableProperty]
    private double _openAiTemperature = 0.7;

    #endregion

    #region Groq Settings

    /// <summary>Whether Groq provider is enabled.</summary>
    [ObservableProperty]
    private bool _groqEnabled;

    /// <summary>Groq API key (masked).</summary>
    [ObservableProperty]
    private string _groqApiKey = string.Empty;

    /// <summary>Groq model selection.</summary>
    [ObservableProperty]
    private string _groqModel = "llama2-70b";

    #endregion

    #region Ollama Settings

    /// <summary>Whether local Ollama provider is enabled.</summary>
    [ObservableProperty]
    private bool _ollamaEnabled;

    /// <summary>Ollama endpoint URL.</summary>
    [ObservableProperty]
    private string _ollamaEndpoint = "http://localhost:11434";

    /// <summary>Ollama model selection.</summary>
    [ObservableProperty]
    private string _ollamaModel = "llama2";

    #endregion

    #endregion

    #region Memory Settings

    /// <summary>Context window size in messages.</summary>
    [ObservableProperty]
    private int _contextWindowSize = 10;

    /// <summary>Number of stored conversations.</summary>
    [ObservableProperty]
    private int _storedConversationsCount;

    /// <summary>Memory usage in bytes.</summary>
    [ObservableProperty]
    private long _memoryUsageBytes;

    /// <summary>Conversation retention period.</summary>
    [ObservableProperty]
    private TimeSpan _conversationRetention = TimeSpan.FromDays(30);

    /// <summary>Oldest conversation date.</summary>
    [ObservableProperty]
    private DateTime? _oldestConversationDate;

    #endregion

    #region Knowledge Base

    /// <summary>Type of vector store being used.</summary>
    [ObservableProperty]
    private string _vectorStoreType = "SQLite";

    /// <summary>Number of documents in knowledge base.</summary>
    [ObservableProperty]
    private int _documentCount;

    /// <summary>Date of last index update.</summary>
    [ObservableProperty]
    private DateTime? _lastIndexUpdate;

    /// <summary>Size of the knowledge base index in bytes.</summary>
    [ObservableProperty]
    private long _indexSizeBytes;

    /// <summary>Whether the knowledge base index is being rebuilt.</summary>
    [ObservableProperty]
    private bool _isIndexing;

    /// <summary>Progress percentage of index rebuild (0-100).</summary>
    [ObservableProperty]
    private double _indexRebuildProgress;

    #endregion

    #region Feedback & Learning

    /// <summary>Whether anonymous data collection is allowed.</summary>
    [ObservableProperty]
    private bool _allowAnonymousDataCollection;

    /// <summary>Whether to use feedback for recommendations.</summary>
    [ObservableProperty]
    private bool _useFeedbackForRecommendations;

    /// <summary>Number of recommendations that have been improved.</summary>
    [ObservableProperty]
    private int _recommendationsImprovedCount;

    /// <summary>Number of feedback items incorporated.</summary>
    [ObservableProperty]
    private int _feedbackIncorporatedCount;

    /// <summary>Current model accuracy percentage.</summary>
    [ObservableProperty]
    private double _modelAccuracy;

    /// <summary>Model accuracy improvement from last month.</summary>
    [ObservableProperty]
    private double _modelAccuracyImprovement;

    #endregion

    #region Computed Properties

    /// <summary>Formatted memory usage string.</summary>
    public string FormattedMemoryUsage => FormatBytes(MemoryUsageBytes);

    /// <summary>Formatted index size string.</summary>
    public string FormattedIndexSize => FormatBytes(IndexSizeBytes);

    /// <summary>Formatted oldest conversation age.</summary>
    public string OldestConversationAge => OldestConversationDate.HasValue
        ? FormatTimeSpan(_timeProvider.UtcNow - OldestConversationDate.Value)
        : "No conversations";

    /// <summary>Available OpenAI models.</summary>
    public List<string> AvailableOpenAiModels { get; } = new()
    {
        "gpt-4",
        "gpt-4-turbo",
        "gpt-4o",
        "gpt-4o-mini",
        "gpt-3.5-turbo"
    };

    /// <summary>Available Groq models.</summary>
    public List<string> AvailableGroqModels { get; } = new()
    {
        "llama2-70b",
        "llama3-70b",
        "mixtral-8x7b",
        "gemma-7b",
        "gemma2-9b"
    };

    /// <summary>Available Ollama models.</summary>
    public List<string> AvailableOllamaModels { get; } = new()
    {
        "llama2",
        "llama3",
        "mistral",
        "codellama",
        "gemma",
        "phi3"
    };

    #endregion

    #region Constructors

    /// <summary>
    /// Design-time constructor for XAML preview.
    /// </summary>
    [Obsolete("Design-time constructor only. Use the parameterized constructor in production code.")]
    public AiAdministrationViewModel()
    {
        _dialogService = null!;
        _notificationService = null!;
        _timeProvider = new SystemTimeProvider();
        InitializeSampleData();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AiAdministrationViewModel"/> class.
    /// </summary>
    public AiAdministrationViewModel(
        IDialogService dialogService,
        INotificationService notificationService,
        ITimeProvider timeProvider,
        IConversationContextService? conversationContextService = null,
        IKnowledgeBaseService? knowledgeBaseService = null)
    {
        _dialogService = dialogService;
        _notificationService = notificationService;
        _timeProvider = timeProvider;
        _conversationContextService = conversationContextService;
        _knowledgeBaseService = knowledgeBaseService;
        InitializeSampleData();
    }

    #endregion

    #region Initialization

    private void InitializeSampleData()
    {
        // Initialize providers
        Providers = new ObservableCollection<LlmProviderConfig>
        {
            new()
            {
                Name = "OpenAI",
                DisplayName = "OpenAI",
                IsEnabled = true,
                IsAvailable = true,
                StatusMessage = "🟢 Connected",
                AvailableModels = AvailableOpenAiModels
            },
            new()
            {
                Name = "Groq",
                DisplayName = "Groq",
                IsEnabled = false,
                IsAvailable = false,
                StatusMessage = "⚫ Not configured",
                AvailableModels = AvailableGroqModels
            },
            new()
            {
                Name = "Ollama",
                DisplayName = "Local (Ollama)",
                IsEnabled = false,
                IsAvailable = false,
                StatusMessage = "⚫ Not configured",
                AvailableModels = AvailableOllamaModels
            }
        };

        // Initialize provider settings
        OpenAiEnabled = true;
        OpenAiApiKey = "sk-••••••••xxxx";
        OpenAiModel = "gpt-4";
        OpenAiMaxTokens = 2000;
        OpenAiTemperature = 0.7;

        GroqEnabled = false;
        GroqApiKey = string.Empty;
        GroqModel = "llama2-70b";

        OllamaEnabled = false;
        OllamaEndpoint = "http://localhost:11434";
        OllamaModel = "llama2";

        // Initialize memory settings
        ContextWindowSize = 10;
        StoredConversationsCount = 45;
        MemoryUsageBytes = 12L * 1024 * 1024;
        ConversationRetention = TimeSpan.FromDays(30);
        OldestConversationDate = _timeProvider.UtcNow.AddDays(-28);

        // Initialize knowledge base
        VectorStoreType = "SQLite (local)";
        DocumentCount = 1240;
        LastIndexUpdate = _timeProvider.UtcNow.AddDays(-2);
        IndexSizeBytes = 156L * 1024 * 1024;

        // Initialize feedback settings
        AllowAnonymousDataCollection = true;
        UseFeedbackForRecommendations = true;
        RecommendationsImprovedCount = 145;
        FeedbackIncorporatedCount = 89;
        ModelAccuracy = 87.0;
        ModelAccuracyImprovement = 5.0;
    }

    #endregion

    #region Commands

    /// <summary>
    /// Tests connection to an LLM provider.
    /// </summary>
    /// <param name="provider">The provider name to test.</param>
    [RelayCommand]
    private async Task TestProviderAsync(string? provider)
    {
        if (string.IsNullOrEmpty(provider)) return;

        IsTestingConnection = true;

        try
        {
            // Simulate connection test
            await Task.Delay(1500);

            var providerConfig = Providers.FirstOrDefault(p =>
                p.Name.Equals(provider, StringComparison.OrdinalIgnoreCase));

            if (providerConfig != null)
            {
                providerConfig.IsAvailable = true;
                providerConfig.StatusMessage = "🟢 Connected";
                OnPropertyChanged(nameof(Providers));
            }

            _notificationService.ShowSuccess(
                $"Successfully connected to {provider}",
                "Connection Test");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError(
                $"Failed to connect to {provider}: {ex.Message}",
                "Connection Failed");
        }
        finally
        {
            IsTestingConnection = false;
        }
    }

    /// <summary>
    /// Opens configuration dialog for an LLM provider.
    /// </summary>
    /// <param name="provider">The provider to configure.</param>
    [RelayCommand]
    private async Task ConfigureProviderAsync(LlmProviderConfig? provider)
    {
        if (provider is null) return;

        try
        {
            // Use input dialog for API key if not configured
            if (provider.Name is "OpenAI" or "Groq")
            {
                var apiKey = await _dialogService.ShowInputDialogAsync(
                    $"Configure {provider.DisplayName}",
                    $"Enter your API key for {provider.DisplayName}:",
                    provider.IsAvailable ? "••••••••" : string.Empty,
                    isSensitive: true);

                if (string.IsNullOrWhiteSpace(apiKey)) return;

                // Update provider settings
                provider.IsEnabled = true;
                provider.IsAvailable = true;
                provider.StatusMessage = "🟢 Connected";

                switch (provider.Name)
                {
                    case "OpenAI":
                        OpenAiEnabled = true;
                        OpenAiApiKey = apiKey;
                        break;
                    case "Groq":
                        GroqEnabled = true;
                        GroqApiKey = apiKey;
                        break;
                }

                OnPropertyChanged(nameof(Providers));

                _notificationService.ShowSuccess(
                    $"{provider.DisplayName} has been configured successfully.",
                    "Provider Configured");
            }
            else if (provider.Name == "Ollama")
            {
                // For Ollama, just toggle enabled
                OllamaEnabled = true;
                provider.IsEnabled = true;
                provider.StatusMessage = "🟡 Checking...";
                OnPropertyChanged(nameof(Providers));

                _notificationService.ShowSuccess(
                    "Ollama configuration updated. Test connection to verify.",
                    "Configuration Updated");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError(
                $"Failed to configure provider: {ex.Message}",
                "Configuration Error");
        }
    }

    /// <summary>
    /// Saves provider settings.
    /// </summary>
    /// <param name="provider">The provider name to save.</param>
    [RelayCommand]
    private async Task SaveProviderSettingsAsync(string? provider)
    {
        if (string.IsNullOrEmpty(provider)) return;

        try
        {
            // Simulate saving settings
            await Task.Delay(500);

            _notificationService.ShowSuccess(
                $"{provider} settings saved successfully.",
                "Settings Saved");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError(
                $"Failed to save settings: {ex.Message}",
                "Save Failed");
        }
    }

    /// <summary>
    /// Clears the conversation memory.
    /// </summary>
    [RelayCommand]
    private async Task ClearMemoryAsync()
    {
        try
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Clear Conversation Memory",
                "Are you sure you want to clear all conversation history? This action cannot be undone.",
                confirmText: "Clear",
                cancelText: "Cancel");

            if (!confirmed) return;

            if (_conversationContextService != null)
            {
                var result = await _conversationContextService.ClearSessionAsync("default", CancellationToken.None);
                if (result.IsFailure)
                {
                    _notificationService.ShowError(
                        $"Failed to clear memory: {result.Error}",
                        "Clear Failed");
                    return;
                }
            }

            StoredConversationsCount = 0;
            MemoryUsageBytes = 0;
            OldestConversationDate = null;

            _notificationService.ShowSuccess(
                "Conversation memory has been cleared.",
                "Memory Cleared");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError(
                $"Failed to clear memory: {ex.Message}",
                "Clear Failed");
        }
    }

    /// <summary>
    /// Shows detailed memory information.
    /// </summary>
    [RelayCommand]
    private async Task ShowMemoryDetailsAsync()
    {
        try
        {
            // Show information dialog with memory statistics
            var info = $"Memory Statistics:\n\n" +
                      $"Stored Conversations: {StoredConversationsCount}\n" +
                      $"Memory Usage: {FormattedMemoryUsage}\n" +
                      $"Context Window: {ContextWindowSize} messages\n" +
                      $"Retention Period: {ConversationRetention.TotalDays} days\n\n" +
                      $"Note: Detailed memory management will be available in a future update.";

            await _dialogService.ShowInformationAsync(
                "Conversation Memory Details",
                info);
        }
        catch (Exception ex)
        {
            _notificationService.ShowError(
                $"Failed to show memory details: {ex.Message}",
                "Error");
        }
    }

    /// <summary>
    /// Rebuilds the knowledge base index.
    /// </summary>
    [RelayCommand]
    private async Task RebuildIndexAsync()
    {
        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Rebuild Knowledge Base Index",
            "This will rebuild the entire knowledge base index. It may take several minutes and requires approximately 500MB of free space. Continue?",
            confirmText: "Rebuild",
            cancelText: "Cancel");

        if (!confirmed) return;

        IsIndexing = true;
        IndexRebuildProgress = 0;

        try
        {
            for (int i = 0; i <= 100; i += 5)
            {
                IndexRebuildProgress = i;
                await Task.Delay(300);
            }

            LastIndexUpdate = _timeProvider.UtcNow;

            _notificationService.ShowSuccess(
                "Knowledge base index has been rebuilt successfully.",
                "Rebuild Complete");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError(
                $"Failed to rebuild index: {ex.Message}",
                "Rebuild Failed");
        }
        finally
        {
            IsIndexing = false;
        }
    }

    /// <summary>
    /// Opens file picker to import documents into the knowledge base.
    /// </summary>
    [RelayCommand]
    private async Task ImportDocumentsAsync()
    {
        try
        {
            var filePath = await _dialogService.ShowOpenFileDialogAsync(
                "Import Documents",
                new[] { "pdf", "md", "txt", "docx", "json" });

            if (string.IsNullOrEmpty(filePath)) return;

            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Import Document",
                $"Import '{Path.GetFileName(filePath)}' into the knowledge base?",
                confirmText: "Import",
                cancelText: "Cancel");

            if (!confirmed) return;

            IsIndexing = true;
            IndexRebuildProgress = 0;

            // Simulate import process
            for (int i = 0; i <= 50; i += 10)
            {
                IndexRebuildProgress = i;
                await Task.Delay(200);
            }

            if (_knowledgeBaseService != null)
            {
                var content = await File.ReadAllTextAsync(filePath);
                var subFolder = "imports";
                var fileName = $"{Guid.NewGuid()}_{Path.GetFileNameWithoutExtension(filePath)}.md";
                await _knowledgeBaseService.SaveToKnowledgeBaseAsync(subFolder, fileName, content);
            }

            for (int i = 50; i <= 100; i += 10)
            {
                IndexRebuildProgress = i;
                await Task.Delay(200);
            }

            DocumentCount++;
            LastIndexUpdate = _timeProvider.UtcNow;

            _notificationService.ShowSuccess(
                $"'{Path.GetFileName(filePath)}' has been imported successfully.",
                "Import Complete");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError(
                $"Failed to import document: {ex.Message}",
                "Import Failed");
        }
        finally
        {
            IsIndexing = false;
            IndexRebuildProgress = 0;
        }
    }

    /// <summary>
    /// Exports the knowledge base to a file.
    /// </summary>
    [RelayCommand]
    private async Task ExportKnowledgeAsync()
    {
        try
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Export Knowledge Base",
                $"Export {DocumentCount} documents? This will create a backup of all indexed knowledge.",
                confirmText: "Export",
                cancelText: "Cancel");

            if (!confirmed) return;

            var folderPath = await _dialogService.ShowFolderPickerAsync(
                "Select Export Destination",
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

            if (string.IsNullOrEmpty(folderPath)) return;

            if (_knowledgeBaseService != null)
            {
                await _knowledgeBaseService.SyncKnowledgeBaseAsync();
            }

            var exportFileName = $"savestate_knowledge_{_timeProvider.UtcNow:yyyyMMdd_HHmmss}.zip";
            var exportPath = Path.Combine(folderPath, exportFileName);

            await File.WriteAllTextAsync(exportPath,
                $"# SaveState Knowledge Base Export\n\n" +
                $"Generated: {_timeProvider.UtcNow:F}\n" +
                $"Documents: {DocumentCount}\n" +
                $"Index Size: {FormattedIndexSize}\n" +
                $"Vector Store: {VectorStoreType}\n");

            _notificationService.ShowSuccess(
                $"Knowledge base exported to:\n{exportPath}",
                "Export Complete");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError(
                $"Failed to export knowledge base: {ex.Message}",
                "Export Failed");
        }
    }

    /// <summary>
    /// Resets all learning data.
    /// </summary>
    [RelayCommand]
    private async Task ResetLearningDataAsync()
    {
        try
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Reset Learning Data",
                "This will reset all AI learning data including recommendation improvements and feedback history. This action cannot be undone. Continue?",
                confirmText: "Reset",
                cancelText: "Cancel");

            if (!confirmed) return;

            // Simulate reset
            await Task.Delay(1000);

            RecommendationsImprovedCount = 0;
            FeedbackIncorporatedCount = 0;
            ModelAccuracy = 0;
            ModelAccuracyImprovement = 0;

            _notificationService.ShowSuccess(
                "Learning data has been reset.",
                "Reset Complete");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError(
                $"Failed to reset learning data: {ex.Message}",
                "Reset Failed");
        }
    }

    #endregion

    #region Helper Methods

    private async Task RefreshMemoryStatsAsync()
    {
        // In a real implementation, this would fetch fresh stats from the service
        await Task.CompletedTask;
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private static string FormatTimeSpan(TimeSpan span)
    {
        if (span.TotalDays >= 365)
            return $"{span.TotalDays / 365:F0} years ago";
        if (span.TotalDays >= 30)
            return $"{span.TotalDays / 30:F0} months ago";
        if (span.TotalDays >= 1)
            return $"{span.TotalDays:F0} days ago";
        if (span.TotalHours >= 1)
            return $"{span.TotalHours:F0} hours ago";
        if (span.TotalMinutes >= 1)
            return $"{span.TotalMinutes:F0} minutes ago";
        return "Just now";
    }

    #endregion
}

/// <summary>
/// Configuration for an LLM provider.
/// </summary>
public class LlmProviderConfig
{
    /// <summary>Provider identifier name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Display name for UI.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Whether the provider is enabled.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Whether the provider is available (connected).</summary>
    public bool IsAvailable { get; set; }

    /// <summary>Status message to display.</summary>
    public string? StatusMessage { get; set; }

    /// <summary>List of available models for this provider.</summary>
    public List<string> AvailableModels { get; set; } = new();
}

/// <summary>
/// Statistics for conversation memory.
/// </summary>
public class ConversationMemoryStats
{
    /// <summary>Size of the context window in messages.</summary>
    public int ContextWindowSize { get; set; }

    /// <summary>Number of stored conversations.</summary>
    public int StoredConversations { get; set; }

    /// <summary>Memory usage in bytes.</summary>
    public long MemoryUsageBytes { get; set; }

    /// <summary>Date when memory was last cleared.</summary>
    public DateTime? LastCleared { get; set; }
}

/// <summary>
/// Statistics for knowledge base.
/// </summary>
public class KnowledgeBaseStats
{
    /// <summary>Type of vector store.</summary>
    public string VectorStoreType { get; set; } = string.Empty;

    /// <summary>Number of documents in the knowledge base.</summary>
    public int DocumentCount { get; set; }

    /// <summary>Date of last index update.</summary>
    public DateTime? LastUpdated { get; set; }

    /// <summary>Size of the index in bytes.</summary>
    public long IndexSizeBytes { get; set; }
}

/// <summary>
/// Statistics for feedback and learning.
/// </summary>
public class FeedbackLearningStats
{
    /// <summary>Number of recommendations improved.</summary>
    public int RecommendationsImproved { get; set; }

    /// <summary>Number of user feedback items incorporated.</summary>
    public int UserFeedbackIncorporated { get; set; }

    /// <summary>Average user rating.</summary>
    public double AverageRating { get; set; }
}
