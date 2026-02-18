using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services.AutomatedBalancing.Engines;

/// <summary>
/// Monitors game state for balance tracking.
/// </summary>
public class GameStateMonitor
{
    private readonly ILogger<GameStateMonitor> _logger;

    public GameStateMonitor(ILogger<GameStateMonitor> logger)
    {
        _logger = logger;
    }

    public async Task<GameStateSnapshot> CaptureSnapshotAsync(string gameId, CancellationToken ct = default)
    {
        _logger.LogInformation("Capturing game state for {GameId}", gameId);

        return new GameStateSnapshot
        {
            GameId = gameId,
            CapturedAt = DateTime.UtcNow,
            ActiveCharacters = Array.Empty<string>(),
            MatchCount = 0,
            AverageMatchDuration = TimeSpan.Zero
        };
    }

    public async Task<IReadOnlyList<GameStateSnapshot>> GetHistoryAsync(string gameId, TimeSpan period, CancellationToken ct = default)
    {
        _logger.LogInformation("Getting game state history for {GameId}", gameId);
        return await Task.FromResult(Array.Empty<GameStateSnapshot>());
    }
}

/// <summary>
/// Snapshot of game state.
/// </summary>
public class GameStateSnapshot
{
    public string GameId { get; set; } = default!;
    public DateTime CapturedAt { get; set; }
    public IReadOnlyList<string> ActiveCharacters { get; set; } = Array.Empty<string>();
    public int MatchCount { get; set; }
    public TimeSpan AverageMatchDuration { get; set; }
}