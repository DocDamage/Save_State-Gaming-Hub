using MediatR;
using SaveState.Core.Common;
using SaveState.Core.MobileCompanion.Models;
using SaveState.Core.MobileCompanion.Services;

namespace SaveState.Application.MobileCompanion.Commands;

public sealed record CompletePairingCommand(
    string PairingCode,
    DeviceInfoDto DeviceInfo
) : IRequest<Result<MobileDeviceDto>>;

public sealed class CompletePairingCommandHandler : IRequestHandler<CompletePairingCommand, Result<MobileDeviceDto>>
{
    private readonly IMobileCompanionService _companionService;

    public CompletePairingCommandHandler(IMobileCompanionService companionService)
    {
        _companionService = companionService;
    }

    public async Task<Result<MobileDeviceDto>> Handle(CompletePairingCommand request, CancellationToken cancellationToken)
    {
        var deviceInfo = new DeviceInfo
        {
            DeviceName = request.DeviceInfo.DeviceName,
            DeviceType = request.DeviceInfo.DeviceType,
            DeviceModel = request.DeviceInfo.DeviceModel,
            OsVersion = request.DeviceInfo.OsVersion,
            AppVersion = request.DeviceInfo.AppVersion,
            PushNotificationToken = request.DeviceInfo.PushNotificationToken
        };

        var result = await _companionService.CompletePairingAsync(
            request.PairingCode,
            deviceInfo,
            cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return Result<MobileDeviceDto>.Failure(result.Error!, result.ErrorType);
        }

        var dto = MapToDto(result.Value);
        return Result<MobileDeviceDto>.Success(dto);
    }

    private static MobileDeviceDto MapToDto(MobileDevice device)
    {
        return new MobileDeviceDto
        {
            Id = device.Id,
            DeviceName = device.DeviceName,
            DeviceType = device.DeviceType,
            DeviceModel = device.DeviceModel,
            OsVersion = device.OsVersion,
            AppVersion = device.AppVersion,
            PairedAt = device.PairedAt,
            LastConnectedAt = device.LastConnectedAt,
            IsConnected = device.IsConnected,
            Status = device.Status,
            Permissions = device.Permissions
        };
    }
}
