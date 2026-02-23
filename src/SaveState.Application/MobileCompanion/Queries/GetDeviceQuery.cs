using MediatR;
using SaveState.Core.Common;
using SaveState.Core.MobileCompanion.Models;
using SaveState.Core.MobileCompanion.Services;

namespace SaveState.Application.MobileCompanion.Queries;

public sealed record GetDeviceQuery(Guid DeviceId) : IRequest<Result<MobileDeviceDto>>;

public sealed class GetDeviceQueryHandler : IRequestHandler<GetDeviceQuery, Result<MobileDeviceDto>>
{
    private readonly IMobileCompanionService _companionService;

    public GetDeviceQueryHandler(IMobileCompanionService companionService)
    {
        _companionService = companionService;
    }

    public async Task<Result<MobileDeviceDto>> Handle(GetDeviceQuery request, CancellationToken cancellationToken)
    {
        var result = await _companionService.GetDeviceAsync(request.DeviceId, cancellationToken).ConfigureAwait(false);

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
