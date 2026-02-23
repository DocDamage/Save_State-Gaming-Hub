using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary;
using SaveState.Core.Sync.Services;
using SaveState.Core.Sync.Services.DTOs;
using QualityLevel = SaveState.Core.Sync.Services.DTOs.QualityLevel;

namespace SaveState.Tests.Fakes;

/// <summary>
/// Fake implementation of ICloudGamingManager for integration testing.
/// Provides extended methods that the real implementation doesn't have yet.
/// </summary>
public class FakeCloudGamingManager : ICloudGamingManager
{
    private readonly ILogger<FakeCloudGamingManager> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly IGameRepository _gameRepository;

    private readonly Dictionary<Guid, CloudProviderConnection> _connectedProviders = new();
    private readonly Dictionary<string, CloudGamingSession> _sessions = new();
    private readonly Dictionary<string, CloudGameInfo> _games = new();
    private readonly Dictionary<string, DataCenterInfo> _dataCenters = new();
    private CloudGamingSession? _activeSession;

    public FakeCloudGamingManager(
        ILogger<FakeCloudGamingManager> logger,
        ITimeProvider timeProvider,
        IGameRepository gameRepository)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _gameRepository = gameRepository;

        InitializeFakeData();
    }

    private void InitializeFakeData()
    {
        // Initialize fake providers
        var providers = new[]
        {
            new CloudProviderInfo { Id = Guid.NewGuid(), Name = "GeForce NOW", Type = CloudGamingProvider.GeForceNow },
            new CloudProviderInfo { Id = Guid.NewGuid(), Name = "Xbox Cloud Gaming", Type = CloudGamingProvider.XboxCloud },
            new CloudProviderInfo { Id = Guid.NewGuid(), Name = "Amazon Luna", Type = CloudGamingProvider.AmazonLuna }
        };

        _availableProviders.AddRange(providers);

        // Initialize fake games
        var games = new[]
        {
            new CloudGameInfo { Id = "game_1", Title = "Cyberpunk 2077", ProviderId = providers[0].Id },
            new CloudGameInfo { Id = "game_2", Title = "The Witcher 3", ProviderId = providers[0].Id },
            new CloudGameInfo { Id = "game_3", Title = "Forza Horizon 5", ProviderId = providers[1].Id }
        };

        foreach (var game in games)
        {
            _games[game.Id] = game;
        }

        // Initialize fake data centers
        var dataCenters = new[]
        {
            new DataCenterInfo { Id = "dc_us_west", Name = "US West", Region = "us-west", Latency = 20 },
            new DataCenterInfo { Id = "dc_us_east", Name = "US East", Region = "us-east", Latency = 45 },
            new DataCenterInfo { Id = "dc_eu_west", Name = "EU West", Region = "eu-west", Latency = 120 }
        };

        foreach (var dc in dataCenters)
        {
            _dataCenters[dc.Id] = dc;
        }
    }

    private readonly List<CloudProviderInfo> _availableProviders = new();

    #region ICloudGamingManager Implementation

    public Task<Result<IReadOnlyList<CloudGamingProvider>>> GetAvailableProvidersAsync(CancellationToken ct = default)
    {
        var providers = _availableProviders.Select(p => p.Type).ToList();
        return Task.FromResult(Result.Success<IReadOnlyList<CloudGamingProvider>>(providers));
    }

    public Task<Result<CloudSession>> StartSessionAsync(Guid gameId, CloudGamingProvider provider, CancellationToken ct = default)
    {
        var session = new CloudSession(
            Guid.NewGuid(),
            gameId,
            provider,
            _timeProvider.UtcNow,
            new NetworkQuality(20, 2, 0, 100, QualityLevel.Good, _timeProvider.UtcNow));

        return Task.FromResult(Result.Success(session));
    }

    public Task<Result> EndSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result<NetworkQuality>> GetNetworkQualityAsync(CancellationToken ct = default)
    {
        var quality = new NetworkQuality(20, 2, 0, 100, QualityLevel.Good, _timeProvider.UtcNow);
        return Task.FromResult(Result.Success(quality));
    }

    public Task<Result<IReadOnlyList<CloudSession>>> GetActiveSessionsAsync(CancellationToken ct = default)
    {
        var sessions = _sessions.Values
            .Where(s => s.IsActive)
            .Select(s => new CloudSession(
                Guid.Parse(s.Id),
                Guid.Parse(s.GameId),
                GetProviderById(s.ProviderId) ?? CloudGamingProvider.GeForceNow,
                s.StartedAt,
                new NetworkQuality(20, 2, 0, 100, QualityLevel.Good, s.StartedAt)))
            .ToList();

        return Task.FromResult(Result.Success<IReadOnlyList<CloudSession>>(sessions));
    }

    public Task<Result> OptimizeNetworkSettingsAsync(CloudGamingProvider provider, CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success());
    }

    public async Task<Result<bool>> IsGameAvailableAsync(Guid gameId, CloudGamingProvider provider, CancellationToken ct = default)
    {
        var game = await _gameRepository.GetByIdAsync(Core.GameLibrary.ValueObjects.GameId.From(gameId), ct);
        if (game == null)
            return Result.Success(false);

        return Result.Success(true);
    }

    public Task<Result<IReadOnlyList<string>>> GetNetworkRecommendationsAsync(CloudGamingProvider provider, CancellationToken ct = default)
    {
        var recommendations = new[]
        {
            "Use wired connection for better stability",
            "Close bandwidth-intensive applications",
            "Choose the nearest data center"
        };
        return Task.FromResult(Result.Success<IReadOnlyList<string>>(recommendations));
    }

    public Result SetCloudAvailabilityOverride(Guid gameId, CloudGamingProvider provider, bool isAvailable)
    {
        return Result.Success();
    }

    public Result ClearCloudAvailabilityOverride(Guid gameId, CloudGamingProvider? provider = null)
    {
        return Result.Success();
    }

    public Result<IReadOnlyDictionary<CloudGamingProvider, bool>> GetCloudAvailabilityOverrides(Guid gameId)
    {
        return Result.Success<IReadOnlyDictionary<CloudGamingProvider, bool>>(new Dictionary<CloudGamingProvider, bool>());
    }

    #endregion

    #region Extended Methods for Tests

    public Task<Result<IReadOnlyList<CloudProviderInfo>>> GetAvailableProviderInfosAsync(CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success<IReadOnlyList<CloudProviderInfo>>(_availableProviders));
    }

    public Task<Result> ConnectToProviderAsync(Guid providerId, string token, string? refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(token) || token == "invalid_token")
        {
            return Task.FromResult(Result.Failure("Invalid credentials"));
        }

        _connectedProviders[providerId] = new CloudProviderConnection
        {
            ProviderId = providerId,
            Token = token,
            RefreshToken = refreshToken,
            ConnectedAt = _timeProvider.UtcNow
        };

        _logger.LogInformation("Connected to provider {ProviderId}", providerId);
        return Task.FromResult(Result.Success());
    }

    public Task<Result> DisconnectFromProviderAsync(Guid providerId, CancellationToken ct = default)
    {
        _connectedProviders.Remove(providerId);
        return Task.FromResult(Result.Success());
    }

    public Task<Result<CloudProviderStatus>> GetProviderStatusAsync(Guid providerId, CancellationToken ct = default)
    {
        var isConnected = _connectedProviders.ContainsKey(providerId);
        var status = new CloudProviderStatus
        {
            ProviderId = providerId,
            IsConnected = isConnected,
            ConnectionQuality = isConnected ? ConnectionQuality.Good : ConnectionQuality.Disconnected
        };
        return Task.FromResult(Result.Success(status));
    }

    public Task<Result<bool>> IsProviderConnectedAsync(Guid providerId, CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success(_connectedProviders.ContainsKey(providerId)));
    }

    public Task<Result<CloudProviderInfo>> GetProviderByIdAsync(Guid providerId, CancellationToken ct = default)
    {
        var provider = _availableProviders.FirstOrDefault(p => p.Id == providerId);
        if (provider == null)
            return Task.FromResult(Result.Failure<CloudProviderInfo>("Provider not found"));

        return Task.FromResult(Result.Success(provider));
    }

    public Task<Result<CloudGamingSession>> StartCloudSessionAsync(Guid providerId, string gameId, StreamQuality quality, CancellationToken ct = default)
    {
        if (!_connectedProviders.ContainsKey(providerId))
        {
            return Task.FromResult(Result.Failure<CloudGamingSession>("Not connected to provider"));
        }

        var session = new CloudGamingSession
        {
            Id = Guid.NewGuid().ToString(),
            ProviderId = providerId,
            GameId = gameId,
            Quality = quality,
            StartedAt = _timeProvider.UtcNow,
            IsActive = true
        };

        _sessions[session.Id] = session;
        _activeSession = session;

        return Task.FromResult(Result.Success(session));
    }

    public Task<Result> StopCloudSessionAsync(string sessionId, CancellationToken ct = default)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.IsActive = false;
            session.EndedAt = _timeProvider.UtcNow;

            if (_activeSession?.Id == sessionId)
            {
                _activeSession = null;
            }
        }

        return Task.FromResult(Result.Success());
    }

    public Task<Result<CloudGamingSession?>> GetActiveSessionAsync(CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success<CloudGamingSession?>(_activeSession));
    }

    public Task<Result<CloudGamingSession>> GetSessionAsync(string sessionId, CancellationToken ct = default)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            return Task.FromResult(Result.Success(session));
        }

        return Task.FromResult(Result.Failure<CloudGamingSession>("Session not found"));
    }

    public Task<Result> ResumeCloudSessionAsync(string sessionId, CancellationToken ct = default)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.IsActive = true;
            _activeSession = session;
        }

        return Task.FromResult(Result.Success());
    }

    public Task<Result> PauseCloudSessionAsync(string sessionId, CancellationToken ct = default)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.IsActive = false;
            if (_activeSession?.Id == sessionId)
            {
                _activeSession = null;
            }
        }

        return Task.FromResult(Result.Success());
    }

    public Task<Result> ChangeStreamQualityAsync(string sessionId, StreamQuality quality, CancellationToken ct = default)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.Quality = quality;
        }

        return Task.FromResult(Result.Success());
    }

    public Task<Result<StreamQuality>> GetRecommendedQualityAsync(CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success(StreamQuality.High));
    }

    public Task<Result<IReadOnlyList<StreamQuality>>> GetAvailableQualitiesAsync(Guid providerId, CancellationToken ct = default)
    {
        var qualities = new[] { StreamQuality.Low, StreamQuality.Medium, StreamQuality.High, StreamQuality.Ultra };
        return Task.FromResult(Result.Success<IReadOnlyList<StreamQuality>>(qualities));
    }

    public Task<Result> SetMaxBitrateAsync(string sessionId, int bitrate, CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result<ConnectionTestResult>> TestConnectionAsync(Guid providerId, CancellationToken ct = default)
    {
        var result = new ConnectionTestResult
        {
            Success = true,
            Latency = 20,
            PacketLoss = 0,
            Bandwidth = 100
        };
        return Task.FromResult(Result.Success(result));
    }

    public Task<Result<ConnectionMetrics>> GetConnectionMetricsAsync(string sessionId, CancellationToken ct = default)
    {
        var metrics = new ConnectionMetrics
        {
            SessionId = sessionId,
            AverageLatency = 20,
            PacketLoss = 0,
            Bandwidth = 100
        };
        return Task.FromResult(Result.Success(metrics));
    }

    public Task<Result<double>> GetLatencyAsync(Guid providerId, CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success(20.0));
    }

    public Task<Result<double>> GetPacketLossAsync(Guid providerId, CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success(0.0));
    }

    public Task<Result<double>> GetBandwidthAsync(Guid providerId, CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success(100.0));
    }

    public Task<Result<IReadOnlyList<DataCenterInfo>>> GetAvailableDataCentersAsync(Guid providerId, CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success<IReadOnlyList<DataCenterInfo>>(_dataCenters.Values.ToList()));
    }

    public Task<Result<DataCenterInfo>> GetNearestDataCenterAsync(Guid providerId, CancellationToken ct = default)
    {
        var nearest = _dataCenters.Values.OrderBy(dc => dc.Latency).First();
        return Task.FromResult(Result.Success(nearest));
    }

    public Task<Result> SelectDataCenterAsync(Guid providerId, string dataCenterId, CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result<double>> GetDataCenterLatencyAsync(Guid providerId, string dataCenterId, CancellationToken ct = default)
    {
        if (_dataCenters.TryGetValue(dataCenterId, out var dc))
        {
            return Task.FromResult(Result.Success((double)dc.Latency));
        }
        return Task.FromResult(Result.Success(50.0));
    }

    public Task<Result<IReadOnlyList<CloudSaveState>>> GetCloudSaveStatesAsync(Guid providerId, string gameId, CancellationToken ct = default)
    {
        var saveStates = new List<CloudSaveState>
        {
            new() { Id = "save_1", GameId = gameId, CreatedAt = _timeProvider.UtcNow.AddHours(-1) },
            new() { Id = "save_2", GameId = gameId, CreatedAt = _timeProvider.UtcNow.AddHours(-2) }
        };
        return Task.FromResult(Result.Success<IReadOnlyList<CloudSaveState>>(saveStates));
    }

    public Task<Result> SyncSaveStateToCloudAsync(Guid providerId, string gameId, string saveData, CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result<string>> DownloadSaveStateFromCloudAsync(Guid providerId, string gameId, string saveStateId, CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success("save_state_data"));
    }

    public Task<Result> DeleteCloudSaveStateAsync(Guid providerId, string gameId, string saveStateId, CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success());
    }

    #endregion

    #region Helper Methods

    private CloudGamingProvider? GetProviderById(Guid providerId)
    {
        return _availableProviders.FirstOrDefault(p => p.Id == providerId)?.Type;
    }

    #endregion
}

