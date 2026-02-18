using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System.Net;

namespace SaveState.Infrastructure.HealthChecks;

/// <summary>
/// Health check for external API availability.
/// </summary>
public class ExternalApiHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalApiHealthCheck> _logger;
    private readonly Dictionary<string, string> _externalEndpoints;

    public ExternalApiHealthCheck(
        HttpClient httpClient, 
        ILogger<ExternalApiHealthCheck> logger,
        Dictionary<string, string>? externalEndpoints = null)
    {
        _httpClient = httpClient;
        _logger = logger;
        _externalEndpoints = externalEndpoints ?? new Dictionary<string, string>
        {
            ["SteamAPI"] = "https://api.steampowered.com/ISteamWebAPIUtil/GetServerInfo/v1/",
            ["IGDB"] = "https://api.igdb.com/v4/games", // Will return 400 without auth, but confirms connectivity
        };
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, object>();
        var unhealthyServices = new List<string>();

        foreach (var endpoint in _externalEndpoints)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(5));
                
                var response = await _httpClient.GetAsync(endpoint.Value, cts.Token);
                
                // Any response (even 401/400) means the service is up
                var isHealthy = response.StatusCode != HttpStatusCode.ServiceUnavailable &&
                               response.StatusCode != HttpStatusCode.GatewayTimeout;
                
                results[endpoint.Key] = new
                {
                    Status = (int)response.StatusCode,
                    Healthy = isHealthy
                };

                if (!isHealthy)
                {
                    unhealthyServices.Add(endpoint.Key);
                }
            }
            catch (OperationCanceledException)
            {
                results[endpoint.Key] = new { Status = "Timeout", Healthy = false };
                unhealthyServices.Add(endpoint.Key);
            }
            catch (Exception ex)
            {
                results[endpoint.Key] = new { Status = $"Error: {ex.Message}", Healthy = false };
                unhealthyServices.Add(endpoint.Key);
            }
        }

        if (unhealthyServices.Any())
        {
            return HealthCheckResult.Degraded(
                $"External APIs degraded: {string.Join(", ", unhealthyServices)}",
                data: results);
        }

        return HealthCheckResult.Healthy("All external APIs are accessible", data: results);
    }
}
