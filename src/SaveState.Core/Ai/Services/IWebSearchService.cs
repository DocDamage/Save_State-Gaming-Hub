using System.Threading;
using System.Threading.Tasks;
using SaveState.Core.Common;

namespace SaveState.Core.Ai.Services;

/// <summary>
/// Service for performing web searches and retrieving content from the internet.
/// </summary>
public interface IWebSearchService
{
    /// <summary>
    /// Performs a web search and returns a summary of the results.
    /// </summary>
    Task<string> SearchAsync(string query, CancellationToken ct = default);

    /// <summary>
    /// Fetches the content of a specific URL and returns it as a formatted string (typically Markdown).
    /// </summary>
    Task<Result<string>> FetchUrlContentAsync(string url, CancellationToken ct = default);
}
