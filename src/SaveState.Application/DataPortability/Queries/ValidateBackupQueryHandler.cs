using MediatR;
using SaveState.Core.Common;
using SaveState.Core.DataPortability;

namespace SaveState.Application.DataPortability.Queries;

/// <summary>
/// Handler for validating backup files.
/// </summary>
public class ValidateBackupQueryHandler : IRequestHandler<ValidateBackupQuery, Result<BackupValidationResult>>
{
    private readonly IDataImportService _importService;

    public ValidateBackupQueryHandler(IDataImportService importService)
    {
        _importService = importService;
    }

    public async Task<Result<BackupValidationResult>> Handle(ValidateBackupQuery request, CancellationToken cancellationToken)
    {
        return await _importService.ValidateBackupAsync(request.BackupPath, cancellationToken);
    }
}