#region Supporting Types

public class CloudProviderInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public CloudGamingProvider Type { get; set; }
}

public class CloudProviderConnection
{
    public Guid ProviderId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTime ConnectedAt { get; set; }
}

public class CloudProviderStatus
{
    public Guid ProviderId { get; set; }
    public bool IsConnected { get; set; }
    public ConnectionQuality ConnectionQuality { get; set; }
}

public class CloudGamingSession
{
    public string Id { get; set; } = string.Empty;
    public Guid ProviderId { get; set; }
    public string GameId { get; set; } = string.Empty;
    public StreamQuality Quality { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public bool IsActive { get; set; }
}

public class CloudGameInfo
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public Guid ProviderId { get; set; }
}

public class DataCenterInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public int Latency { get; set; }
}

public class ConnectionTestResult
{
    public bool Success { get; set; }
    public double Latency { get; set; }
    public double PacketLoss { get; set; }
    public double Bandwidth { get; set; }
}

public class ConnectionMetrics
{
    public string SessionId { get; set; } = string.Empty;
    public double AverageLatency { get; set; }
    public double PacketLoss { get; set; }
    public double Bandwidth { get; set; }
}

public class CloudSaveState
{
    public string Id { get; set; } = string.Empty;
    public string GameId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public enum ConnectionQuality
{
    Disconnected,
    Poor,
    Fair,
    Good,
    Excellent
}

public enum StreamQuality
{
    Low,
    Medium,
    High,
    Ultra
}

#endregion
