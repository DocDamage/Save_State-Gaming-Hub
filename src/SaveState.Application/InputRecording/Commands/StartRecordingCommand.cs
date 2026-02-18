using MediatR;
using SaveState.Core.Common;
using SaveState.Core.InputRecording;
using SaveState.Core.InputRecording.Services;

namespace SaveState.Application.InputRecording.Commands;

/// <summary>
/// Command to start a new input recording.
/// </summary>
public sealed record StartRecordingCommand(
    Guid GameId,
    string Name,
    string? Description = null,
    RecordingType Type = RecordingType.Gameplay,
    InputDeviceType DeviceType = InputDeviceType.Keyboard,
    int Fps = 60,
    string? StartingState = null,
    string? RomHash = null,
    string? EmulatorCore = null,
    List<string>? Tags = null) : IRequest<Result<RecordingSession>>;

/// <summary>
/// Handler for StartRecordingCommand.
/// </summary>
public sealed class StartRecordingCommandHandler : IRequestHandler<StartRecordingCommand, Result<RecordingSession>>
{
    private readonly IInputRecordingService _recordingService;

    public StartRecordingCommandHandler(IInputRecordingService recordingService)
    {
        _recordingService = recordingService;
    }

    public async Task<Result<RecordingSession>> Handle(StartRecordingCommand request, CancellationToken cancellationToken)
    {
        var startRequest = new StartRecordingRequest
        {
            GameId = request.GameId,
            Name = request.Name,
            Description = request.Description,
            Type = request.Type,
            DeviceType = request.DeviceType,
            Fps = request.Fps,
            StartingState = request.StartingState,
            RomHash = request.RomHash,
            EmulatorCore = request.EmulatorCore,
            Tags = request.Tags
        };

        return await _recordingService.StartRecordingAsync(startRequest, cancellationToken);
    }
}
