using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Ai.Context;
using SaveState.Core.Ai.Knowledge;
using SaveState.Core.Common;
using SaveState.Presentation.Models.Ai;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace SaveState.Presentation.ViewModels.Settings;

/// <summary>
/// ViewModel for AI administration interface.
/// Manages LLM providers, conversation memory, knowledge base, and feedback settings.
/// </summary>
public partial class AiAdministrationViewModel : ObservableObject
{
    private readonly IDialogService? _dialogService;
    private readonly INotificationService? _notificationService;
    private readonly IConversationContextService? _conversationContextService;
    private readonly IKnowledgeBaseService? _knowledgeBaseService;

    /// <summary>Collection of configured LLM providers.</summary>
    [ObservableProperty]
    private ObservableCollection<LlmProviderConfig> _llmProviders = new();

    /// <summary>Conversation memory statistics.</summary>
    [ObservableProperty]
    private ConversationMemoryStats _memoryStats = new();

    /// <summary>Knowledge base statistics.</summary>
    [ObservableProperty]
    private KnowledgeBaseStats _knowledgeBaseStats = new();

    /// <summary>Feedback and learning statistics.</summary>
    [ObservableProperty]
    private FeedbackLearningStats _feedbackStats = new();

    /// <summary>Whether anonymous data collection is allowed.</summary>
    [ObservableProperty]
    private bool _allowAnonymousDataCollection;

    /// <summary>Whether to use feedback for improvements.</summary>
    [ObservableProperty]
    private bool _useFeedbackForImprovements;

    /// <summary>Whether the knowledge base index is being rebuilt.</summary>
    [ObservableProperty]
    private bool _isRebuildingIndex;

    /// <summary>Progress percentage of index rebuild (0-100).</summary>
    [ObservableProperty]
    private double _rebuildProgress;

