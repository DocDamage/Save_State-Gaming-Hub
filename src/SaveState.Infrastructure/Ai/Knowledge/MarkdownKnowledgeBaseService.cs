using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Ai.Knowledge;
using SaveState.Core.Configuration;

namespace SaveState.Infrastructure.Ai.Knowledge;

public class MarkdownKnowledgeBaseService : IKnowledgeBaseService
{
    private readonly SemanticKnowledgeClient _knowledgeClient;
    private readonly IKnowledgeStore _store;
    private readonly ILogger<MarkdownKnowledgeBaseService> _logger;
    private readonly string _knowledgeBasePath;

    public MarkdownKnowledgeBaseService(
        SemanticKnowledgeClient knowledgeClient,
        IKnowledgeStore store,
        ILogger<MarkdownKnowledgeBaseService> logger)
    {
        _knowledgeClient = knowledgeClient;
        _store = store;
        _logger = logger;

        // Point to AppData/SaveStateReborn/KnowledgeBase as requested
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _knowledgeBasePath = Path.Combine(appDataPath, "SaveStateReborn", "KnowledgeBase");

        if (!Directory.Exists(_knowledgeBasePath))
        {
            Directory.CreateDirectory(_knowledgeBasePath);
        }
    }

    public async Task<int> SyncKnowledgeBaseAsync(CancellationToken ct = default)
    {
        if (!Directory.Exists(_knowledgeBasePath))
        {
            _logger.LogWarning("Knowledge base directory not found: {Path}", _knowledgeBasePath);
            return 0;
        }

        var mdFiles = Directory.GetFiles(_knowledgeBasePath, "*.md", SearchOption.AllDirectories);
        int totalChunks = 0;

        foreach (var file in mdFiles)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                var content = await File.ReadAllTextAsync(file, ct);
                var fileName = Path.GetFileName(file);

                // Chunk the content by headers or paragraphs
                var chunks = ChunkMarkdown(content, 1000);

                for (int i = 0; i < chunks.Count; i++)
                {
                    var chunkId = $"{fileName}_chunk_{i}";
                    var chunkContent = $"FILE: {fileName}\n\n{chunks[i]}";

                    await _knowledgeClient.IndexDocumentAsync(chunkId, chunkContent, ct).ConfigureAwait(false);
                    totalChunks++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to index file: {File}", file);
            }
        }

        _logger.LogInformation("Knowledge base sync complete. Indexed {Count} chunks from {FileCount} files.", totalChunks, mdFiles.Length);
        return totalChunks;
    }

    public async Task ClearKnowledgeBaseAsync(CancellationToken ct = default)
    {
        await _store.ClearAllAsync(ct);
    }

    public async Task SaveToKnowledgeBaseAsync(string subFolder, string fileName, string content, CancellationToken ct = default)
    {
        var targetDir = Path.Combine(_knowledgeBasePath, subFolder);
        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        var filePath = Path.Combine(targetDir, fileName);
        await File.WriteAllTextAsync(filePath, content, ct);

        // Index the new file immediately
        try
        {
            var chunks = ChunkMarkdown(content, 1000);
            for (int i = 0; i < chunks.Count; i++)
            {
                var chunkId = $"{fileName}_chunk_{i}";
                var chunkContent = $"FILE: {fileName}\n\n{chunks[i]}";
                await _knowledgeClient.IndexDocumentAsync(chunkId, chunkContent, ct).ConfigureAwait(false);
            }
            _logger.LogInformation("Saved and indexed new knowledge file: {File}", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to index new file: {File}", fileName);
        }
    }

    private List<string> ChunkMarkdown(string content, int maxChars)
    {
        var chunks = new List<string>();

        // Simple splitting by headers first
        var parts = Regex.Split(content, @"(?=^# )", RegexOptions.Multiline);

        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part)) continue;

            if (part.Length <= maxChars)
            {
                chunks.Add(part.Trim());
            }
            else
            {
                // If header section is too long, split by double newline
                var subParts = part.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                var currentChunk = "";

                foreach (var sp in subParts)
                {
                    if ((currentChunk + sp).Length > maxChars && currentChunk.Length > 0)
                    {
                        chunks.Add(currentChunk.Trim());
                        currentChunk = "";
                    }
                    currentChunk += sp + "\n\n";
                }

                if (!string.IsNullOrWhiteSpace(currentChunk))
                    chunks.Add(currentChunk.Trim());
            }
        }

        return chunks;
    }
}
