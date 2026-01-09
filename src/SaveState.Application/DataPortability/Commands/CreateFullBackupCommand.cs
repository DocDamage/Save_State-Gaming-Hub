using MediatR;
using SaveState.Core.Common;

namespace SaveState.Application.DataPortability.Commands;

/// <summary>
/// Command to create a full backup of all SaveState data.
/// </summary>
public record CreateFullBackupCommand(
    string OutputPath,
    bool IncludeActualSaveFiles = false) : IRequest<Result<string>>;
