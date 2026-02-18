namespace SaveState.Application.Mugen.Services.NetworkFeatures.Engines;

using System.Collections.Concurrent;
using SaveState.Application.Mugen.Models.NetworkFeatures;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using Microsoft.Extensions.Logging;

/// <summary>
/// Engine for managing game lobbies.
/// </summary>
public class LobbyEngine
{
    private readonly ILogger<LobbyEngine> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, Lobby> _lobbies = new();
    private readonly Random _random = new();
    private const string LobbyCodeChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int LobbyCodeSegmentLength = 3;

    /// <summary>
    /// Initializes a new instance of the <see cref="LobbyEngine"/> class.
    /// </summary>
    public LobbyEngine(ILogger<LobbyEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Creates a new lobby with the specified configuration.
    /// </summary>
    /// <param name="configuration">The lobby configuration.</param>
    /// <param name="hostPlayerId">The ID of the host player.</param>
    /// <returns>A result containing the created lobby or an error message.</returns>
    public Result<Lobby> CreateLobby(LobbyConfiguration configuration, string hostPlayerId)
    {
        if (configuration is null)
        {
            return Result.Failure<Lobby>("Lobby configuration is required.", ErrorType.Validation);
        }

        if (string.IsNullOrWhiteSpace(hostPlayerId))
        {
            return Result.Failure<Lobby>("Host player ID is required.", ErrorType.Validation);
        }

        // Validate lobby name
        if (string.IsNullOrWhiteSpace(configuration.Name))
        {
            return Result.Failure<Lobby>("Lobby name is required.", ErrorType.Validation);
        }

        if (configuration.Name.Length > 100)
        {
            return Result.Failure<Lobby>("Lobby name must not exceed 100 characters.", ErrorType.Validation);
        }

        // Validate max players (2-64 range)
        if (configuration.MaxPlayers < 2 || configuration.MaxPlayers > 64)
        {
            return Result.Failure<Lobby>("Max players must be between 2 and 64.", ErrorType.Validation);
        }

        // Validate game mode
        if (string.IsNullOrWhiteSpace(configuration.GameMode))
        {
            return Result.Failure<Lobby>("Game mode is required.", ErrorType.Validation);
        }

        // Validate region
        if (string.IsNullOrWhiteSpace(configuration.Region))
        {
            return Result.Failure<Lobby>("Region is required.", ErrorType.Validation);
        }

        // Generate unique lobby code
        string lobbyCode;
        int maxAttempts = 100;
        int attempts = 0;

        do
        {
            lobbyCode = GenerateLobbyCode();
            attempts++;
        }
        while (_lobbies.Values.Any(l => l.Code == lobbyCode) && attempts < maxAttempts);

        if (attempts >= maxAttempts)
        {
            _logger.LogError("Failed to generate unique lobby code after {MaxAttempts} attempts", maxAttempts);
            return Result.Failure<Lobby>("Unable to create lobby. Please try again.", ErrorType.Internal);
        }

        // Create the lobby
        var lobby = new Lobby
        {
            Id = Guid.NewGuid().ToString("N"),
            Code = lobbyCode,
            Name = configuration.Name.Trim(),
            HostPlayerId = hostPlayerId,
            HostName = configuration.Name.Trim(), // Will be updated with actual player name
            MaxPlayers = configuration.MaxPlayers,
            GameMode = configuration.GameMode.Trim(),
            Region = configuration.Region.Trim(),
            IsPrivate = configuration.IsPrivate,
            PasswordHash = !string.IsNullOrEmpty(configuration.Password) 
                ? HashPassword(configuration.Password) 
                : null,
            AllowSpectators = configuration.AllowSpectators,
            Status = LobbyStatus.Waiting,
            Players = new List<LobbyPlayer>(),
            CreatedAt = _timeProvider.UtcNow,
            CustomSettings = configuration.CustomSettings ?? new Dictionary<string, string>()
        };

        // Add host as first player
        lobby.Players.Add(new LobbyPlayer(
            hostPlayerId,
            "Host", // Will be updated with actual player name
            string.Empty,
            false,
            true));

        // Store the lobby
        if (!_lobbies.TryAdd(lobby.Id, lobby))
        {
            return Result.Failure<Lobby>("Failed to create lobby. Please try again.", ErrorType.Internal);
        }

        _logger.LogInformation(
            "Created lobby {LobbyId} with code {LobbyCode} for host {HostPlayerId}",
            lobby.Id,
            lobby.Code,
            hostPlayerId);

        return Result.Success(lobby);
    }

    /// <summary>
    /// Validates whether a player can join a lobby.
    /// </summary>
    /// <param name="lobbyId">The ID of the lobby to join.</param>
    /// <param name="playerId">The ID of the player attempting to join.</param>
    /// <param name="password">The password for private lobbies.</param>
    /// <returns>A result containing whether the player can join and an error message if applicable.</returns>
    public Result<(bool CanJoin, string Error)> ValidateLobbyJoin(string lobbyId, string playerId, string? password)
    {
        if (string.IsNullOrWhiteSpace(lobbyId))
        {
            return Result.Failure<(bool CanJoin, string Error)>(
                "Lobby ID is required.",
                ErrorType.Validation);
        }

        if (string.IsNullOrWhiteSpace(playerId))
        {
            return Result.Failure<(bool CanJoin, string Error)>(
                "Player ID is required.",
                ErrorType.Validation);
        }

        if (!_lobbies.TryGetValue(lobbyId, out var lobby))
        {
            return Result.Success((false, "Lobby not found."));
        }

        // Check if lobby is full
        if (lobby.IsFull)
        {
            return Result.Success((false, "Lobby is full."));
        }

        // Check if player is already in the lobby
        if (lobby.Players.Any(p => p.PlayerId == playerId))
        {
            return Result.Success((false, "Player is already in this lobby."));
        }

        // Check lobby status
        if (lobby.Status != LobbyStatus.Waiting)
        {
            return Result.Success((false, $"Lobby is {lobby.Status.ToString().ToLowerInvariant()}."));
        }

        // Check password for private lobbies
        if (lobby.HasPassword)
        {
            if (string.IsNullOrEmpty(password))
            {
                return Result.Success((false, "Password is required for this lobby."));
            }

            if (!VerifyPassword(password, lobby.PasswordHash!))
            {
                return Result.Success((false, "Incorrect password."));
            }
        }

        return Result.Success((true, string.Empty));
    }

    /// <summary>
    /// Filters lobbies based on the specified criteria.
    /// </summary>
    /// <param name="filter">The filter criteria.</param>
    /// <returns>A read-only list of lobbies matching the filter.</returns>
    public IReadOnlyList<Lobby> FilterLobbies(LobbyFilter filter)
    {
        if (filter is null)
        {
            return _lobbies.Values.ToList().AsReadOnly();
        }

        var query = _lobbies.Values.AsEnumerable();

        // Filter by game mode
        if (!string.IsNullOrWhiteSpace(filter.GameMode))
        {
            query = query.Where(l => l.GameMode.Equals(filter.GameMode, StringComparison.OrdinalIgnoreCase));
        }

        // Filter by region
        if (!string.IsNullOrWhiteSpace(filter.Region))
        {
            query = query.Where(l => l.Region.Equals(filter.Region, StringComparison.OrdinalIgnoreCase));
        }

        // Filter private lobbies
        if (filter.PrivateOnly.HasValue)
        {
            if (filter.PrivateOnly.Value)
            {
                query = query.Where(l => l.IsPrivate);
            }
            else
            {
                query = query.Where(l => !l.IsPrivate);
            }
        }

        // Hide full lobbies
        if (filter.HideFull == true)
        {
            query = query.Where(l => !l.IsFull);
        }

        // Hide password-protected lobbies
        if (filter.HidePasswordProtected == true)
        {
            query = query.Where(l => !l.HasPassword);
        }

        // Filter by minimum player count (current players)
        if (filter.MinPlayers.HasValue)
        {
            query = query.Where(l => l.CurrentPlayerCount >= filter.MinPlayers.Value);
        }

        // Filter by maximum player count (lobby capacity)
        if (filter.MaxPlayers.HasValue)
        {
            query = query.Where(l => l.MaxPlayers <= filter.MaxPlayers.Value);
        }

        // Only show waiting lobbies
        query = query.Where(l => l.Status == LobbyStatus.Waiting);

        return query.OrderByDescending(l => l.CreatedAt).ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets a lobby by its ID.
    /// </summary>
    /// <param name="lobbyId">The lobby ID.</param>
    /// <returns>The lobby if found; otherwise null.</returns>
    public Lobby? GetLobbyById(string lobbyId)
    {
        _lobbies.TryGetValue(lobbyId, out var lobby);
        return lobby;
    }

    /// <summary>
    /// Gets a lobby by its code.
    /// </summary>
    /// <param name="lobbyCode">The lobby code.</param>
    /// <returns>The lobby if found; otherwise null.</returns>
    public Lobby? GetLobbyByCode(string lobbyCode)
    {
        return _lobbies.Values.FirstOrDefault(l => 
            l.Code.Equals(lobbyCode, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Removes a lobby by its ID.
    /// </summary>
    /// <param name="lobbyId">The lobby ID.</param>
    /// <returns>True if the lobby was removed; otherwise false.</returns>
    public bool RemoveLobby(string lobbyId)
    {
        if (_lobbies.TryRemove(lobbyId, out _))
        {
            _logger.LogInformation("Removed lobby {LobbyId}", lobbyId);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets all active lobbies.
    /// </summary>
    /// <returns>A read-only list of all lobbies.</returns>
    public IReadOnlyList<Lobby> GetAllLobbies()
    {
        return _lobbies.Values.ToList().AsReadOnly();
    }

    private string GenerateLobbyCode()
    {
        // Generate readable lobby ID format: ABC-123-XYZ
        var segments = new string[3];
        
        for (int i = 0; i < 3; i++)
        {
            var chars = new char[LobbyCodeSegmentLength];
            for (int j = 0; j < LobbyCodeSegmentLength; j++)
            {
                chars[j] = LobbyCodeChars[_random.Next(LobbyCodeChars.Length)];
            }
            segments[i] = new string(chars);
        }

        return string.Join("-", segments);
    }

    private static string HashPassword(string password)
    {
        // Simple hash for demonstration - in production, use proper password hashing
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));
    }

    private static bool VerifyPassword(string password, string passwordHash)
    {
        return HashPassword(password) == passwordHash;
    }
}
