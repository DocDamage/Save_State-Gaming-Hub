using Microsoft.Extensions.Logging;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Core.RomManagement;
using SaveState.Core.RomManagement.RomValidation;
using SaveState.Core.RomManagement.RomValidation.Services;

namespace SaveState.Application.RomManagement.RomValidation.Commands.Handlers;

/// <summary>
/// Handler for validating a single ROM file.
/// </summary>
public class ValidateRomCommandHandler : MediatR.IRequestHandler<ValidateRomCommand, Result<RomValidationReport>>
{
    private readonly IRomValidationService _validationService;
    private readonly IRomFileRepository _romRepository;
    private readonly ILogger<ValidateRomCommandHandler> _logger;

    public ValidateRomCommandHandler(
        IRomValidationService validationService,
        IRomFileRepository romRepository,
        ILogger<ValidateRomCommandHandler> logger)
    {
        _validationService = validationService;
        _romRepository = romRepository;
        _logger = logger;
    }

    /// <summary>
    /// Handles the command to validate a ROM file.
    /// </summary>
    public async Task<Result<RomValidationReport>> Handle(ValidateRomCommand request, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Validating ROM {RomFileId}", request.RomFileId);

            var rom = await _romRepository.GetByIdAsync(request.RomFileId, ct).ConfigureAwait(false);
            if (rom is null)
            {
                return Result<RomValidationReport>.Failure($"ROM file {request.RomFileId} not found");
            }

            var options = request.Options ?? new RomValidationOptions
            {
                CalculateCrc32 = true,
                CalculateMd5 = true,
                CalculateSha1 = true,
                MatchAgainstDatFiles = true
            };

            var result = await _validationService.ValidateRomAsync(rom, options, ct).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "ROM {RomFileId} validation completed with status {Status}",
                    request.RomFileId, result.Value.Status);
            }
            else
            {
                _logger.LogWarning(
                    "ROM {RomFileId} validation failed: {Error}",
                    request.RomFileId, result.Error);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating ROM {RomFileId}", request.RomFileId);
            return Result<RomValidationReport>.Failure($"Validation failed: {ex.Message}");
        }
    }
}
