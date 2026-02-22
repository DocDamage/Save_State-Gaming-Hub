using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Sequence director engine - orchestrates camera sequences, timing, and event triggers.
/// </summary>
public class SequenceDirectorEngine
{
    private readonly ILogger<SequenceDirectorEngine> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, CinematicCameraSystemCameraSequence> _sequences = new();
    private readonly Dictionary<string, CancellationTokenSource> _activePlays = new();

    public SequenceDirectorEngine(ILogger<SequenceDirectorEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public CinematicCameraSystemCameraSequence CreateSequence(CinematicCameraSystemCameraSequenceRequest request)
    {
        var sequence = new CinematicCameraSystemCameraSequence
        {
            SequenceId = Guid.NewGuid().ToString(),
            Name = request.Name,
            Description = request.Description,
            Movements = request.Movements,
            Transitions = request.Transitions,
            Events = request.Events,
            CameraSettings = request.CameraSettings,
            IsLooping = request.IsLooping,
            CreatedAt = _timeProvider.UtcNow
        };
        _sequences[sequence.SequenceId] = sequence;
        _logger.LogInformation("Created sequence {SequenceId}: {Name}", sequence.SequenceId, sequence.Name);
        return sequence;
    }

    public async Task PlaySequenceAsync(
        string sequenceId,
        Func<CinematicCameraSystemCameraMovement, Task> onMovement,
        Func<CinematicCameraSystemCinematicEvent, Task> onEvent,
        CancellationToken ct)
    {
        if (!_sequences.TryGetValue(sequenceId, out var sequence))
        {
            _logger.LogWarning("Sequence {SequenceId} not found", sequenceId);
            return;
        }

        var cts = new CancellationTokenSource();
        _activePlays[sequenceId] = cts;
        sequence.IsPlaying = true;

        try
        {
            var context = new CinematicCameraSystemCameraContext
            {
                SequenceId = sequenceId,
                CurrentTime = TimeSpan.Zero,
                PendingEvents = new List<CinematicCameraSystemCinematicEvent>(sequence.Events)
            };

            foreach (var movement in sequence.Movements)
            {
                if (cts.Token.IsCancellationRequested) break;
                await onMovement(movement);
                await Task.Delay(movement.Duration, cts.Token);
                context.CurrentTime += movement.Duration;
                await CheckAndTriggerEventsAsync(context, onEvent, cts.Token);
            }
        }
        finally
        {
            sequence.IsPlaying = false;
            _activePlays.Remove(sequenceId);
        }
    }

    public void StopSequence(string sequenceId)
    {
        if (_activePlays.TryGetValue(sequenceId, out var cts))
        {
            cts.Cancel();
            _logger.LogInformation("Stopped sequence {SequenceId}", sequenceId);
        }
    }

    private async Task CheckAndTriggerEventsAsync(
        CinematicCameraSystemCameraContext context,
        Func<CinematicCameraSystemCinematicEvent, Task> onEvent,
        CancellationToken ct)
    {
        var eventsToTrigger = context.PendingEvents
            .Where(e => e.TriggerTime <= context.CurrentTime && !e.HasTriggered)
            .ToList();

        foreach (var evt in eventsToTrigger)
        {
            evt.HasTriggered = true;
            await onEvent(evt);
        }
    }

    public CinematicCameraSystemCameraSequence? GetSequence(string sequenceId)
    {
        return _sequences.TryGetValue(sequenceId, out var sequence) ? sequence : null;
    }
}
