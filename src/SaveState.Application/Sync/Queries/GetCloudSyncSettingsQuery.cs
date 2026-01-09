using MediatR;
using SaveState.Core.Common;

namespace SaveState.Application.Sync.Queries;

public record GetCloudSyncSettingsQuery() : IRequest<Result<CloudSyncSettingsDto>>;

public record CloudSyncSettingsDto(
    string PreferredProvider,
    bool AutoSyncOnExit,
    string OneDriveClientId,
    string GoogleDriveClientId);
