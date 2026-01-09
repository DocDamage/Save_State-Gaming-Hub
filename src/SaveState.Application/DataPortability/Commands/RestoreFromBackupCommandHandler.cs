using MediatR;
using SaveState.Core.Common;
using SaveState.Core.DataPortability;

namespace SaveState.Application.DataPortability.Commands;

/// <summary>
/// Handler for restoring from backups.
/// </summary>
public class RestoreFromBackupCommandHandler : IRequestHandler<RestoreFromBackupCommand, Result<DataImportResult>>
{
    private readonly IDataImportService _importService;

    public RestoreFromBackupCommandHandler(IDataImportService importService)
    {
        _importService = importService;
    }

    public async Task<Result<DataImportResult>> Handle(RestoreFromBackupCommand request, CancellationToken cancellationToken)
    {
        return await _importService.RestoreFromBackupAsync(
            request.BackupPath,
            request.Options,
            cancellationToken);
    }
}
