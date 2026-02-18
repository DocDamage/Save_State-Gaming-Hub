using MediatR;
using SaveState.Core.Common;
using SaveState.Core.InputRecording;
using SaveState.Core.InputRecording.Services;
using InputRecordingEntity = SaveState.Core.InputRecording.InputRecording;

namespace SaveState.Application.InputRecording.Commands;

/// <summary>
/// Command to stop recording and save.
/// </summary>
public sealed record StopRecordingCommand(Guid SessionId) : IRequest<Result<InputRecordingEntity>>;

/// <summary>
/// Handler for StopRecordingCommand.
/// </summary>
public sealed class StopRecordingCommandHandler : IRequestHandler<StopRecordingCommand, Result<InputRecordingEntity>>
{
    private readonly IInputRecordingService _recordingService;

    public StopRecordingCommandHandler(IInputRecordingService recordingService)
    {
        _recordingService = recordingService;
    }

    public async Task<Result<InputRecordingEntity>> Handle(StopRecordingCommand request, CancellationToken cancellationToken)
    {
        return await _recordingService.StopRecordingAsync(request.SessionId, cancellationToken);
    }
}

/// <summary>
/// Command to pause recording.
/// </summary>
public sealed record PauseRecordingCommand(Guid SessionId) : IRequest<Result>;

/// <summary>
/// Handler for PauseRecordingCommand.
/// </summary>
public sealed class PauseRecordingCommandHandler : IRequestHandler<PauseRecordingCommand, Result>
{
    private readonly IInputRecordingService _recordingService;

    public PauseRecordingCommandHandler(IInputRecordingService recordingService)
    {
        _recordingService = recordingService;
    }

    public async Task<Result> Handle(PauseRecordingCommand request, CancellationToken cancellationToken)
    {
        return await _recordingService.PauseRecordingAsync(request.SessionId, cancellationToken);
    }
}

/// <summary>
/// Command to resume recording.
/// </summary>
public sealed record ResumeRecordingCommand(Guid SessionId) : IRequest<Result>;

/// <summary>
/// Handler for ResumeRecordingCommand.
/// </summary>
public sealed class ResumeRecordingCommandHandler : IRequestHandler<ResumeRecordingCommand, Result>
{
    private readonly IInputRecordingService _recordingService;

    public ResumeRecordingCommandHandler(IInputRecordingService recordingService)
    {
        _recordingService = recordingService;
    }

    public async Task<Result> Handle(ResumeRecordingCommand request, CancellationToken cancellationToken)
    {
        return await _recordingService.ResumeRecordingAsync(request.SessionId, cancellationToken);
    }
}

/// <summary>
/// Command to record a single frame.
/// </summary>
public sealed record RecordFrameCommand(Guid SessionId, InputFrame Frame) : IRequest<Result>;

/// <summary>
/// Handler for RecordFrameCommand.
/// </summary>
public sealed class RecordFrameCommandHandler : IRequestHandler<RecordFrameCommand, Result>
{
    private readonly IInputRecordingService _recordingService;

    public RecordFrameCommandHandler(IInputRecordingService recordingService)
    {
        _recordingService = recordingService;
    }

    public async Task<Result> Handle(RecordFrameCommand request, CancellationToken cancellationToken)
    {
        return await _recordingService.RecordFrameAsync(request.SessionId, request.Frame, cancellationToken);
    }
}
