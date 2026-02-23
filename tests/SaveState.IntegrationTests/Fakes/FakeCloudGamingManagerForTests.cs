using SaveState.Core.Common;

namespace SaveState.IntegrationTests;

// These types are defined in CloudGamingIntegrationTests.cs
// Using the test-defined interfaces, not the Core interfaces

/// <summary>
/// Fake implementation of ICloudGamingManager for integration tests.
/// Implements the test-defined interface from CloudGamingIntegrationTests.cs.
/// </summary>
public class FakeCloudGamingManagerForTests : ICloudGamingManager
{
    private readonly Dictionary<string, CloudProvider> _providers = new();
    private readonly Dictionary<string, ProviderConnection> _connections = new();
    private readonly Dictionary<string, CloudSession> _sessions = new();
    private readonly Dictionary<string, DataCenter> _dataCenters = new();
    private readonly Dictionary<string, List<CloudSaveState>> _saveStates = new();
    private readonly Dictionary<string, List<InputMapping>> _inputMappings = new();
    private readonly Dictionary<string, CloudGame> _knownGames = new(); // Track valid games
    private CloudSession? _activeSession;

    public FakeCloudGamingManagerForTests()
    {
        InitializeFakeData();
    }

    /// <summary>
    /// Resets the connection state for tests that require clean state.
    /// </summary>
    public void ResetConnections()
    {
        _connections.Clear();
        _sessions.Clear();
        _activeSession = null;
    }

    private void InitializeFakeData()
    {
        // Initialize providers
        _providers["geforce_now"] = new CloudProvider
        {
            Id = "geforce_now",
            Name = "GeForce NOW",
            Description = "NVIDIA's cloud gaming service",
            IconUrl = "https://example.com/geforce.png",
            RequiresSubscription = true,
            SupportedQualities = new List<StreamQuality> { StreamQuality.Low, StreamQuality.Medium, StreamQuality.High, StreamQuality.Ultra }
        };

        _providers["xbox_cloud"] = new CloudProvider
        {
            Id = "xbox_cloud",
            Name = "Xbox Cloud Gaming",
            Description = "Microsoft's cloud gaming service",
            IconUrl = "https://example.com/xbox.png",
            RequiresSubscription = true,
            SupportedQualities = new List<StreamQuality> { StreamQuality.Low, StreamQuality.Medium, StreamQuality.High, StreamQuality.Ultra }
        };

        _providers["amazon_luna"] = new CloudProvider
        {
            Id = "amazon_luna",
            Name = "Amazon Luna",
            Description = "Amazon's cloud gaming service",
            IconUrl = "https://example.com/luna.png",
            RequiresSubscription = true,
            SupportedQualities = new List<StreamQuality> { StreamQuality.Low, StreamQuality.Medium, StreamQuality.High }
        };

        // Initialize data centers
        _dataCenters["dc_us_west"] = new DataCenter
        {
            Id = "dc_us_west",
            Name = "US West (Oregon)",
            Region = "us-west-2",
            Country = "USA",
            Latitude = 45.5231,
            Longitude = -122.6765
        };

        _dataCenters["dc_us_east"] = new DataCenter
        {
            Id = "dc_us_east",
            Name = "US East (Virginia)",
            Region = "us-east-1",
            Country = "USA",
            Latitude = 37.4316,
            Longitude = -78.6569
        };

        _dataCenters["dc_eu_west"] = new DataCenter
        {
            Id = "dc_eu_west",
            Name = "EU West (Ireland)",
            Region = "eu-west-1",
            Country = "Ireland",
            Latitude = 53.3498,
            Longitude = -6.2603
        };

        // Initialize input mappings
        _inputMappings["geforce_now"] = new List<InputMapping>
        {
            new() { Action = "Jump", Key = "Space", AltKey = "GamepadA" },
            new() { Action = "Crouch", Key = "LeftControl", AltKey = "GamepadB" }
        };

        _inputMappings["xbox_cloud"] = new List<InputMapping>
        {
            new() { Action = "Jump", Key = "Space", AltKey = "GamepadA" },
            new() { Action = "Crouch", Key = "LeftControl", AltKey = "GamepadB" }
        };

        _inputMappings["amazon_luna"] = new List<InputMapping>
        {
            new() { Action = "Jump", Key = "Space", AltKey = "GamepadA" },
            new() { Action = "Crouch", Key = "LeftControl", AltKey = "GamepadB" }
        };

        // Initialize known games for validation
        _knownGames["game_123"] = new CloudGame { Id = "game_123", ProviderId = "geforce_now", Title = "Test Game" };
        _knownGames["game_1"] = new CloudGame { Id = "game_1", ProviderId = "geforce_now", Title = "Cyberpunk 2077" };
        _knownGames["game_2"] = new CloudGame { Id = "game_2", ProviderId = "geforce_now", Title = "The Witcher 3" };
        _knownGames["game_3"] = new CloudGame { Id = "game_3", ProviderId = "xbox_cloud", Title = "Forza Horizon 5" };
        _knownGames["game_4"] = new CloudGame { Id = "game_4", ProviderId = "xbox_cloud", Title = "Halo Infinite" };
        _knownGames["game_5"] = new CloudGame { Id = "game_5", ProviderId = "amazon_luna", Title = "Assassin's Creed Valhalla" };
    }

