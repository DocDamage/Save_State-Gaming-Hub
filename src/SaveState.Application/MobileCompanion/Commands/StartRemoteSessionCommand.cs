using MediatR;
using SaveState.Core.Common;
using SaveState.Core.MobileCompanion.Models;
using SaveState.Core.MobileCompanion.Services;

namespace SaveState.Application.MobileCompanion.Commands;

public sealed record StartRemoteSessionCommand(
    Guid DeviceId,
    string ConnectionId
) : IRequest<Result<RemoteSessionDto>>;

public sealed class StartRemoteSessionCommandHandler : IRequestHandler<StartRemoteSessionCommand, Result<RemoteSessionDto>>
{
    private readonly IMobileCompanionService _companionService;

    public StartRemoteSessionCommandHandler(IMobileCompanionService companionService)
    {
        _companionService = companionService;
    }

    public async Task<Result<RemoteSessionDto>> Handle(StartRemoteSessionCommand request, CancellationToken cancellationToken)
    {
        var result = await _companionService.StartSessionAsync(
            request.DeviceId,
            request.ConnectionId,
            cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return Result<RemoteSessionDto>.Failure(result.Error!, result.ErrorType);
        }

        var dto = new RemoteSessionDto
        {
            Id = result.Value.Id,
            DeviceId = result.Value.DeviceId,
            StartedAt = result.Value.StartedAt,
            LastActivityAt = result.Value.LastActivityAt,
            CurrentMode = result.Value.CurrentMode,
            IsActive = result.Value.IsActive,
            ConnectionId = result.Value.ConnectionId
        };

        return Result<RemoteSessionDto>.Success(dto);
    }
}
