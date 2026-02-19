using MediatR;
using SaveState.Core.Common;
using SaveState.Core.RomManagement;
using SaveState.Core.RomManagement.Entities;
using SaveState.Core.RomManagement.RomValidation;
using SaveState.Core.RomManagement.RomValidation.Services;

namespace SaveState.Application.RomManagement.RomValidation.Commands.Handlers;

/// <summary>
/// Handler for batch ROM validation.
/// </summary>
public sealed class BatchValidateRomsCommandHandler
    : IRequestHandler<BatchValidateRomsCommand, Result<RomValidationJob>>
{
    private readonly IRomValidationService _validationService;
    private readonly IRomFileRepository _romFileRepository;
    private readonly IRomValidationReportRepository _reportRepository;

    public BatchValidateRomsCommandHandler(
        IRomValidationService validationService,
        IRomFileRepository romFileRepository,
        IRomValidationReportRepository reportRepository)
    {
        _validationService = validationService;
        _romFileRepository = romFileRepository;
        _reportRepository = reportRepository;
    }

    public async Task<Result<RomValidationJob>> Handle(
        BatchValidateRomsCommand request,
        CancellationToken cancellationToken)
    {
        var options = request.Options ?? new RomValidationOptions
        {
            CalculateCrc32 = true,
            CalculateMd5 = true,
            CalculateSha1 = true,
            CalculateSha256 = false,
            MatchAgainstDatFiles = true
        };

        var romFiles = new List<RomFile>();

        if (request.RomFileIds?.Count > 0)
        {
            foreach (var id in request.RomFileIds)
            {
                var romFile = await _romFileRepository.GetByIdAsync(id, cancellationToken);
                if (romFile is not null)
                {
                    romFiles.Add(romFile);
                }
            }
        }
        else if (request.PlatformIds?.Count > 0)
        {
            foreach (var platformId in request.PlatformIds)
            {
                var files = await _romFileRepository.GetByPlatformIdAsync(platformId, cancellationToken);
                romFiles.AddRange(files);
            }
        }

        if (romFiles.Count == 0)
        {
            return Result<RomValidationJob>.Failure(
                "No ROM files found for validation",
                ErrorType.Validation);
        }

        var job = new RomValidationJob
        {
            Name = request.JobName,
            RomFileIds = romFiles.Select(r => r.Id).ToList(),
            TotalRoms = romFiles.Count
        };

        foreach (var romFile in romFiles)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var result = await _validationService.ValidateRomAsync(
                romFile,
                options,
                cancellationToken);

            if (result.IsSuccess)
            {
                await _reportRepository.AddAsync(result.Value, cancellationToken);
                job.ProcessedRoms++;
            }
            else
            {
                job.Errors.Add($"Failed to validate {romFile.Id}: {result.Error}");
            }
        }

        job.Status = JobStatus.Completed;
        return Result<RomValidationJob>.Success(job);
    }
}