    public Task<Result<List<CloudProvider>>> GetAvailableProvidersAsync()
    {
        return Task.FromResult(Result.Success(_providers.Values.ToList()));
    }

    public Task<Result<CloudProvider>> GetProviderByIdAsync(string providerId)
    {
        if (_providers.TryGetValue(providerId, out var provider))
        {
            return Task.FromResult(Result.Success(provider));
        }
        return Task.FromResult(Result.Failure<CloudProvider>("Provider not found", ErrorType.NotFound));
    }

    public Task<Result<bool>> ConnectToProviderAsync(string providerId, string accessToken, string? refreshToken)
    {
        if (!_providers.ContainsKey(providerId))
        {
            return Task.FromResult(Result.Failure<bool>("Provider not found", ErrorType.NotFound));
        }

        if (string.IsNullOrEmpty(accessToken) || accessToken == "invalid_token")
        {
            return Task.FromResult(Result.Failure<bool>("Invalid credentials", ErrorType.Unauthorized));
        }

        _connections[providerId] = new ProviderConnection
        {
            ProviderId = providerId,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ConnectedAt = DateTime.UtcNow
        };

        return Task.FromResult(Result.Success(true));
    }

    public Task<Result<bool>> DisconnectFromProviderAsync(string providerId)
    {
        _connections.Remove(providerId);
        return Task.FromResult(Result.Success(true));
    }

    public Task<Result<ProviderStatus>> GetProviderStatusAsync(string providerId)
    {
        var isConnected = _connections.ContainsKey(providerId);
        var status = new ProviderStatus
        {
            ProviderId = providerId,
            IsConnected = isConnected,
            UserDisplayName = isConnected ? "Test User" : null,
            ConnectedAt = isConnected ? _connections[providerId].ConnectedAt : null,
            TokenExpiresAt = isConnected ? DateTime.UtcNow.AddHours(1) : null
        };

        return Task.FromResult(Result.Success(status));
    }

    public Task<Result<bool>> IsProviderConnectedAsync(string providerId)
    {
        return Task.FromResult(Result.Success(_connections.ContainsKey(providerId)));
    }

    public Task<Result<CloudSession>> StartCloudSessionAsync(string providerId, string gameId, StreamQuality quality)
    {
        if (!_connections.ContainsKey(providerId))
        {
            return Task.FromResult(Result.Failure<CloudSession>("Not connected to provider", ErrorType.Unauthorized));
        }

        // Validate that the gameId exists (edge case: invalid game ID)
        if (!_knownGames.ContainsKey(gameId))
        {
            return Task.FromResult(Result.Failure<CloudSession>($"Game '{gameId}' not found", ErrorType.NotFound));
        }

        var session = new CloudSession
        {
            Id = Guid.NewGuid().ToString(),
            ProviderId = providerId,
            GameId = gameId,
            Quality = quality,
            IsActive = true,
            StartedAt = DateTime.UtcNow
        };

        _sessions[session.Id] = session;
        _activeSession = session;

        return Task.FromResult(Result.Success(session));
    }

