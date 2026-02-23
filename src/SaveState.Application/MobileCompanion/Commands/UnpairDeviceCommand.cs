using MediatR;
using SaveState.Core.Common;
using SaveState.Core.MobileCompanion.Services;

namespace SaveState.Application.MobileCompanion.Commands;

public sealed record UnpairDeviceCommand(Guid DeviceId) : IRequest<Result>;

public sealed class UnpairDeviceCommandHandler : IRequestHandler<UnpairDeviceCommand, Result>
{
    private readonly IMobileCompanionService _companionService;

    public UnpairDeviceCommandHandler(IMobileCompanionService companionService)
    {
        _companionService = companionService;
    }

    public async Task<Result> Handle(UnpairDeviceCommand request, CancellationToken cancellationToken)
    {
        return await _companionService.UnpairDeviceAsync(request.DeviceId, cancellationToken)
            .ConfigureAwait(false);
    }
}
