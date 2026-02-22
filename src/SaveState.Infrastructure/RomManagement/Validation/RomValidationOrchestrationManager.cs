using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.RomManagement;
using SaveState.Core.RomManagement.Entities;
using SaveState.Core.RomManagement.RomValidation;
using SaveState.Core.RomManagement.RomValidation.Services;

namespace SaveState.Infrastructure.RomManagement.Validation;

internal sealed class RomValidationOrchestrationManager
{
    private readonly IRomFileRepository _romRepository;
    private readonly IRomHashInfoRepository _hashRepository;
    private readonly IRomValidationReportRepository _reportRepository;
    private readonly ILogger<RomValidationService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly RomHashWorkflowManager _hashWorkflowManager;
    private readonly RomDatMatchingManager _datMatchingManager;

    public RomValidationOrchestrationManager(
        IRomFileRepository romRepository,
        IRomHashInfoRepository hashRepository,
        IRomValidationReportRepository reportRepository,
        ILogger<RomValidationService> logger,
        ITimeProvider timeProvider,
        RomHashWorkflowManager hashWorkflowManager,
        RomDatMatchingManager datMatchingManager)
    {
        _romRepository = romRepository;
        _hashRepository = hashRepository;
        _reportRepository = reportRepository;
        _logger = logger;
        _timeProvider = timeProvider;
        _hashWorkflowManager = hashWorkflowManager;
        _datMatchingManager = datMatchingManager;
    }

    public async Task<Result<RomValidationReport>> ValidateRomAsync(
        RomFile romFile,
        RomValidationOptions options,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Validating ROM: {RomTitle}", romFile.Title);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var report = new RomValidationReport
            {
                RomFileId = (Guid)romFile.Id,
                Status = ValidationStatus.Validating
            };

            if (options.VerifyFileIntegrity)
            {
                var integrityResult = await _hashWorkflowManager.VerifyFileIntegrityAsync(romFile.FilePath.Value, ct).ConfigureAwait(false);
                if (!integrityResult.IsSuccess)
                {
                    report.Issues.Add(new ValidationIssue
                    {
                        Severity = IssueSeverity.Error,
                        Category = IssueCategory.File,
                        Message = $"File integrity check failed: {integrityResult.Error}"
                    });
                    report.Status = ValidationStatus.Corrupted;
                }
                else if (!integrityResult.Value.IsIntact)
                {
                    foreach (var error in integrityResult.Value.ReadErrors)
                    {
                        report.Issues.Add(new ValidationIssue
                        {
                            Severity = IssueSeverity.Error,
                            Category = IssueCategory.File,
                            Message = error
                        });
                    }

                    report.Status = ValidationStatus.Corrupted;
                }
            }

            RomHashInfo? hashInfo = null;
            if (report.Status != ValidationStatus.Corrupted)
            {
                var existingHash = await _hashRepository.GetByRomFileIdAsync((Guid)romFile.Id, ct).ConfigureAwait(false);

                if (existingHash != null && options.SkipValidated)
                {
                    hashInfo = existingHash;
                    _logger.LogDebug("Using existing hash info for {RomTitle}", romFile.Title);
                }
                else
                {
                    var hashResult = await _hashWorkflowManager.CalculateHashesAsync(romFile, options, ct).ConfigureAwait(false);
                    if (hashResult.IsSuccess)
                    {
                        hashInfo = hashResult.Value;
                        romFile.SetChecksum(hashInfo.GetPrimaryHash());
                    }
                    else
                    {
                        report.Issues.Add(new ValidationIssue
                        {
                            Severity = IssueSeverity.Error,
                            Category = IssueCategory.Hash,
                            Message = $"Hash calculation failed: {hashResult.Error}"
                        });
                    }
                }
            }

            report.HashInfo = hashInfo;

            if (options.MatchAgainstDatFiles && hashInfo != null && options.DatFilePaths.Any())
            {
                var matchResult = await _datMatchingManager.MatchAgainstDatFilesAsync(hashInfo, options.DatFilePaths, ct).ConfigureAwait(false);
                if (matchResult.IsSuccess)
                {
                    report.MatchResult = matchResult.Value;

                    if (matchResult.Value.IsMatchFound)
                    {
                        if (matchResult.Value.IsGoodDump)
                        {
                            report.Status = ValidationStatus.Verified;
                            report.SuggestedName = matchResult.Value.MatchedEntry?.Name;
                        }
                        else
                        {
                            report.Status = ValidationStatus.BadDump;
                            report.Issues.Add(new ValidationIssue
                            {
                                Severity = IssueSeverity.Warning,
                                Category = IssueCategory.Database,
                                Message = $"ROM is a {matchResult.Value.MatchedEntry?.DumpStatus} dump",
                                SuggestedFix = "Consider replacing with a verified good dump"
                            });
                        }
                    }
                    else
                    {
                        report.Status = ValidationStatus.Unknown;
                        report.Issues.Add(new ValidationIssue
                        {
                            Severity = IssueSeverity.Warning,
                            Category = IssueCategory.Database,
                            Message = "ROM not found in DAT files",
                            SuggestedFix = "Verify ROM source or add custom DAT entry"
                        });
                    }
                }
            }
            else if (report.Status != ValidationStatus.Corrupted)
            {
                report.Status = ValidationStatus.Valid;
            }

            stopwatch.Stop();
            report.ValidationDuration = stopwatch.Elapsed;
            await _reportRepository.AddAsync(report, ct).ConfigureAwait(false);

            _logger.LogInformation(
                "Validation completed for {RomTitle} with status {Status} in {ElapsedMs}ms",
                romFile.Title,
                report.Status,
                stopwatch.ElapsedMilliseconds);

            return Result<RomValidationReport>.Success(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate ROM: {RomTitle}", romFile.Title);
            return Result<RomValidationReport>.Failure($"Validation failed: {ex.Message}");
        }
    }

