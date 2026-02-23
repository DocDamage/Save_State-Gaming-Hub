using MediatR;
using SaveState.Core.Common;
using SaveState.Core.MobileCompanion.Models;
using SaveState.Core.MobileCompanion.Services;

namespace SaveState.Application.MobileCompanion.Queries;

public sealed record GetPairedDevicesQuery : IRequest<Result<IReadOnlyList<MobileDeviceDto>>>;

public sealed class GetPairedDevicesQueryHandler : IRequestHandler<GetPairedDevicesQuery, Result<IReadOnlyList<MobileDeviceDto>>>
{
    private readonly IMobileCompanionService _companionService;

    public GetPairedDevicesQueryHandler(IMobileCompanionService companionService)
    {
        _companionService = companionService;
    }

    public async Task<Result<IReadOnlyList<MobileDeviceDto>>> Handle(GetPairedDevicesQuery request, CancellationToken cancellationToken)
    {
        var result = await _companionService.GetPairedDevicesAsync(cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return Result<IReadOnlyList<MobileDeviceDto>>.Failure(result.Error!, result.ErrorType);
        }

        var dtos = result.Value.Select(MapToDto).ToList();
        return Result<IReadOnlyList<MobileDeviceDto>>.Success(dtos);
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
