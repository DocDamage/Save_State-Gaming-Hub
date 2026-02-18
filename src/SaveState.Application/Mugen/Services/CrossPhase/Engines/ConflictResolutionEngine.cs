namespace SaveState.Application.Mugen.Services.CrossPhase.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.CrossPhase;
using SaveState.Core.Common.Services;

public class ConflictResolutionEngine
{
    private readonly ILogger<ConflictResolutionEngine> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly IntegrationEngine _integrationEngine;

    public ConflictResolutionEngine(ILogger<ConflictResolutionEngine> logger, ITimeProvider timeProvider, IntegrationEngine integrationEngine)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _integrationEngine = integrationEngine;
    }

    public Task<MechanicConflictResolution> ResolveMechanicConflictsAsync(
        string sessionId,
        IReadOnlyList<MechanicConflict> conflicts,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Resolving {ConflictCount} mechanic conflicts for session {SessionId}",
            conflicts.Count, sessionId);

        var resolutions = new List<ConflictResolution>();
        var successfulResolutions = 0;

        foreach (var conflict in conflicts)
        {
            var resolution = ResolveConflict(conflict);
            resolutions.Add(resolution);

            if (resolution.Success)
            {
                successfulResolutions++;
            }
        }

        var result = new MechanicConflictResolution
        {
            SessionId = sessionId,
            ConflictsResolved = conflicts.Count,
            SuccessfulResolutions = successfulResolutions,
            Resolutions = resolutions,
            ResolutionTimestamp = _timeProvider.UtcNow
        };

        return Task.FromResult(result);
    }

    private ConflictResolution ResolveConflict(MechanicConflict conflict)
    {
        var success = conflict.Severity < 0.7f;

        return new ConflictResolution
        {
            ConflictId = conflict.ConflictId,
            ResolutionType = success ? "Automatic" : "Manual",
            Success = success,
            AppliedAt = _timeProvider.UtcNow
        };
    }
}
