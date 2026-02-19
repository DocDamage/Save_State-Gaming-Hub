using MediatR;
using SaveState.Application.Common;
using SaveState.Core.RomManagement.RomValidation;
using SaveState.Core.RomManagement.RomValidation.Services;

namespace SaveState.Application.RomManagement.RomValidation.Commands;

/// <summary>
/// Command to export ROM validation results to a file.
/// </summary>
public sealed record ExportValidationResultsCommand(
    string OutputPath,
    ValidationExportFormat Format = ValidationExportFormat.Json,
    Guid? PlatformId = null,
    List<ValidationStatus>? IncludeStatuses = null,
    bool IncludeHashes = true,
    bool IncludeDatMatches = true,
    bool IncludeDuplicates = true) : IRequest<Result<string>>;
