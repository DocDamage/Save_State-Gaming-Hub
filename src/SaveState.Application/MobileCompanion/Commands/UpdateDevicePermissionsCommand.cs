using MediatR;
using SaveState.Core.Common;
using SaveState.Core.MobileCompanion.Services;

namespace SaveState.Application.MobileCompanion.Commands;

public sealed record UpdateDevicePermissionsCommand(
    Guid DeviceId,
    List<string> Permissions
) : IRequest<Result>;

public sealed class UpdateDevicePermissionsCommandHandler : IRequestHandler<UpdateDevicePermissionsCommand, Result>
{
    private readonly IMobileCompanionService _companionService;

    public UpdateDevicePermissionsCommandHandler(IMobileCompanionService companionService)
    {
        _companionService = companionService;
    }

    public async Task<Result> Handle(UpdateDevicePermissionsCommand request, CancellationToken cancellationToken)
    {
        return await _companionService.UpdateDevicePermissionsAsync(
            request.DeviceId,
            request.Permissions,
            cancellationToken).ConfigureAwait(false);
    }
}
