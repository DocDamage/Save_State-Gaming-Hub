using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Application.Sync.Queries.Handlers;

public class GetCloudSyncSettingsQueryHandler : IRequestHandler<GetCloudSyncSettingsQuery, Result<CloudSyncSettingsDto>>
{
    private readonly IUserPreferencesService _preferencesService;

    public GetCloudSyncSettingsQueryHandler(IUserPreferencesService preferencesService)
    {
        _preferencesService = preferencesService;
    }

    public async Task<Result<CloudSyncSettingsDto>> Handle(GetCloudSyncSettingsQuery request, CancellationToken cancellationToken)
    {
        var preferredProvider = await _preferencesService.GetPreferredCloudProviderAsync(cancellationToken);
        var autoSyncOnExit = await _preferencesService.GetAutoSyncOnExitAsync(cancellationToken);
        var onedriveClientId = await _preferencesService.GetCloudClientIdAsync("OneDrive", cancellationToken);
        var googledriveClientId = await _preferencesService.GetCloudClientIdAsync("Google Drive", cancellationToken);

        return Result<CloudSyncSettingsDto>.Success(new CloudSyncSettingsDto(
            preferredProvider,
            autoSyncOnExit,
            onedriveClientId,
            googledriveClientId));
    }
}
