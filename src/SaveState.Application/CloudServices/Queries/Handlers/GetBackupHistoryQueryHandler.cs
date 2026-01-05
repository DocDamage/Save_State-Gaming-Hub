using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.CloudServices.Services;
using SaveState.Core.Common;

namespace SaveState.Application.CloudServices.Queries.Handlers;

public class GetBackupHistoryQueryHandler : IRequestHandler<GetBackupHistoryQuery, Result<IReadOnlyList<BackupMetadata>>>
{
    private readonly IBackupService _backupService;
    private readonly ILogger<GetBackupHistoryQueryHandler> _logger;

    public GetBackupHistoryQueryHandler(IBackupService backupService, ILogger<GetBackupHistoryQueryHandler> logger)
    {
        _backupService = backupService;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<BackupMetadata>>> Handle(GetBackupHistoryQuery request, CancellationToken ct)
    {
        try
        {
            var history = await _backupService.GetBackupHistoryAsync(ct);
            return Result<IReadOnlyList<BackupMetadata>>.Success(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get backup history");
            return Result<IReadOnlyList<BackupMetadata>>.Failure($"Failed to get backup history: {ex.Message}");
        }
    }
}
