namespace SaveState.Application.Mugen.Services.LiveSync.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.LiveSync;

/// <summary>
/// Engine for resolving synchronization conflicts between local and remote data.
/// </summary>
public class ConflictResolutionEngine
{
    private readonly ILogger<ConflictResolutionEngine> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictResolutionEngine"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ConflictResolutionEngine(ILogger<ConflictResolutionEngine> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Resolves a sync conflict using the specified resolution strategy.
    /// </summary>
    /// <param name="conflict">The conflict to resolve.</param>
    /// <param name="resolution">The resolution strategy and data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure of the resolution.</returns>
    public async Task<ConflictResolutionResult> ResolveConflictAsync(
        SyncConflict conflict,
        ConflictResolution resolution,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Resolving conflict {ConflictId} for item {ItemId} using strategy {Strategy}",
            conflict.ConflictId,
            conflict.ItemId,
            resolution.Strategy);

        try
        {
            IReadOnlyDictionary<string, object>? finalData = resolution.Strategy switch
            {
                ResolutionStrategy.UseLocal => ResolveUsingLocal(conflict),
                ResolutionStrategy.UseRemote => ResolveUsingRemote(conflict),
                ResolutionStrategy.Merge => MergeVersions(conflict),
                ResolutionStrategy.Manual => ResolveManually(conflict, resolution),
                _ => throw new NotSupportedException($"Resolution strategy {resolution.Strategy} is not supported")
            };

            var result = new ConflictResolutionResult
            {
                Success = true,
                ResolutionId = GenerateResolutionId(),
                FinalData = finalData,
                ErrorMessage = null
            };

            _logger.LogInformation(
                "Conflict {ConflictId} resolved successfully with strategy {Strategy}. Resolution ID: {ResolutionId}",
                conflict.ConflictId,
                resolution.Strategy,
                result.ResolutionId);

            if (!string.IsNullOrWhiteSpace(resolution.ResolutionNotes))
            {
                _logger.LogDebug("Resolution notes for conflict {ConflictId}: {Notes}",
                    conflict.ConflictId,
                    resolution.ResolutionNotes);
            }

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to resolve conflict {ConflictId} using strategy {Strategy}",
                conflict.ConflictId,
                resolution.Strategy);

            return await Task.FromResult(new ConflictResolutionResult
            {
                Success = false,
                ResolutionId = null,
                FinalData = null,
                ErrorMessage = ex.Message
            });
        }
    }

    /// <summary>
    /// Resolves the conflict by using the local version.
    /// </summary>
    /// <param name="conflict">The conflict to resolve.</param>
    /// <returns>The local version data.</returns>
    private static IReadOnlyDictionary<string, object> ResolveUsingLocal(SyncConflict conflict)
    {
        return conflict.LocalVersion;
    }

    /// <summary>
    /// Resolves the conflict by using the remote version.
    /// </summary>
    /// <param name="conflict">The conflict to resolve.</param>
    /// <returns>The remote version data.</returns>
    private static IReadOnlyDictionary<string, object> ResolveUsingRemote(SyncConflict conflict)
    {
        return conflict.RemoteVersion;
    }

    /// <summary>
    /// Merges the local and remote versions into a single unified dataset.
    /// </summary>
    /// <param name="conflict">The conflict to merge.</param>
    /// <returns>The merged data.</returns>
    private static IReadOnlyDictionary<string, object> MergeVersions(SyncConflict conflict)
    {
        var merged = new Dictionary<string, object>(conflict.RemoteVersion);

        foreach (var kvp in conflict.LocalVersion)
        {
            // Local version takes precedence for conflicting keys
            merged[kvp.Key] = kvp.Value;
        }

        return merged;
    }

    /// <summary>
    /// Resolves the conflict using manually provided data.
    /// </summary>
    /// <param name="conflict">The conflict to resolve.</param>
    /// <param name="resolution">The resolution containing manual data.</param>
    /// <returns>The manually resolved data.</returns>
    /// <exception cref="InvalidOperationException">Thrown when manual resolution data is not provided.</exception>
    private static IReadOnlyDictionary<string, object> ResolveManually(
        SyncConflict conflict,
        ConflictResolution resolution)
    {
        if (resolution.ResolvedData is null || resolution.ResolvedData.Count == 0)
        {
            throw new InvalidOperationException(
                "Manual resolution requires ResolvedData to be provided.");
        }

        return resolution.ResolvedData;
    }

    /// <summary>
    /// Generates a unique resolution identifier.
    /// </summary>
    /// <returns>A unique resolution ID string.</returns>
    private static string GenerateResolutionId()
    {
        return $"RES-{Guid.NewGuid():N}";
    }
}
