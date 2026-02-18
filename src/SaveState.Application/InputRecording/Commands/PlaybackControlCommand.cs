using MediatR;
using SaveState.Core.Common;
using SaveState.Core.InputRecording;
using SaveState.Core.InputRecording.Services;

namespace SaveState.Application.InputRecording.Commands;

/// <summary>
/// Command to start playback.
/// </summary>
public sealed record StartPlaybackCommand(
    Guid RecordingId,
    PlaybackSpeed Speed = PlaybackSpeed.Normal,
    long StartFrame = 0,
    bool Loop = false) : IRequest<Result<PlaybackSession>>;

/// <summary>
/// Handler for StartPlaybackCommand.
/// </summary>
public sealed class StartPlaybackCommandHandler : IRequestHandler<StartPlaybackCommand, Result<PlaybackSession>>
{
    private readonly IInputRecordingService _recordingService;

    public StartPlaybackCommandHandler(IInputRecordingService recordingService)
    {
        _recordingService = recordingService;
    }

    public async Task<Result<PlaybackSession>> Handle(StartPlaybackCommand request, CancellationToken cancellationToken)
    {
        var startRequest = new StartPlaybackRequest
        {
            RecordingId = request.RecordingId,
            Speed = request.Speed,
            StartFrame = request.StartFrame,
            Loop = request.Loop
        };

        return await _recordingService.StartPlaybackAsync(startRequest, cancellationToken);
    }
}

/// <summary>
/// Command to stop playback.
/// </summary>
public sealed record StopPlaybackCommand(Guid SessionId) : IRequest<Result>;

/// <summary>
/// Handler for StopPlaybackCommand.
/// </summary>
public sealed class StopPlaybackCommandHandler : IRequestHandler<StopPlaybackCommand, Result>
{
    private readonly IInputRecordingService _recordingService;

    public StopPlaybackCommandHandler(IInputRecordingService recordingService)
    {
        _recordingService = recordingService;
    }

    public async Task<Result> Handle(StopPlaybackCommand request, CancellationToken cancellationToken)
    {
        return await _recordingService.StopPlaybackAsync(request.SessionId, cancellationToken);
    }
}

/// <summary>
/// Command to pause playback.
/// </summary>
public sealed record PausePlaybackCommand(Guid SessionId) : IRequest<Result>;

/// <summary>
/// Handler for PausePlaybackCommand.
/// </summary>
public sealed class PausePlaybackCommandHandler : IRequestHandler<PausePlaybackCommand, Result>
{
    private readonly IInputRecordingService _recordingService;

    public PausePlaybackCommandHandler(IInputRecordingService recordingService)
    {
        _recordingService = recordingService;
    }

    public async Task<Result> Handle(PausePlaybackCommand request, CancellationToken cancellationToken)
    {
        return await _recordingService.PausePlaybackAsync(request.SessionId, cancellationToken);
    }
}

/// <summary>
/// Command to resume playback.
/// </summary>
public sealed record ResumePlaybackCommand(Guid SessionId) : IRequest<Result>;

/// <summary>
/// Handler for ResumePlaybackCommand.
/// </summary>
public sealed class ResumePlaybackCommandHandler : IRequestHandler<ResumePlaybackCommand, Result>
{
    private readonly IInputRecordingService _recordingService;

    public ResumePlaybackCommandHandler(IInputRecordingService recordingService)
    {
        _recordingService = recordingService;
    }

    public async Task<Result> Handle(ResumePlaybackCommand request, CancellationToken cancellationToken)
    {
        return await _recordingService.ResumePlaybackAsync(request.SessionId, cancellationToken);
    }
}

/// <summary>
/// Command to advance one frame (frame-stepping).
/// </summary>
public sealed record AdvanceFrameCommand(Guid SessionId) : IRequest<Result<InputFrame>>;

/// <summary>
/// Handler for AdvanceFrameCommand.
/// </summary>
public sealed class AdvanceFrameCommandHandler : IRequestHandler<AdvanceFrameCommand, Result<InputFrame>>
{
    private readonly IInputRecordingService _recordingService;

    public AdvanceFrameCommandHandler(IInputRecordingService recordingService)
    {
        _recordingService = recordingService;
    }

    public async Task<Result<InputFrame>> Handle(AdvanceFrameCommand request, CancellationToken cancellationToken)
    {
        return await _recordingService.AdvanceFrameAsync(request.SessionId, cancellationToken);
    }
}

/// <summary>
/// Command to rewind playback.
/// </summary>
public sealed record RewindCommand(Guid SessionId, long FrameCount) : IRequest<Result>;

/// <summary>
/// Handler for RewindCommand.
/// </summary>
public sealed class RewindCommandHandler : IRequestHandler<RewindCommand, Result>
{
    private readonly IInputRecordingService _recordingService;

    public RewindCommandHandler(IInputRecordingService recordingService)
    {
        _recordingService = recordingService;
    }

    public async Task<Result> Handle(RewindCommand request, CancellationToken cancellationToken)
    {
        return await _recordingService.RewindAsync(request.SessionId, request.FrameCount, cancellationToken);
    }
}

/// <summary>
/// Command to set playback speed.
/// </summary>
public sealed record SetPlaybackSpeedCommand(Guid SessionId, PlaybackSpeed Speed) : IRequest<Result>;

/// <summary>
/// Handler for SetPlaybackSpeedCommand.
/// </summary>
public sealed class SetPlaybackSpeedCommandHandler : IRequestHandler<SetPlaybackSpeedCommand, Result>
{
    private readonly IInputRecordingService _recordingService;

    public SetPlaybackSpeedCommandHandler(IInputRecordingService recordingService)
    {
        _recordingService = recordingService;
    }

    public async Task<Result> Handle(SetPlaybackSpeedCommand request, CancellationToken cancellationToken)
    {
        return await _recordingService.SetPlaybackSpeedAsync(request.SessionId, request.Speed, cancellationToken);
    }
}

/// <summary>
/// Command to seek to a specific frame.
/// </summary>
public sealed record SeekToFrameCommand(Guid SessionId, long FrameNumber) : IRequest<Result>;

/// <summary>
/// Handler for SeekToFrameCommand.
/// </summary>
public sealed class SeekToFrameCommandHandler : IRequestHandler<SeekToFrameCommand, Result>
{
    private readonly IInputRecordingService _recordingService;

    public SeekToFrameCommandHandler(IInputRecordingService recordingService)
    {
        _recordingService = recordingService;
    }

    public async Task<Result> Handle(SeekToFrameCommand request, CancellationToken cancellationToken)
    {
        return await _recordingService.SeekToFrameAsync(request.SessionId, request.FrameNumber, cancellationToken);
    }
}
