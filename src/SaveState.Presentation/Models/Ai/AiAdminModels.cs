namespace SaveState.Presentation.Models.Ai;

/// <summary>
/// Configuration for an LLM provider.
/// </summary>
public class LlmProviderConfig
{
    /// <summary>Name of the provider (OpenAI, Groq, Local Ollama, etc.).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Whether this provider is enabled.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Currently selected model.</summary>
    public string? SelectedModel { get; set; }

    /// <summary>List of available models for this provider.</summary>
    public List<string> AvailableModels { get; set; } = new();

    /// <summary>Status of the API key configuration.</summary>
    public string? ApiKeyStatus { get; set; }
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

    /// <summary>Timestamp when memory was last cleared.</summary>
    public DateTime? LastCleared { get; set; }
}

/// <summary>
/// Statistics for the knowledge base.
/// </summary>
public class KnowledgeBaseStats
{
    /// <summary>Type of vector store used.</summary>
    public string VectorStoreType { get; set; } = string.Empty;

    /// <summary>Number of documents in the index.</summary>
    public int DocumentCount { get; set; }

    /// <summary>Timestamp of last index update.</summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>Size of the index in bytes.</summary>
    public long IndexSizeBytes { get; set; }
}

/// <summary>
/// Statistics for feedback-based learning.
/// </summary>
public class FeedbackLearningStats
{
    /// <summary>Number of recommendations that have been improved.</summary>
    public int RecommendationsImproved { get; set; }

    /// <summary>Number of user feedback items incorporated.</summary>
    public int UserFeedbackIncorporated { get; set; }

    /// <summary>Average user rating.</summary>
    public double AverageRating { get; set; }
}
