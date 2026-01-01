using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Performance.Services;

namespace SaveState.Application.Performance.Commands;

/// <summary>
/// Command to create an audio profile for a game.
/// </summary>
public sealed record CreateAudioProfileCommand(
    Guid GameId,
    int SampleRate = 48000,
    int BitDepth = 24,
    int BufferSize = 480,
    bool ExclusiveMode = false,
    bool SpatialAudio = false,
    AudioLatencyMode LatencyMode = AudioLatencyMode.Default) : IRequest<Result<AudioProfile>>;

/// <summary>
/// Handler for CreateAudioProfileCommand.
/// </summary>
public sealed class CreateAudioProfileCommandHandler : IRequestHandler<CreateAudioProfileCommand, Result<AudioProfile>>
{
    private readonly IAudioOptimizer _audioOptimizer;

    public CreateAudioProfileCommandHandler(IAudioOptimizer audioOptimizer)
    {
        _audioOptimizer = audioOptimizer;
    }

    public async Task<Result<AudioProfile>> Handle(CreateAudioProfileCommand request, CancellationToken cancellationToken)
    {
        var settings = new AudioSettings(
            SampleRate: request.SampleRate,
            BitDepth: request.BitDepth,
            BufferSize: request.BufferSize,
            Channels: 2,
            ExclusiveMode: request.ExclusiveMode,
            SpatialAudio: request.SpatialAudio,
            LatencyMode: request.LatencyMode);

        return await _audioOptimizer.CreateGameProfileAsync(request.GameId, settings, cancellationToken);
    }
}

/// <summary>
/// Command to apply an audio profile.
/// </summary>
public sealed record ApplyAudioProfileCommand(Guid ProfileId) : IRequest<Result>;

/// <summary>
/// Handler for ApplyAudioProfileCommand.
/// </summary>
public sealed class ApplyAudioProfileCommandHandler : IRequestHandler<ApplyAudioProfileCommand, Result>
{
    private readonly IAudioOptimizer _audioOptimizer;

    public ApplyAudioProfileCommandHandler(IAudioOptimizer audioOptimizer)
    {
        _audioOptimizer = audioOptimizer;
    }

    public async Task<Result> Handle(ApplyAudioProfileCommand request, CancellationToken cancellationToken)
    {
        return await _audioOptimizer.ApplyProfileAsync(request.ProfileId, cancellationToken);
    }
}

/// <summary>
/// Command to revert audio settings to original.
/// </summary>
public sealed record RevertAudioSettingsCommand : IRequest<Result>;

/// <summary>
/// Handler for RevertAudioSettingsCommand.
/// </summary>
public sealed class RevertAudioSettingsCommandHandler : IRequestHandler<RevertAudioSettingsCommand, Result>
{
    private readonly IAudioOptimizer _audioOptimizer;

    public RevertAudioSettingsCommandHandler(IAudioOptimizer audioOptimizer)
    {
        _audioOptimizer = audioOptimizer;
    }

    public async Task<Result> Handle(RevertAudioSettingsCommand request, CancellationToken cancellationToken)
    {
        return await _audioOptimizer.RevertSettingsAsync(cancellationToken);
    }
}

/// <summary>
/// Query to get current audio settings.
/// </summary>
public sealed record GetAudioSettingsQuery : IRequest<Result<AudioSettings>>;

/// <summary>
/// Handler for GetAudioSettingsQuery.
/// </summary>
public sealed class GetAudioSettingsQueryHandler : IRequestHandler<GetAudioSettingsQuery, Result<AudioSettings>>
{
    private readonly IAudioOptimizer _audioOptimizer;

    public GetAudioSettingsQueryHandler(IAudioOptimizer audioOptimizer)
    {
        _audioOptimizer = audioOptimizer;
    }

    public async Task<Result<AudioSettings>> Handle(GetAudioSettingsQuery request, CancellationToken cancellationToken)
    {
        return await _audioOptimizer.GetCurrentSettingsAsync(cancellationToken);
    }
}

/// <summary>
/// Query to get available audio devices.
/// </summary>
public sealed record GetAudioDevicesQuery : IRequest<Result<IReadOnlyList<AudioDevice>>>;

/// <summary>
/// Handler for GetAudioDevicesQuery.
/// </summary>
public sealed class GetAudioDevicesQueryHandler : IRequestHandler<GetAudioDevicesQuery, Result<IReadOnlyList<AudioDevice>>>
{
    private readonly IAudioOptimizer _audioOptimizer;

    public GetAudioDevicesQueryHandler(IAudioOptimizer audioOptimizer)
    {
        _audioOptimizer = audioOptimizer;
    }

    public async Task<Result<IReadOnlyList<AudioDevice>>> Handle(GetAudioDevicesQuery request, CancellationToken cancellationToken)
    {
        return await _audioOptimizer.GetAvailableDevicesAsync(cancellationToken);
    }
}
