using MediatR;
using SaveState.Application.CloudServices.Services;
using SaveState.Core.Common;

namespace SaveState.Application.CloudServices.Queries;

public record GetBackupHistoryQuery : IRequest<Result<IReadOnlyList<BackupMetadata>>>;
