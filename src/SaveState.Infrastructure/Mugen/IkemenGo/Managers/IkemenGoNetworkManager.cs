using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;

namespace SaveState.Infrastructure.Mugen.IkemenGo.Managers;

/// <summary>
/// Manages IKEMEN GO network features, online play, and rollback netcode configuration.
/// </summary>
public sealed class IkemenGoNetworkManager
{
    private readonly ILogger<IkemenGoNetworkManager> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly HttpClient _httpClient;

    private static readonly List<IkemenGoServer> DefaultServers = new()
    {
        new("IKEMEN Official", "lobby.ikemen.dev", 7500, "US-East", 12, 100, 45),
        new("EU Server", "eu.ikemen.dev", 7500, "Europe", 8, 100, 120),
        new("Asia Server", "asia.ikemen.dev", 7500, "Asia", 15, 100, 180)
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="IkemenGoNetworkManager"/> class.
    /// </summary>
    public IkemenGoNetworkManager(
        ILogger<IkemenGoNetworkManager> logger,
        ITimeProvider timeProvider,
        HttpClient httpClient)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _httpClient = httpClient;
    }

    /// <summary>
    /// Configures online play settings.
    /// </summary>
    public async Task<Result> ConfigureOnlinePlayAsync(
        string configPath,
        IkemenGoNetworkSettings settings,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Configuring online play settings");

            if (!File.Exists(configPath))
            {
                return Result.Failure("Config file not found", ErrorType.NotFound);
            }

            var json = await File.ReadAllTextAsync(configPath, ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Create updated config with new network settings
            var config = new Dictionary<string, object>();
            foreach (var property in root.EnumerateObject())
            {
                config[property.Name] = property.Value;
            }

            // Update network section
            config["Network"] = new Dictionary<string, object>
            {
                ["playerName"] = settings.PlayerName,
                ["listenPort"] = settings.ListenPort,
                ["maxPing"] = settings.MaxPing,
                ["useLobby"] = settings.UseLobby,
                ["lobbyServer"] = settings.LobbyServer,
                ["rollback"] = new Dictionary<string, object>
                {
                    ["enabled"] = settings.Rollback.Enabled,
                    ["inputDelay"] = settings.Rollback.InputDelay,
                    ["rollbackFrames"] = settings.Rollback.RollbackFrames,
                    ["desyncDetection"] = settings.Rollback.DesyncDetection
                }
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            var updatedJson = JsonSerializer.Serialize(config, options);
            await File.WriteAllTextAsync(configPath, updatedJson, ct);

            _logger.LogInformation("Online play settings configured successfully");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure online play");
            return Result.Failure($"Configuration failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Tests network connectivity for online play.
    /// </summary>
    public async Task<Result<NetworkTestResult>> TestNetworkConnectionAsync(
        string host,
        int port,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Testing network connection to {Host}:{Port}", host, port);

            using var ping = new Ping();
            var reply = await ping.SendPingAsync(host, 5000).ConfigureAwait(false);

            // Also try TCP connection test
            bool tcpSuccess = false;
            long tcpLatency = -1;
            try
            {
                using var tcpClient = new System.Net.Sockets.TcpClient();
                var connectStopwatch = Stopwatch.StartNew();
                await tcpClient.ConnectAsync(host, port).WaitAsync(TimeSpan.FromSeconds(5), ct);
                connectStopwatch.Stop();
                tcpLatency = connectStopwatch.ElapsedMilliseconds;
                tcpSuccess = true;
                tcpClient.Close();
            }
            catch { /* TCP test is optional */ }

            var result = new NetworkTestResult(
                reply.Status == IPStatus.Success || tcpSuccess,
                reply.Status == IPStatus.Success ? (int)reply.RoundtripTime : (int)tcpLatency,
                reply.Status == IPStatus.Success ? 0 : (tcpSuccess ? 0 : 100),
                reply.Status == IPStatus.Success || tcpSuccess ? null : $"Ping: {reply.Status}, TCP: {(tcpSuccess ? "Success" : "Failed")}");

            return Result<NetworkTestResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Network test failed");
            return Result<NetworkTestResult>.Success(new NetworkTestResult(
                false,
                -1,
                0,
                ex.Message));
        }
    }

    /// <summary>
    /// Gets available online lobby servers.
    /// </summary>
    public async Task<Result<IReadOnlyList<IkemenGoServer>>> GetLobbyServersAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Retrieving lobby servers");

            // Try to fetch from official server list API
            try
            {
                var response = await _httpClient.GetAsync("https://ikemen.dev/api/servers", ct);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(ct);
                    var servers = JsonSerializer.Deserialize<List<IkemenGoServer>>(content);
                    if (servers?.Count > 0)
                    {
                        return Result<IReadOnlyList<IkemenGoServer>>.Success(servers);
                    }
                }
            }
            catch { /* Fall back to default servers */ }

            // Return default/community servers
            return Result<IReadOnlyList<IkemenGoServer>>.Success(DefaultServers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get lobby servers");
            return Result<IReadOnlyList<IkemenGoServer>>.Failure($"Failed to get servers: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Configures rollback netcode settings.
    /// </summary>
    public async Task<Result> ConfigureRollbackNetcodeAsync(
        string configPath,
        RollbackNetcodeSettings settings,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Configuring rollback netcode settings");

            if (!File.Exists(configPath))
            {
                return Result.Failure("Config file not found", ErrorType.NotFound);
            }

            var json = await File.ReadAllTextAsync(configPath, ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var config = new Dictionary<string, object>();
            foreach (var property in root.EnumerateObject())
            {
                config[property.Name] = property.Value;
            }

            // Get or create network section
            if (!config.ContainsKey("Network"))
            {
                config["Network"] = new Dictionary<string, object>();
            }

            var networkConfig = (Dictionary<string, object>)config["Network"];
            networkConfig["rollback"] = new Dictionary<string, object>
            {
                ["enabled"] = settings.Enabled,
                ["inputDelay"] = settings.InputDelay,
                ["rollbackFrames"] = settings.RollbackFrames,
                ["desyncDetection"] = settings.DesyncDetection
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            var updatedJson = JsonSerializer.Serialize(config, options);
            await File.WriteAllTextAsync(configPath, updatedJson, ct);

            _logger.LogInformation("Rollback netcode settings configured successfully");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure rollback netcode");
            return Result.Failure($"Configuration failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Validates port forwarding for hosting.
    /// </summary>
    public async Task<Result<PortValidationResult>> ValidatePortForwardingAsync(
        int port,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Validating port forwarding for port {Port}", port);

            // Check if port is in valid range
            if (port < 1024 || port > 65535)
            {
                return Result<PortValidationResult>.Success(new PortValidationResult(
                    false,
                    "unknown",
                    "Port must be between 1024 and 65535"));
            }

            // Try to bind to the port to see if it's available
            bool isPortAvailable = false;
            try
            {
                var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, port);
                listener.Start();
                listener.Stop();
                isPortAvailable = true;
            }
            catch { /* Port is in use */ }

            // In a real implementation, this would check external connectivity
            // through a STUN server or external service
            var publicIp = await GetPublicIpAsync(ct);

            return Result<PortValidationResult>.Success(new PortValidationResult(
                isPortAvailable,
                publicIp ?? "unknown",
                isPortAvailable ? null : "Port is already in use or blocked"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Port validation failed");
            return Result<PortValidationResult>.Success(new PortValidationResult(
                false,
                "unknown",
                ex.Message));
        }
    }

    /// <summary>
    /// Gets the recommended input delay based on ping.
    /// </summary>
    public int GetRecommendedInputDelay(int pingMs)
    {
        return pingMs switch
        {
            < 30 => 0,
            < 60 => 1,
            < 100 => 2,
            < 150 => 3,
            _ => 4
        };
    }

    /// <summary>
    /// Gets the recommended rollback frames based on connection quality.
    /// </summary>
    public int GetRecommendedRollbackFrames(int pingMs, int packetLossPercent)
    {
        if (packetLossPercent > 5)
            return 10;
        
        return pingMs switch
        {
            < 50 => 3,
            < 100 => 5,
            < 200 => 8,
            _ => 10
        };
    }

    private async Task<string?> GetPublicIpAsync(CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.GetAsync("https://api.ipify.org", ct);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync(ct);
            }
        }
        catch { /* Ignore */ }

        return null;
    }
}
