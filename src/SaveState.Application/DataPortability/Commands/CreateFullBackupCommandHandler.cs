using MediatR;
using SaveState.Core.Common;
using SaveState.Core.DataPortability;

namespace SaveState.Application.DataPortability.Commands;

/// <summary>
/// Handler for creating full backups.
/// </summary>
public class CreateFullBackupCommandHandler : IRequestHandler<CreateFullBackupCommand, Result<string>>
{
    private readonly IDataExportService _exportService;

    public CreateFullBackupCommandHandler(IDataExportService exportService)
    {
        _exportService = exportService;
    }

    public async Task<Result<string>> Handle(CreateFullBackupCommand request, CancellationToken cancellationToken)
    {
        return await _exportService.CreateFullBackupAsync(
            request.OutputPath,
            request.IncludeActualSaveFiles,
            cancellationToken);
    }
}