    public Task<Result<bool>> StopCloudSessionAsync(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.IsActive = false;
            session.EndedAt = DateTime.UtcNow;

            if (_activeSession?.Id == sessionId)
            {
                _activeSession = null;
            }

            return Task.FromResult(Result.Success(true));
        }

        return Task.FromResult(Result.Failure<bool>("Session not found", ErrorType.NotFound));
    }

    public Task<Result<CloudSession>> GetSessionAsync(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            return Task.FromResult(Result.Success(session));
        }

        return Task.FromResult(Result.Failure<CloudSession>("Session not found", ErrorType.NotFound));
    }

    public Task<Result<CloudSession?>> GetActiveSessionAsync()
    {
        return Task.FromResult(Result.Success<CloudSession?>(_activeSession));
    }

    public Task<Result<bool>> PauseCloudSessionAsync(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.IsActive = false;
            if (_activeSession?.Id == sessionId)
            {
                _activeSession = null;
            }
            return Task.FromResult(Result.Success(true));
        }

        return Task.FromResult(Result.Failure<bool>("Session not found", ErrorType.NotFound));
    }

    public Task<Result<bool>> ResumeCloudSessionAsync(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.IsActive = true;
            _activeSession = session;
            return Task.FromResult(Result.Success(true));
        }

        return Task.FromResult(Result.Failure<bool>("Session not found", ErrorType.NotFound));
    }

    public Task<Result<bool>> ChangeStreamQualityAsync(string sessionId, StreamQuality quality)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.Quality = quality;
            return Task.FromResult(Result.Success(true));
        }

        return Task.FromResult(Result.Failure<bool>("Session not found", ErrorType.NotFound));
    }

    public Task<Result<StreamQuality>> GetRecommendedQualityAsync()
    {
        return Task.FromResult(Result.Success(StreamQuality.High));
    }

    public Task<Result<List<StreamQuality>>> GetAvailableQualitiesAsync(string providerId)
    {
        if (_providers.TryGetValue(providerId, out var provider))
        {
            return Task.FromResult(Result.Success(provider.SupportedQualities));
        }

        return Task.FromResult(Result.Success(new List<StreamQuality> { StreamQuality.Low, StreamQuality.Medium, StreamQuality.High }));
    }

    public Task<Result<bool>> SetMaxBitrateAsync(string sessionId, int bitrateKbps)
    {
        if (_sessions.ContainsKey(sessionId))
        {
            return Task.FromResult(Result.Success(true));
        }

        return Task.FromResult(Result.Failure<bool>("Session not found", ErrorType.NotFound));
    }

    public Task<Result<ConnectionTestResult>> TestConnectionAsync(string providerId)
    {
        var result = new ConnectionTestResult
        {
            Success = true,
            LatencyMs = 20,
            PacketLoss = 0.5,
            BandwidthKbps = 50000
        };

        return Task.FromResult(Result.Success(result));
    }

    public Task<Result<ConnectionMetrics>> GetConnectionMetricsAsync(string sessionId)
    {
        if (!_sessions.ContainsKey(sessionId))
        {
            return Task.FromResult(Result.Failure<ConnectionMetrics>("Session not found", ErrorType.NotFound));
        }

        var metrics = new ConnectionMetrics
        {
            CurrentLatencyMs = 20,
            AverageLatencyMs = 22,
            PacketLoss = 0.5,
            CurrentBitrateKbps = 25000,
            FramesPerSecond = 60,
            Timestamp = DateTime.UtcNow
        };

        return Task.FromResult(Result.Success(metrics));
    }

    public Task<Result<int>> GetLatencyAsync(string providerId)
    {
        return Task.FromResult(Result.Success(20));
    }

    public Task<Result<double>> GetPacketLossAsync(string providerId)
    {
        return Task.FromResult(Result.Success(0.5));
    }

    public Task<Result<int>> GetBandwidthAsync(string providerId)
    {
        return Task.FromResult(Result.Success(50000));
    }

    public Task<Result<List<DataCenter>>> GetAvailableDataCentersAsync(string providerId)
    {
        return Task.FromResult(Result.Success(_dataCenters.Values.ToList()));
    }

    public Task<Result<DataCenter>> GetNearestDataCenterAsync(string providerId)
    {
        var nearest = _dataCenters.Values.First();
        return Task.FromResult(Result.Success(nearest));
    }

    public Task<Result<bool>> SelectDataCenterAsync(string providerId, string dataCenterId)
    {
        if (!_dataCenters.ContainsKey(dataCenterId))
        {
            return Task.FromResult(Result.Failure<bool>("Data center not found", ErrorType.NotFound));
        }

        return Task.FromResult(Result.Success(true));
    }

    public Task<Result<int>> GetDataCenterLatencyAsync(string providerId, string dataCenterId)
    {
        if (_dataCenters.TryGetValue(dataCenterId, out var dc))
        {
            return Task.FromResult(Result.Success(dc.Region.StartsWith("us") ? 20 : 100));
        }

        return Task.FromResult(Result.Success(50));
    }

    public Task<Result<List<CloudSaveState>>> GetCloudSaveStatesAsync(string providerId, string gameId)
    {
        var key = $"{providerId}:{gameId}";
        if (!_saveStates.TryGetValue(key, out var states))
        {
            states = new List<CloudSaveState>
            {
                new()
                {
                    Id = "save_1",
                    GameId = gameId,
                    CreatedAt = DateTime.UtcNow.AddHours(-1),
                    SizeBytes = 1024 * 1024,
                    Description = "Auto-save"
                },
                new()
                {
                    Id = "save_2",
                    GameId = gameId,
                    CreatedAt = DateTime.UtcNow.AddHours(-2),
                    SizeBytes = 1024 * 1024,
                    Description = "Manual save"
                }
            };
            _saveStates[key] = states;
        }

        return Task.FromResult(Result.Success(states));
    }

    public Task<Result<bool>> SyncSaveStateToCloudAsync(string providerId, string gameId, string saveData)
    {
        return Task.FromResult(Result.Success(true));
    }

    public Task<Result<string>> DownloadSaveStateFromCloudAsync(string providerId, string gameId, string saveStateId)
    {
        return Task.FromResult(Result.Success("downloaded_save_state_data"));
    }

    public Task<Result<bool>> DeleteCloudSaveStateAsync(string providerId, string gameId, string saveStateId)
    {
        return Task.FromResult(Result.Success(true));
    }

    public Task<Result<List<InputMapping>>> GetInputMappingsAsync(string providerId)
    {
        if (_inputMappings.TryGetValue(providerId, out var mappings))
        {
            return Task.FromResult(Result.Success(mappings));
        }

        return Task.FromResult(Result.Success(new List<InputMapping>()));
    }

    public Task<Result<bool>> UpdateInputMappingAsync(string providerId, InputMapping mapping)
    {
        if (!_inputMappings.TryGetValue(providerId, out var mappings))
        {
            mappings = new List<InputMapping>();
            _inputMappings[providerId] = mappings;
        }

        var existing = mappings.FirstOrDefault(m => m.Action == mapping.Action);
        if (existing != null)
        {
            mappings.Remove(existing);
        }

        mappings.Add(mapping);
        return Task.FromResult(Result.Success(true));
    }

    public Task<Result<bool>> ResetInputMappingsAsync(string providerId)
    {
        _inputMappings[providerId] = new List<InputMapping>
        {
            new() { Action = "Jump", Key = "Space", AltKey = "GamepadA" },
            new() { Action = "Crouch", Key = "LeftControl", AltKey = "GamepadB" }
        };

        return Task.FromResult(Result.Success(true));
    }

    private class ProviderConnection
    {
        public string ProviderId { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public DateTime ConnectedAt { get; set; }
    }
}
