using MediatR;
using SaveState.Core.Common;
using SaveState.Core.DataPortability;

namespace SaveState.Application.DataPortability.Commands;

/// <summary>
/// Command to restore data from a backup file.
/// </summary>
public record RestoreFromBackupCommand(
    string BackupPath,
    RestoreOptions Options) : IRequest<Result<DataImportResult>>;