    public async Task<Result<RomValidationJob>> ValidateBatchAsync(
        RomValidationJob job,
        RomValidationOptions options,
        IProgress<RomValidationProgress>? progress = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Starting batch validation job: {JobName}", job.Name);

            job.Status = JobStatus.Running;
            job.StartedAt = _timeProvider.UtcNow;

            IEnumerable<RomFile> roms;
            if (job.RomFileIds.Any())
            {
                roms = await _romRepository.GetByIdsAsync(job.RomFileIds, ct).ConfigureAwait(false);
            }
            else if (job.PlatformIds.Any())
            {
                roms = await _romRepository.GetByPlatformIdsAsync(job.PlatformIds, ct).ConfigureAwait(false);
            }
            else
            {
                roms = await _romRepository.GetAllAsync(ct).ConfigureAwait(false);
            }

            var romList = roms.ToList();
            job.TotalRoms = romList.Count;

            for (int i = 0; i < romList.Count; i++)
            {
                if (ct.IsCancellationRequested)
                {
                    job.Status = JobStatus.Cancelled;
                    return Result<RomValidationJob>.Success(job);
                }

                var rom = romList[i];
                progress?.Report(new RomValidationProgress
                {
                    CurrentFile = rom.Title,
                    ProcessedCount = i,
                    TotalCount = romList.Count,
                    Operation = "Validating",
                    Status = job.Status.ToString()
                });

                try
                {
                    var result = await ValidateRomAsync(rom, options, ct).ConfigureAwait(false);
                    if (result.IsSuccess)
                    {
                        job.Results.Add(result.Value);
                    }
                    else
                    {
                        job.Errors.Add($"Failed to validate {rom.Title}: {result.Error}");
                    }
                }
                catch (Exception ex)
                {
                    job.Errors.Add($"Exception validating {rom.Title}: {ex.Message}");
                    _logger.LogError(ex, "Error validating ROM in batch: {RomTitle}", rom.Title);
                }

                job.ProcessedRoms = i + 1;
            }

            job.Status = JobStatus.Completed;
            job.CompletedAt = _timeProvider.UtcNow;

            _logger.LogInformation(
                "Batch validation job completed: {Processed}/{Total} ROMs validated",
                job.ProcessedRoms,
                job.TotalRoms);

            return Result<RomValidationJob>.Success(job);
        }
        catch (Exception ex)
        {
            job.Status = JobStatus.Failed;
            _logger.LogError(ex, "Batch validation job failed: {JobName}", job.Name);
            return Result<RomValidationJob>.Failure($"Batch validation failed: {ex.Message}");
        }
    }
}
