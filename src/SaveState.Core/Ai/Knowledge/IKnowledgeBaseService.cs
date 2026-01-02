namespace SaveState.Core.Ai.Knowledge;

/// <summary>
/// Service for managing the ingestion and synchronization of the knowledge base.
/// </summary>
public interface IKnowledgeBaseService
{
    /// <summary>
    /// Syncs documents from the configured knowledge directory into the vector store.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success and the number of documents indexed.</returns>
    Task<int> SyncKnowledgeBaseAsync(CancellationToken ct = default);

    /// <summary>
    /// Clears the knowledge base.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task ClearKnowledgeBaseAsync(CancellationToken ct = default);

    /// <summary>
    /// Saves new information to the knowledge base as a markdown file.
    /// </summary>
    /// <param name="subFolder">The subfolder within KnowledgeBase (e.g., 'internet-search').</param>
    /// <param name="fileName">The name of the file (should end with .md).</param>
    /// <param name="content">The markdown content to save.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SaveToKnowledgeBaseAsync(string subFolder, string fileName, string content, CancellationToken ct = default);
}
