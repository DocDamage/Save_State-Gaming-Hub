using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.MobileCompanion.Models;
using SaveState.Core.MobileCompanion.Services;

namespace SaveState.Application.MobileCompanion.Commands;

public sealed record SendRemoteCommand(
    Guid DeviceId,
    RemoteControlCommand Command,
    Dictionary<string, object>? Parameters = null,
    string? GameId = null
) : IRequest<Result>;

public sealed class SendRemoteCommandHandler : IRequestHandler<SendRemoteCommand, Result>
{
    private readonly IMobileCompanionService _companionService;
    private readonly ITimeProvider _timeProvider;

    public SendRemoteCommandHandler(IMobileCompanionService companionService, ITimeProvider timeProvider)
    {
        _companionService = companionService;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(SendRemoteCommand request, CancellationToken cancellationToken)
    {
        var message = new RemoteCommandMessage
        {
            Id = Guid.NewGuid(),
            Command = request.Command,
            Parameters = request.Parameters,
            Timestamp = _timeProvider.UtcNow,
            GameId = request.GameId
        };

        return await _companionService.SendCommandAsync(
            request.DeviceId,
            message,
            cancellationToken).ConfigureAwait(false);
    }
}
