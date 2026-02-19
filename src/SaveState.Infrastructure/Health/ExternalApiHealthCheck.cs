using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Configuration;
using SaveState.Infrastructure.External;

namespace SaveState.Infrastructure.Health;

/// <summary>
/// Health check for external API services (Steam, GOG, Epic, IGDB).
/// Tests connectivity and basic functionality of external dependencies.
/// </summary>
public class ExternalApiHealthCheck : IHealthCheck
{
    private readonly ISteamApiClient _steamClient;
    private readonly IGogApiClient _gogClient;
    private readonly IEpicApiClient _epicClient;
    private readonly IIgdbApiClient _igdbClient;
    private readonly ILogger<ExternalApiHealthCheck> _logger;
    private readonly SteamOptions _steamOptions;
    private readonly GogOptions _gogOptions;
    private readonly EpicOptions _epicOptions;
    private readonly IgdbOptions _igdbOptions;

    public ExternalApiHealthCheck(
        ISteamApiClient steamClient,
        IGogApiClient gogClient,
        IEpicApiClient epicClient,
        IIgdbApiClient igdbClient,
        ILogger<ExternalApiHealthCheck> logger,
        IOptions<SteamOptions> steamOptions,
        IOptions<GogOptions> gogOptions,
        IOptions<EpicOptions> epicOptions,
        IOptions<IgdbOptions> igdbOptions)
    {
        _steamClient = steamClient;
        _gogClient = gogClient;
        _epicClient = epicClient;
        _igdbClient = igdbClient;
        _logger = logger;
        _steamOptions = steamOptions.Value;
        _gogOptions = gogOptions.Value;
        _epicOptions = epicOptions.Value;
        _igdbOptions = igdbOptions.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, object>();
        var degradedServices = new List<string>();
        var unhealthyServices = new List<string>();

        try
        {
            // Test Steam API
            if (!string.IsNullOrEmpty(_steamOptions.ApiKey))
            {
                var steamResult = await TestSteamApiAsync(cancellationToken);
                results["Steam"] = steamResult;
                if (steamResult.Status == HealthStatus.Unhealthy)
                    unhealthyServices.Add("Steam");
                else if (steamResult.Status == HealthStatus.Degraded)
                    degradedServices.Add("Steam");
            }
            else
            {
                results["Steam"] = new { Status = "NotConfigured", Message = "API key not configured" };
            }

            // Test GOG API
            if (!string.IsNullOrEmpty(_gogOptions.Username) && !string.IsNullOrEmpty(_gogOptions.Password))
            {
                var gogResult = await TestGogApiAsync(cancellationToken);
                results["GOG"] = gogResult;
                if (gogResult.Status == HealthStatus.Unhealthy)
                    unhealthyServices.Add("GOG");
                else if (gogResult.Status == HealthStatus.Degraded)
                    degradedServices.Add("GOG");
            }
            else
            {
                results["GOG"] = new { Status = "NotConfigured", Message = "Credentials not configured" };
            }

            // Test Epic API
            if (!string.IsNullOrEmpty(_epicOptions.AccountId) && !string.IsNullOrEmpty(_epicOptions.AuthToken))
            {
                var epicResult = await TestEpicApiAsync(cancellationToken);
                results["Epic"] = epicResult;
                if (epicResult.Status == HealthStatus.Unhealthy)
                    unhealthyServices.Add("Epic");
                else if (epicResult.Status == HealthStatus.Degraded)
                    degradedServices.Add("Epic");
            }
            else
            {
                results["Epic"] = new { Status = "NotConfigured", Message = "Credentials not configured" };
            }

            // Test IGDB API
            if (!string.IsNullOrEmpty(_igdbOptions.ClientId) && !string.IsNullOrEmpty(_igdbOptions.ClientSecret))
            {
                var igdbResult = await TestIgdbApiAsync(cancellationToken);
                results["IGDB"] = igdbResult;
                if (igdbResult.Status == HealthStatus.Unhealthy)
                    unhealthyServices.Add("IGDB");
                else if (igdbResult.Status == HealthStatus.Degraded)
                    degradedServices.Add("IGDB");
            }
            else
            {
                results["IGDB"] = new { Status = "NotConfigured", Message = "Client credentials not configured" };
            }

            // Determine overall health
            if (unhealthyServices.Any())
            {
                return HealthCheckResult.Unhealthy(
                    $"External API services are unhealthy: {string.Join(", ", unhealthyServices)}",
                    data: results);
            }

            if (degradedServices.Any())
            {
                return HealthCheckResult.Degraded(
                    $"External API services are degraded: {string.Join(", ", degradedServices)}",
                    data: results);
            }

            return HealthCheckResult.Healthy("All configured external API services are healthy", results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check external API health");
            return HealthCheckResult.Unhealthy("External API health check failed", ex, results);
        }
    }

    private async Task<HealthStatusResult> TestSteamApiAsync(CancellationToken ct)
    {
        try
        {
            // Try to get game details for a known Steam app (Steam itself - app ID 753)
            var metadata = await _steamClient.GetGameDetailsAsync("753", ct);

            if (metadata != null && !string.IsNullOrEmpty(metadata.Title))
            {
                return new HealthStatusResult(HealthStatus.Healthy, $"Steam API is responding - retrieved: {metadata.Title}");
            }
            else
            {
                return new HealthStatusResult(HealthStatus.Degraded, "Steam API returned empty or invalid metadata");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Steam API health check failed");
            return new HealthStatusResult(HealthStatus.Unhealthy, $"Steam API check failed: {ex.Message}");
        }
    }

    private async Task<HealthStatusResult> TestGogApiAsync(CancellationToken ct)
    {
        try
        {
            // Try to get game details (using a sample ID for connectivity test)
            var metadataResult = await _gogClient.GetGameDetailsAsync("sample-game-id", ct);

            if (metadataResult.IsSuccess && metadataResult.Value != null && !string.IsNullOrEmpty(metadataResult.Value.Title))
            {
                return new HealthStatusResult(HealthStatus.Healthy, $"GOG API is responding - retrieved: {metadataResult.Value.Title}");
            }
            else if (metadataResult.IsFailure)
            {
                return new HealthStatusResult(HealthStatus.Degraded, $"GOG API returned failure: {metadataResult.Error}");
            }
            else
            {
                // GOG API might return empty metadata for invalid IDs, which is expected for health checks
                return new HealthStatusResult(HealthStatus.Degraded, "GOG API returned empty metadata (expected for test ID)");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GOG API health check failed");
            return new HealthStatusResult(HealthStatus.Unhealthy, $"GOG API check failed: {ex.Message}");
        }
    }

    private async Task<HealthStatusResult> TestEpicApiAsync(CancellationToken ct)
    {
        try
        {
            // Try to get game details (using a sample ID for connectivity test)
            var metadataResult = await _epicClient.GetGameDetailsAsync("sample-game-id", ct);

            if (metadataResult.IsSuccess && metadataResult.Value != null && !string.IsNullOrEmpty(metadataResult.Value.Title))
            {
                return new HealthStatusResult(HealthStatus.Healthy, $"Epic API is responding - retrieved: {metadataResult.Value.Title}");
            }
            else if (metadataResult.IsFailure)
            {
                return new HealthStatusResult(HealthStatus.Degraded, $"Epic API returned failure: {metadataResult.Error}");
            }
            else
            {
                // Epic API might return empty metadata for invalid IDs, which is expected for health checks
                return new HealthStatusResult(HealthStatus.Degraded, "Epic API returned empty metadata (expected for test ID)");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Epic API health check failed");
            return new HealthStatusResult(HealthStatus.Unhealthy, $"Epic API check failed: {ex.Message}");
        }
    }

    private async Task<HealthStatusResult> TestIgdbApiAsync(CancellationToken ct)
    {
        try
        {
            // Try to search for a known game (e.g., "Super Mario")
            var games = await _igdbClient.SearchGamesAsync("Super Mario", ct);

            if (games != null && games.Count > 0)
            {
                return new HealthStatusResult(HealthStatus.Healthy, $"IGDB API is responding (found {games.Count} results)");
            }
            else
            {
                return new HealthStatusResult(HealthStatus.Degraded, "IGDB API returned no results for test query");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "IGDB API health check failed");
            return new HealthStatusResult(HealthStatus.Unhealthy, $"IGDB API check failed: {ex.Message}");
        }
    }

    private record HealthStatusResult(HealthStatus Status, string Message);
}
