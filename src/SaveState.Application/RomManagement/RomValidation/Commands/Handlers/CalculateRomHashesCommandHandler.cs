using MediatR;
using SaveState.Core.Common;
using SaveState.Core.RomManagement;
using SaveState.Core.RomManagement.Entities;
using SaveState.Core.RomManagement.RomValidation;
using SaveState.Core.RomManagement.RomValidation.Services;

namespace SaveState.Application.RomManagement.RomValidation.Commands.Handlers;

/// <summary>
/// Handler for calculating ROM hashes.
/// </summary>
public sealed class CalculateRomHashesCommandHandler
    : IRequestHandler<CalculateRomHashesCommand, Result<RomHashInfo>>
{
    private readonly IRomValidationService _validationService;
    private readonly IRomFileRepository _romFileRepository;
    private readonly IRomHashInfoRepository _hashInfoRepository;

    public CalculateRomHashesCommandHandler(
        IRomValidationService validationService,
        IRomFileRepository romFileRepository,
        IRomHashInfoRepository hashInfoRepository)
    {
        _validationService = validationService;
        _romFileRepository = romFileRepository;
        _hashInfoRepository = hashInfoRepository;
    }

    public async Task<Result<RomHashInfo>> Handle(
        CalculateRomHashesCommand request,
        CancellationToken cancellationToken)
    {
        var romFile = await _romFileRepository.GetByIdAsync(request.RomFileId, cancellationToken);
        if (romFile is null)
        {
            return Result<RomHashInfo>.Failure(
                $"ROM file {request.RomFileId} not found",
                ErrorType.NotFound);
        }

        var options = new RomValidationOptions
        {
            CalculateCrc32 = request.CalculateCrc32,
            CalculateMd5 = request.CalculateMd5,
            CalculateSha1 = request.CalculateSha1,
            CalculateSha256 = request.CalculateSha256,
            MatchAgainstDatFiles = false
        };

        var result = await _validationService.CalculateHashesAsync(
            romFile,
            options,
            cancellationToken);

        if (result.IsSuccess)
        {
            await _hashInfoRepository.AddAsync(result.Value, cancellationToken);
        }

        return result;
    }
}
