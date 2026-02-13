namespace SaveState.Core.Ai.Services;

using SaveState.Core.GameLibrary.Entities;
using System.Threading;
using System.Threading.Tasks;

public interface INaturalLanguageGameSearch
{
    /// <summary>
    /// Parses a natural language query into a structured collection filter.
    /// </summary>
    /// <param name="query">The natural language query (e.g., "RPGs from the 90s").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A structured filter object.</returns>
    Task<CollectionFilter> ParseQueryAsync(string query, CancellationToken ct = default);
}
