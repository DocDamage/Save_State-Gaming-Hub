using System.Collections.Concurrent;
using SaveState.Core.Common;
using SaveState.Core.Netplay.Services;
using Microsoft.Extensions.Logging;

namespace SaveState.Infrastructure.Netplay.Services;

public class SpectatorRelayService : ISpectatorRelayService
{
    private readonly ILogger<SpectatorRelayService> _logger;
    private readonly ConcurrentDictionary<string, HashSet<string>> _spectators;
    private readonly ConcurrentDictionary<string, bool> _activeRelays;

    public SpectatorRelayService(ILogger<SpectatorRelayService> logger)
    {
        _logger = logger;
        _spectators = new ConcurrentDictionary<string, HashSet<string>>();
        _activeRelays = new ConcurrentDictionary<string, bool>();
    }

    public Task<Result> StartRelayAsync(string sessionId, CancellationToken ct = default)
    {
        _activeRelays[sessionId] = true;
        _spectators[sessionId] = new HashSet<string>();
        _logger.LogInformation("Started spectator relay for session {SessionId}", sessionId);
        return Task.FromResult(Result.Success());
    }

    public Task<Result> StopRelayAsync(string sessionId, CancellationToken ct = default)
    {
        _activeRelays.TryRemove(sessionId, out _);
        _spectators.TryRemove(sessionId, out _);
        _logger.LogInformation("Stopped spectator relay for session {SessionId}", sessionId);
        return Task.FromResult(Result.Success());
    }

    public Task<Result> AddSpectatorAsync(string sessionId, string spectatorId, CancellationToken ct = default)
    {
        if (!_activeRelays.ContainsKey(sessionId))
        {
            return Task.FromResult(Result.Failure("Relay not active for session", ErrorType.NotFound));
        }

        var spectators = _spectators.GetOrAdd(sessionId, _ => new HashSet<string>());
        lock (spectators)
        {
            spectators.Add(spectatorId);
        }

        _logger.LogInformation("Added spectator {SpectatorId} to session {SessionId}", spectatorId, sessionId);
        return Task.FromResult(Result.Success());
    }

    public Task<Result> RemoveSpectatorAsync(string sessionId, string spectatorId, CancellationToken ct = default)
    {
        if (_spectators.TryGetValue(sessionId, out var spectators))
        {
            lock (spectators)
            {
                spectators.Remove(spectatorId);
            }
            _logger.LogInformation("Removed spectator {SpectatorId} from session {SessionId}", spectatorId, sessionId);
        }

        return Task.FromResult(Result.Success());
    }

    public Task<Result<int>> GetSpectatorCountAsync(string sessionId, CancellationToken ct = default)
    {
        if (_spectators.TryGetValue(sessionId, out var spectators))
        {
            lock (spectators)
            {
                return Task.FromResult(Result<int>.Success(spectators.Count));
            }
        }

        return Task.FromResult(Result<int>.Success(0));
    }

    public Task<Result> BroadcastFrameAsync(string sessionId, byte[] frameData, CancellationToken ct = default)
    {
        if (!_activeRelays.ContainsKey(sessionId))
        {
            return Task.FromResult(Result.Failure("Relay not active for session", ErrorType.NotFound));
        }

        // In a real implementation, this would broadcast to all connected spectators
        _logger.LogDebug("Broadcasting frame to session {SessionId} ({ByteCount} bytes)", sessionId, frameData.Length);
        return Task.FromResult(Result.Success());
    }
}
