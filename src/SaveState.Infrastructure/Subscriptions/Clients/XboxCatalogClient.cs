// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Subscriptions;

namespace SaveState.Infrastructure.Subscriptions.Clients;

/// <summary>
/// Client for Xbox Game Pass catalog API.
/// </summary>
public sealed class XboxCatalogClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<XboxCatalogClient> _logger;

    public XboxCatalogClient(HttpClient httpClient, ILogger<XboxCatalogClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the Xbox Game Pass catalog.
    /// </summary>
    public async Task<IReadOnlyList<SubscriptionGame>> GetGamePassCatalogAsync(
        string? accessToken = null,
        CancellationToken ct = default)
    {
        try
        {
            // Microsoft Store Catalog API endpoint
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                "https://displaycatalog.mp.microsoft.com/v7.0/products?" +
                "bigIds=9MVGWVZM1P99,9NBLGGH52SWD&" +
                "market=US&" +
                "languages=en-us");

            if (!string.IsNullOrEmpty(accessToken))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            }

            var response = await _httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch Xbox catalog: {StatusCode}", response.StatusCode);
                return new List<SubscriptionGame>();
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            return ParseMicrosoftStoreResponse(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Xbox Game Pass catalog");
            return new List<SubscriptionGame>();
        }
    }

    /// <summary>
    /// Gets games leaving Game Pass soon.
    /// </summary>
    public async Task<IReadOnlyList<SubscriptionGame>> GetLeavingSoonAsync(
        string? accessToken = null,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching Xbox Game Pass leaving soon games");
        return new List<SubscriptionGame>();
    }

    /// <summary>
    /// Gets new arrivals to Game Pass.
    /// </summary>
    public async Task<IReadOnlyList<SubscriptionGame>> GetNewArrivalsAsync(
        string? accessToken = null,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching Xbox Game Pass new arrivals");
        return new List<SubscriptionGame>();
    }

    /// <summary>
    /// Searches for a game in the Xbox catalog.
    /// </summary>
    public async Task<Result<SubscriptionGame>> SearchGameAsync(
        string title,
        string? accessToken = null,
        CancellationToken ct = default)
    {
        try
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://displaycatalog.mp.microsoft.com/v7.0/products?" +
                $"query={Uri.EscapeDataString(title)}&" +
                $"market=US&" +
                $"languages=en-us");

            var response = await _httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to search Xbox catalog: {StatusCode}", response.StatusCode);
                return Result.Failure<SubscriptionGame>("Failed to search Xbox catalog", ErrorType.External);
            }

            // TODO: Implement actual search response parsing
            // For now, return not found as the implementation is incomplete
            return Result.Failure<SubscriptionGame>($"Game '{title}' not found in Xbox catalog", ErrorType.NotFound);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for game: {Title}", title);
            return Result.Failure<SubscriptionGame>($"Error searching for game: {ex.Message}", ErrorType.External);
        }
    }

    private List<SubscriptionGame> ParseMicrosoftStoreResponse(string json)
    {
        var games = new List<SubscriptionGame>();

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("Products", out var products))
            {
                foreach (var product in products.EnumerateArray())
                {
                    var game = ParseProduct(product);
                    if (game != null)
                    {
                        games.Add(game);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Microsoft Store response");
        }

        return games;
    }

    private SubscriptionGame? ParseProduct(System.Text.Json.JsonElement product)
    {
        try
        {
            var title = product.GetProperty("LocalizedProperties")
                .EnumerateArray()
                .FirstOrDefault()
                .GetProperty("ProductTitle")
                .GetString();

            if (string.IsNullOrEmpty(title))
                return null;

            return new SubscriptionGame
            {
                GameId = product.GetProperty("ProductId").GetString() ?? Guid.NewGuid().ToString(),
                Title = title,
                AvailableOn = new List<SubscriptionServiceType> { SubscriptionServiceType.XboxGamePass },
                Genres = new List<string>()
            };
        }
        catch
        {
            return null;
        }
    }
}
