using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.Configuration;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.SaveStates.Services;
using SaveState.Core.SaveStates.Services.DTOs;

namespace SaveState.Infrastructure.SaveStates;

/// <summary>
/// Executes one background save-state cloud sync cycle.
/// </summary>
public sealed class SaveStateCloudSyncDaemonProcessor
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<CloudSyncOptions> _options;
    private readonly ITimeProvider _timeProvider;
    private readonly SaveStateCloudSyncMonitor _monitor;
    private readonly ILogger<SaveStateCloudSyncDaemonProcessor> _logger;

    public SaveStateCloudSyncDaemonProcessor(
        IServiceScopeFactory scopeFactory,
        IOptions<CloudSyncOptions> options,
        ITimeProvider timeProvider,
        SaveStateCloudSyncMonitor monitor,
        ILogger<SaveStateCloudSyncDaemonProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _timeProvider = timeProvider;
        _monitor = monitor;
        _logger = logger;
    }

    public async Task ProcessCycleAsync(CancellationToken ct = default)
    {
        var daemonOptions = _options.Value.SaveStateDaemon;
        _monitor.SetEnabled(
            daemonOptions.Enabled,
            daemonOptions.Enabled
                ? "Background save-state sync daemon is enabled."
                : "Background save-state sync daemon is disabled.");

        if (!daemonOptions.Enabled)
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var gameRepository = scope.ServiceProvider.GetRequiredService<IGameRepository>();
        var cloudService = scope.ServiceProvider.GetRequiredService<ISaveStateCloudService>();

        var games = await gameRepository.GetAllAsync(ct).ConfigureAwait(false);
        var candidates = SelectCandidates(games, daemonOptions.MaxGamesPerCycle);
        if (candidates.Count == 0)
        {
            _monitor.RecordHeartbeat("No games available for background save-state sync.");
            return;
        }

        foreach (var game in candidates)
        {
            ct.ThrowIfCancellationRequested();

            var syncResult = await cloudService.SyncSaveStateAsync(
                game.Id,
                new SaveStateCloudMetadata
                {
                    VersionName = $"Auto Sync {_timeProvider.UtcNow:yyyy-MM-dd HH:mm:ss}",
                    DeviceName = Environment.MachineName,
                    ForceUpload = daemonOptions.ForceUploadOnConflict
                },
                ct).ConfigureAwait(false);

            ProcessSyncResult(game, syncResult);
        }
    }

    private void ProcessSyncResult(Game game, Result<SaveStateCloudSyncStatus> syncResult)
    {
        if (syncResult.IsFailure)
        {
            if (syncResult.ErrorType == ErrorType.NotFound)
            {
                _monitor.RecordSkipped(game.Id, $"Skipped {game.Title}: no local save state available.");
                return;
            }

            var error = syncResult.Error ?? $"Background sync failed for {game.Title}.";
            _logger.LogWarning(
                "Background save-state cloud sync failed for game {GameId}: {Error}",
                game.Id,
                error);
            _monitor.RecordSyncFailure(game.Id, error);
            return;
        }

        var status = syncResult.Value;
        if (status is null)
        {
            _monitor.RecordSyncFailure(game.Id, $"Background sync for {game.Title} returned no status.");
            return;
        }

        if (status.HasConflict && status.ConflictType != SaveStateConflictType.None)
        {
            _monitor.RecordConflict(
                game.Id,
                status.ConflictType,
                status.Message ?? $"Conflict detected while syncing {game.Title}.");
            return;
        }

        if (!status.Uploaded && !status.Downloaded)
        {
            _monitor.RecordSkipped(
                game.Id,
                status.Message ?? $"No cloud transfer required for {game.Title}.");
            return;
        }

        _monitor.RecordSyncSuccess(
            game.Id,
            status.Message ?? $"Background save-state sync completed for {game.Title}.");
    }

    private static IReadOnlyList<Game> SelectCandidates(
        IReadOnlyList<Game> games,
        int maxGamesPerCycle)
    {
        var max = maxGamesPerCycle <= 0 ? 1 : maxGamesPerCycle;
        return games
            .OrderByDescending(game => game.LastPlayedAt ?? game.UpdatedAt ?? game.CreatedAt)
            .ThenBy(game => game.Title, StringComparer.OrdinalIgnoreCase)
            .Take(max)
            .ToArray();
    }
}
