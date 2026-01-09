using SaveState.Core.Common;
using SaveState.Core.Ai.Knowledge;

namespace SaveState.Infrastructure.Ai.Knowledge;

public interface ISemanticKnowledgeClient
{
    Task IndexDocumentAsync(string id, string content, CancellationToken ct);
    Task<string> GetRelevantContextAsync(string query, CancellationToken ct);
}
