using MediatR;
using SaveState.Core.Common;
using SaveState.Core.DataPortability;

namespace SaveState.Application.DataPortability.Queries;

/// <summary>
/// Query to validate a backup file before restoration.
/// </summary>
public record ValidateBackupQuery(string BackupPath) : IRequest<Result<BackupValidationResult>>;
