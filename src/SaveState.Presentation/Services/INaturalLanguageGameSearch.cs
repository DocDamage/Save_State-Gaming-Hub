using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Presentation.Services;

/// <summary>
/// Service for natural language game search functionality.
/// </summary>
public interface INaturalLanguageGameSearch
{
    /// <summary>
    /// Searches for games using natural language query.
    /// </summary>
    Task<Result<IReadOnlyList<Game>>> SearchAsync(string query, CancellationToken ct = default);

    /// <summary>
    /// Gets search suggestions based on partial query.
    /// </summary>
    Task<Result<IReadOnlyList<string>>> GetSuggestionsAsync(string partialQuery, CancellationToken ct = default);

    /// <summary>
    /// Parses a natural language query.
    /// </summary>
    Task<Result<object>> ParseQueryAsync(string query, CancellationToken ct = default);
}
