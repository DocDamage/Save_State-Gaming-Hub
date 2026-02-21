using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Models.Ai;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Settings;

/// <summary>
/// ViewModel for AI administration interface.
/// Manages LLM providers, conversation memory, knowledge base, and feedback settings.
/// </summary>
public partial class AiAdministrationViewModel : ObservableObject
{
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
    /// Initializes a new instance of the <see cref="AiAdministrationViewModel"/> class.
    /// </summary>
    public AiAdministrationViewModel()
    {
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
            LastUpdated = DateTime.Now.AddDays(-1),
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
        // TODO: Open configuration dialog
        await Task.CompletedTask;
    }

    /// <summary>
    /// Clears the conversation memory.
    /// </summary>
    [RelayCommand]
    private async Task ClearMemoryAsync()
    {
        // TODO: Clear conversation memory through service
        MemoryStats.StoredConversations = 0;
        MemoryStats.LastCleared = DateTime.Now;
        await Task.CompletedTask;
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

        KnowledgeBaseStats.LastUpdated = DateTime.Now;
        IsRebuildingIndex = false;
    }

    /// <summary>
    /// Opens file picker to import documents into the knowledge base.
    /// </summary>
    [RelayCommand]
    private async Task ImportDocumentsAsync()
    {
        // TODO: Open file picker for document import
        await Task.CompletedTask;
    }

    /// <summary>
    /// Exports the knowledge base to a file.
    /// </summary>
    [RelayCommand]
    private async Task ExportKnowledgeAsync()
    {
        // TODO: Export knowledge base through service
        await Task.CompletedTask;
    }
}