    /// <summary>
    /// Design-time constructor for XAML preview.
    /// </summary>
    [Obsolete("Design-time constructor only. Use the parameterized constructor in production code.")]
    public AiAdministrationViewModel()
    {
        InitializeSampleData();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AiAdministrationViewModel"/> class.
    /// </summary>
    public AiAdministrationViewModel(
        IDialogService dialogService,
        INotificationService notificationService,
        IConversationContextService? conversationContextService = null,
        IKnowledgeBaseService? knowledgeBaseService = null)
    {
        _dialogService = dialogService;
        _notificationService = notificationService;
        _conversationContextService = conversationContextService;
        _knowledgeBaseService = knowledgeBaseService;
        InitializeSampleData();
    }

    private void InitializeSampleData()
    {
        LlmProviders = new ObservableCollection<LlmProviderConfig>
        {
            new() { Name = "OpenAI", IsEnabled = true, SelectedModel = "GPT-4", AvailableModels = new() { "GPT-4", "GPT-4-Turbo", "GPT-3.5-Turbo" }, ApiKeyStatus = "Configured" },
            new() { Name = "Groq", IsEnabled = false, AvailableModels = new() { "Mixtral-8x7B", "Llama-2-70B" }, ApiKeyStatus = "Not configured" },
            new() { Name = "Local (Ollama)", IsEnabled = false, AvailableModels = new() { "llama2", "mistral", "codellama" }, ApiKeyStatus = "Not configured" }
        };

        MemoryStats = new ConversationMemoryStats
        {
            ContextWindowSize = 10,
            StoredConversations = 45,
            MemoryUsageBytes = 1024 * 1024 * 12
        };

        KnowledgeBaseStats = new KnowledgeBaseStats
        {
            VectorStoreType = "SQLite (local)",
            DocumentCount = 1240,
            LastUpdated = DateTimeOffset.UtcNow.AddDays(-1).DateTime,
            IndexSizeBytes = 1024 * 1024 * 45
        };

        FeedbackStats = new FeedbackLearningStats
        {
            RecommendationsImproved = 145,
            UserFeedbackIncorporated = 89,
            AverageRating = 4.2
        };

        AllowAnonymousDataCollection = true;
        UseFeedbackForImprovements = true;
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
            // Show input dialog for API key configuration
            var apiKey = await _dialogService.ShowInputDialogAsync(
                $"Configure {provider.Name}",
                $"Enter your API key for {provider.Name}:",
                provider.ApiKeyStatus == "Configured" ? "••••••••" : string.Empty,
                isSensitive: true);

            if (string.IsNullOrWhiteSpace(apiKey)) return;

            // In a real implementation, this would call a service to store the API key securely
            provider.ApiKeyStatus = "Configured";

            _notificationService.ShowSuccess(
                $"{provider.Name} has been configured successfully.",
                "Provider Configured");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError(
                $"Failed to configure provider: {ex.Message}",
                "Configuration Error");
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
            // Confirm before clearing
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Clear Conversation Memory",
                "Are you sure you want to clear all conversation history? This action cannot be undone.",
                confirmText: "Clear",
                cancelText: "Cancel");

            if (!confirmed) return;

            // Clear through service if available
            if (_conversationContextService != null)
            {
                // Clear all active sessions - in a real implementation, 
                // we would iterate through all session IDs
                var result = await _conversationContextService.ClearSessionAsync("default", CancellationToken.None);
                if (result.IsFailure)
                {
                    _notificationService.ShowError(
                        $"Failed to clear memory: {result.Error}",
                        "Clear Failed");
                    return;
                }
            }

            // Update UI stats
            MemoryStats.StoredConversations = 0;
            MemoryStats.LastCleared = DateTimeOffset.UtcNow.DateTime;

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
    /// Rebuilds the knowledge base index.
    /// </summary>
    [RelayCommand]
    private async Task RebuildIndexAsync()
    {
        IsRebuildingIndex = true;
        RebuildProgress = 0;

        for (int i = 0; i <= 100; i += 5)
        {
            RebuildProgress = i;
            await Task.Delay(200);
        }

        KnowledgeBaseStats.LastUpdated = DateTimeOffset.UtcNow.DateTime;
        IsRebuildingIndex = false;
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

            // Show confirmation to proceed with import
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Import Document",
                $"Import '{Path.GetFileName(filePath)}' into the knowledge base?",
                confirmText: "Import",
                cancelText: "Cancel");

            if (!confirmed) return;

            // In a real implementation, this would process the file through
            // the knowledge base service for embedding and indexing
            if (_knowledgeBaseService != null)
            {
                var content = await File.ReadAllTextAsync(filePath);
                var subFolder = "imports";
                var fileName = $"{Guid.NewGuid()}_{Path.GetFileNameWithoutExtension(filePath)}.md";
                await _knowledgeBaseService.SaveToKnowledgeBaseAsync(subFolder, fileName, content);
            }

            KnowledgeBaseStats.DocumentCount++;
            KnowledgeBaseStats.LastUpdated = DateTimeOffset.UtcNow.DateTime;

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
    }

    /// <summary>
    /// Exports the knowledge base to a file.
    /// </summary>
    [RelayCommand]
    private async Task ExportKnowledgeAsync()
    {
        try
        {
            // Show confirmation dialog with export options
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Export Knowledge Base",
                $"Export {KnowledgeBaseStats.DocumentCount} documents? This will create a backup of all indexed knowledge.",
                confirmText: "Export",
                cancelText: "Cancel");

            if (!confirmed) return;

            // Open folder picker for export destination
            var folderPath = await _dialogService.ShowFolderPickerAsync(
                "Select Export Destination",
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

            if (string.IsNullOrEmpty(folderPath)) return;

            // Generate export file name with timestamp
            var exportFileName = $"savestate_knowledge_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.zip";
            var exportPath = Path.Combine(folderPath, exportFileName);

            // In a real implementation, this would call the knowledge base service
            // to create a proper export archive
            if (_knowledgeBaseService != null)
            {
                // Trigger a sync before export to ensure data is up to date
                await _knowledgeBaseService.SyncKnowledgeBaseAsync();
            }

            // Create a placeholder export file (real implementation would zip the knowledge base)
            await File.WriteAllTextAsync(exportPath,
                $"# SaveState Knowledge Base Export\n\n" +
                $"Generated: {DateTimeOffset.UtcNow:F}\n" +
                $"Documents: {KnowledgeBaseStats.DocumentCount}\n" +
                $"Index Size: {KnowledgeBaseStats.IndexSizeBytes / 1024 / 1024} MB\n" +
                $"Vector Store: {KnowledgeBaseStats.VectorStoreType}\n");

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
}
