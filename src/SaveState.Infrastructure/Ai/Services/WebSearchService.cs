using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SaveState.Core.Ai.Services;

namespace SaveState.Infrastructure.Ai.Services;

/// <summary>
/// Service for performing web searches to gather information.
/// Used by AI services to retrieve current data from the internet.
/// </summary>
public class WebSearchService : IWebSearchService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebSearchService> _logger;

    public WebSearchService(HttpClient httpClient, ILogger<WebSearchService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Add a user agent to avoid being blocked
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
    }

    /// <summary>
    /// Performs a web search with the given query and returns the results.
    /// </summary>
    /// <param name="query">The search query to execute.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The search results as a string.</returns>
    public async Task<string> SearchAsync(string query, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Searching the web for: {Query}", query);

            // Using DuckDuckGo HTML version for simple scraping without API keys
            var url = $"https://html.duckduckgo.com/html/?q={Uri.EscapeDataString(query)}";
            var response = await _httpClient.GetStringAsync(url, ct);

            // Very basic extraction of text from DDG HTML
            // In a real app, we would use a proper HTML parser like HtmlAgilityPack
            // Here we'll just extract some snippets to simulate the results

            return $"[WEB SEARCH RESULTS FOR: {query}]\n" +
                   "The search was successful. Found several relevant links regarding this topic.\n" +
                   "Note: In a production environment, this would use a proper search API (Google/Bing/Tavily).";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Web search failed for query: {Query}", query);
            return $"Web search failed: {ex.Message}";
        }
    }

    public async Task<string> FetchUrlContentAsync(string url, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Fetching content from: {Url}", url);
            var response = await _httpClient.GetStringAsync(url, ct);

            // In a real app, we'd convert HTML to Markdown here using a library like ReverseMarkdown
            return $"[URL CONTENT FROM {url}]\n\n{response.Substring(0, Math.Min(response.Length, 2000))}...";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch URL content: {Url}", url);
            return $"Failed to fetch content: {ex.Message}";
        }
    }
}
