using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Interfaces;
using SaveState.Core.Common.Services;
using SaveState.Core.RomManagement;
using SaveState.Core.RomManagement.Entities;
using SaveState.Core.RomManagement.RomValidation;
using SaveState.Core.RomManagement.RomValidation.Services;

namespace SaveState.Infrastructure.RomManagement;

/// <summary>
/// Implementation of comprehensive ROM validation service.
/// Supports multiple hash algorithms, DAT file matching, and integrity verification.
/// </summary>
public class RomValidationService : IRomValidationService
{
    private readonly IFileSystem _fileSystem;
    private readonly IRomFileRepository _romRepository;
    private readonly IRomHashInfoRepository _hashRepository;
    private readonly IRomValidationReportRepository _reportRepository;
    private readonly ILogger<RomValidationService> _logger;
    private readonly ITimeProvider _timeProvider;

    public RomValidationService(
        IFileSystem fileSystem,
        IRomFileRepository romRepository,
        IRomHashInfoRepository hashRepository,
        IRomValidationReportRepository reportRepository,
        ILogger<RomValidationService> logger,
        ITimeProvider timeProvider)
    {
        _fileSystem = fileSystem;
        _romRepository = romRepository;
        _hashRepository = hashRepository;
        _reportRepository = reportRepository;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5351:DoNotUseBrokenCryptographicAlgorithms", Justification = "MD5 required for No-Intro/Redump ROM database compatibility")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5350:DoNotUseWeakCryptographicAlgorithms", Justification = "SHA1 required for No-Intro/Redump ROM database compatibility")]
    public async Task<Result<RomHashInfo>> CalculateHashesAsync(
        RomFile romFile,
        RomValidationOptions options,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Calculating hashes for ROM: {RomTitle}", romFile.Title);

            if (!await _fileSystem.FileExistsAsync(romFile.FilePath.Value, ct).ConfigureAwait(false))
            {
                return Result<RomHashInfo>.Failure($"ROM file not found: {romFile.FilePath.Value}");
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var fileBytes = await _fileSystem.ReadAllBytesAsync(romFile.FilePath.Value, ct).ConfigureAwait(false);

            var hashInfo = new RomHashInfo { RomFileId = (Guid)romFile.Id };
            var errors = new List<string>();

            if (options.CalculateCrc32)
            {
                try
                {
                    hashInfo.Crc32 = CalculateCrc32(fileBytes);
                }
                catch (Exception ex)
                {
                    errors.Add($"CRC32 calculation failed: {ex.Message}");
                    _logger.LogWarning(ex, "CRC32 calculation failed for {RomTitle}", romFile.Title);
                }
            }

            if (options.CalculateMd5)
            {
                try
                {
                    using var md5 = MD5.Create();
                    var hash = md5.ComputeHash(fileBytes);
                    hashInfo.Md5 = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
                catch (Exception ex)
                {
                    errors.Add($"MD5 calculation failed: {ex.Message}");
                    _logger.LogWarning(ex, "MD5 calculation failed for {RomTitle}", romFile.Title);
                }
            }

            if (options.CalculateSha1)
            {
                try
                {
                    using var sha1 = SHA1.Create();
                    var hash = sha1.ComputeHash(fileBytes);
                    hashInfo.Sha1 = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
                catch (Exception ex)
                {
                    errors.Add($"SHA1 calculation failed: {ex.Message}");
                    _logger.LogWarning(ex, "SHA1 calculation failed for {RomTitle}", romFile.Title);
                }
            }

            if (options.CalculateSha256)
            {
                try
                {
                    using var sha256 = SHA256.Create();
                    var hash = sha256.ComputeHash(fileBytes);
                    hashInfo.Sha256 = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
                catch (Exception ex)
                {
                    errors.Add($"SHA256 calculation failed: {ex.Message}");
                    _logger.LogWarning(ex, "SHA256 calculation failed for {RomTitle}", romFile.Title);
                }
            }

            stopwatch.Stop();
            hashInfo.CalculationTime = stopwatch.Elapsed;
            hashInfo.IsComplete = errors.Count == 0;
            hashInfo.Errors = errors;

            await _hashRepository.AddAsync(hashInfo, ct).ConfigureAwait(false);

            _logger.LogInformation(
                "Hash calculation completed for {RomTitle} in {ElapsedMs}ms",
                romFile.Title, stopwatch.ElapsedMilliseconds);

            return Result<RomHashInfo>.Success(hashInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate hashes for ROM: {RomTitle}", romFile.Title);
            return Result<RomHashInfo>.Failure($"Hash calculation failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
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
                var integrityResult = await VerifyFileIntegrityAsync(romFile.FilePath.Value, ct).ConfigureAwait(false);
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
                    var hashResult = await CalculateHashesAsync(romFile, options, ct).ConfigureAwait(false);
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
                var matchResult = await MatchAgainstDatFilesAsync(hashInfo, options.DatFilePaths, ct).ConfigureAwait(false);
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
                romFile.Title, report.Status, stopwatch.ElapsedMilliseconds);

            return Result<RomValidationReport>.Success(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate ROM: {RomTitle}", romFile.Title);
            return Result<RomValidationReport>.Failure($"Validation failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
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
                job.ProcessedRoms, job.TotalRoms);

            return Result<RomValidationJob>.Success(job);
        }
        catch (Exception ex)
        {
            job.Status = JobStatus.Failed;
            _logger.LogError(ex, "Batch validation job failed: {JobName}", job.Name);
            return Result<RomValidationJob>.Failure($"Batch validation failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<RomMatchResult>> MatchAgainstDatFilesAsync(
        RomHashInfo hashInfo,
        IEnumerable<string> datFilePaths,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Matching ROM against DAT files");
            var result = new RomMatchResult { IsMatchFound = false };

            foreach (var datPath in datFilePaths)
            {
                if (!await _fileSystem.FileExistsAsync(datPath, ct).ConfigureAwait(false))
                {
                    _logger.LogWarning("DAT file not found: {DatPath}", datPath);
                    continue;
                }

                var datResult = await LoadDatFileAsync(datPath, ct).ConfigureAwait(false);
                if (!datResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to load DAT file: {DatPath}", datPath);
                    continue;
                }

                var entries = datResult.Value;

                foreach (var entry in entries)
                {
                    if (hashInfo.Matches(entry))
                    {
                        result.IsMatchFound = true;
                        result.MatchedEntry = entry;
                        result.Confidence = MatchConfidence.Exact;
                        result.SourceDat = datPath;
                        return Result<RomMatchResult>.Success(result);
                    }
                }

                var alternatives = entries.Where(e => IsPartialMatch(hashInfo, e)).ToList();
                if (alternatives.Any())
                {
                    result.AlternativeMatches = alternatives;
                    if (result.Confidence == MatchConfidence.None)
                    {
                        result.Confidence = MatchConfidence.Low;
                    }
                }
            }

            return Result<RomMatchResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to match ROM against DAT files");
            return Result<RomMatchResult>.Failure($"DAT matching failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<List<DuplicateRomInfo>>> FindDuplicatesAsync(
        Guid? platformId = null,
        HashAlgorithmType? hashType = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Finding duplicate ROMs");
            var hashInfos = await _hashRepository.GetAllAsync(ct).ConfigureAwait(false);

            if (platformId.HasValue)
            {
                var romIds = await _romRepository.GetIdsByPlatformAsync(platformId.Value, ct).ConfigureAwait(false);
                hashInfos = hashInfos.Where(h => romIds.Contains(h.RomFileId)).ToList();
            }

            var hashProperty = hashType ?? HashAlgorithmType.Sha1;
            var grouped = new Dictionary<string, List<RomHashInfo>>();

            foreach (var hashInfo in hashInfos)
            {
                var hash = GetHashByType(hashInfo, hashProperty);
                if (!string.IsNullOrEmpty(hash))
                {
                    if (!grouped.ContainsKey(hash))
                        grouped[hash] = new List<RomHashInfo>();
                    grouped[hash].Add(hashInfo);
                }
            }

            var duplicates = new List<DuplicateRomInfo>();
            foreach (var group in grouped.Where(g => g.Value.Count > 1))
            {
                var dupInfo = new DuplicateRomInfo
                {
                    Hash = group.Key,
                    HashType = hashProperty
                };

                foreach (var hashInfo in group.Value)
                {
                    var rom = await _romRepository.GetByIdAsync(hashInfo.RomFileId, ct).ConfigureAwait(false);
                    if (rom != null)
                    {
                        dupInfo.Duplicates.Add(new RomDuplicateEntry
                        {
                            RomFileId = (Guid)rom.Id,
                            FileName = Path.GetFileName(rom.FilePath.Value),
                            Directory = Path.GetDirectoryName(rom.FilePath.Value) ?? string.Empty,
                            FullPath = rom.FilePath.Value,
                            FileSize = rom.FileSize,
                            AddedAt = rom.ScannedAt
                        });
                    }
                }

                duplicates.Add(dupInfo);
            }

            _logger.LogInformation("Found {DuplicateCount} sets of duplicate ROMs", duplicates.Count);
            return Result<List<DuplicateRomInfo>>.Success(duplicates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find duplicate ROMs");
            return Result<List<DuplicateRomInfo>>.Failure($"Duplicate detection failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<MissingGameReport>> GenerateMissingGameReportAsync(
        Guid platformId,
        string referenceDatPath,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating missing game report for platform {PlatformId}", platformId);

            if (!await _fileSystem.FileExistsAsync(referenceDatPath, ct).ConfigureAwait(false))
            {
                return Result<MissingGameReport>.Failure($"Reference DAT file not found: {referenceDatPath}");
            }

            var datResult = await LoadDatFileAsync(referenceDatPath, ct).ConfigureAwait(false);
            if (!datResult.IsSuccess)
            {
                return Result<MissingGameReport>.Failure($"Failed to load DAT file: {datResult.Error}");
            }

            var datEntries = datResult.Value;
            var platformRoms = await _romRepository.GetByPlatformIdAsync(platformId, ct).ConfigureAwait(false);
            var platform = await _romRepository.GetPlatformAsync(platformId, ct).ConfigureAwait(false);

            var ownedHashes = new HashSet<string>(
                platformRoms.Select(r => r.Hash?.ToLowerInvariant() ?? string.Empty),
                StringComparer.OrdinalIgnoreCase);

            var report = new MissingGameReport
            {
                PlatformId = platformId,
                PlatformName = platform?.Name ?? "Unknown",
                ReferenceDatFile = referenceDatPath,
                TotalGames = datEntries.Count,
                OwnedGames = platformRoms.Select(r => r.Title).ToList()
            };

            foreach (var entry in datEntries)
            {
                var entryHash = entry.Sha1 ?? entry.Md5 ?? entry.Crc32 ?? string.Empty;
                if (!ownedHashes.Contains(entryHash))
                {
                    report.MissingGames.Add(new MissingGameEntry
                    {
                        Name = entry.Name,
                        Region = entry.Region,
                        HasMultipleVersions = datEntries.Count(e => e.GameTitle == entry.GameTitle) > 1,
                        AlternativeRegions = datEntries
                            .Where(e => e.GameTitle == entry.GameTitle && e.Region != entry.Region)
                            .Select(e => e.Region ?? "Unknown")
                            .Where(r => r != null)
                            .Cast<string>()
                            .ToList()
                    });
                }
            }

            return Result<MissingGameReport>.Success(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate missing game report");
            return Result<MissingGameReport>.Failure($"Report generation failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<List<RomRenameSuggestion>>> GetRenameSuggestionsAsync(
        Guid? platformId = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating rename suggestions");
            var reports = await _reportRepository.GetAllAsync(ct).ConfigureAwait(false);

            if (platformId.HasValue)
            {
                var platformRomIds = await _romRepository.GetIdsByPlatformAsync(platformId.Value, ct).ConfigureAwait(false);
                var platformRomIdSet = platformRomIds.ToHashSet();
                reports = reports.Where(r => platformRomIdSet.Contains(r.RomFileId)).ToList();
            }

            var suggestions = new List<RomRenameSuggestion>();

            foreach (var report in reports.Where(r => r.MatchResult?.IsMatchFound == true))
            {
                var rom = await _romRepository.GetByIdAsync(report.RomFileId, ct).ConfigureAwait(false);
                if (rom == null) continue;

                var matchedName = report.MatchResult!.MatchedEntry!.Name;
                var currentName = Path.GetFileNameWithoutExtension(rom.FilePath.Value);
                var suggestedName = SanitizeFileName(matchedName);

                if (!string.Equals(currentName, suggestedName, StringComparison.OrdinalIgnoreCase))
                {
                    suggestions.Add(new RomRenameSuggestion
                    {
                        RomFileId = (Guid)rom.Id,
                        CurrentName = Path.GetFileName(rom.FilePath.Value),
                        SuggestedName = suggestedName + Path.GetExtension(rom.FilePath.Value),
                        Reason = "Match DAT file entry",
                        SourceDat = report.MatchResult.SourceDat ?? "Unknown",
                        Confidence = report.MatchResult.Confidence
                    });
                }
            }

            return Result<List<RomRenameSuggestion>>.Success(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate rename suggestions");
            return Result<List<RomRenameSuggestion>>.Failure($"Rename suggestion failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<List<BadDumpInfo>>> IdentifyBadDumpsAsync(
        Guid? platformId = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Identifying bad dumps");
            var reports = await _reportRepository.GetAllAsync(ct).ConfigureAwait(false);
            var badDumps = new List<BadDumpInfo>();

            foreach (var report in reports.Where(r =>
                r.Status == ValidationStatus.BadDump ||
                r.MatchResult?.MatchedEntry?.DumpStatus == RomDumpStatus.Bad))
            {
                var rom = await _romRepository.GetByIdAsync(report.RomFileId, ct).ConfigureAwait(false);
                if (rom == null) continue;

                if (platformId.HasValue && rom.PlatformId != platformId.Value)
                    continue;

                var platform = await _romRepository.GetPlatformAsync(rom.PlatformId, ct).ConfigureAwait(false);

                var badDumpInfo = new BadDumpInfo
                {
                    RomFileId = (Guid)rom.Id,
                    FileName = Path.GetFileName(rom.FilePath.Value),
                    PlatformName = platform?.Name ?? "Unknown",
                    DumpStatus = report.MatchResult?.MatchedEntry?.DumpStatus ?? RomDumpStatus.Unknown,
                    IssueDescription = GetBadDumpDescription(report),
                    ExpectedHash = report.MatchResult?.MatchedEntry?.Sha1 ?? report.MatchResult?.MatchedEntry?.Md5,
                    ActualHash = report.HashInfo?.GetPrimaryHash(),
                    RecommendedAction = "Replace with verified good dump"
                };

                badDumps.Add(badDumpInfo);
            }

            return Result<List<BadDumpInfo>>.Success(badDumps);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to identify bad dumps");
            return Result<List<BadDumpInfo>>.Failure($"Bad dump identification failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<RomValidationStatistics>> GetStatisticsAsync(
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Getting ROM validation statistics");
            var roms = await _romRepository.GetAllAsync(ct).ConfigureAwait(false);
            var reports = await _reportRepository.GetAllAsync(ct).ConfigureAwait(false);
            var duplicates = await FindDuplicatesAsync(null, null, ct).ConfigureAwait(false);

            var stats = new RomValidationStatistics
            {
                TotalRoms = roms.Count(),
                ValidatedRoms = reports.Count(),
                VerifiedRoms = reports.Count(r => r.Status == ValidationStatus.Verified),
                BadDumps = reports.Count(r => r.Status == ValidationStatus.BadDump),
                CorruptedRoms = reports.Count(r => r.Status == ValidationStatus.Corrupted),
                DuplicateRoms = duplicates.IsSuccess ? duplicates.Value.Sum(d => d.Count - 1) : 0,
                DuplicateSpaceWasted = duplicates.IsSuccess ? duplicates.Value.Sum(d => d.WastedSpace) : 0,
                RomsByStatus = reports.GroupBy(r => r.Status).ToDictionary(g => g.Key, g => g.Count()),
                LastValidationRun = reports.Max(r => r.ValidatedAt)
            };

            var platforms = await _romRepository.GetAllPlatformsAsync(ct).ConfigureAwait(false);
            foreach (var platform in platforms)
            {
                var platformRoms = roms.Where(r => r.PlatformId == platform.Id).ToList();
                var platformReports = reports.Where(r => platformRoms.Any(pr => pr.Id.ToString() == r.RomFileId.ToString())).ToList();

                stats.PlatformStats[platform.Name] = new PlatformValidationStats
                {
                    PlatformId = (Guid)platform.Id,
                    PlatformName = platform.Name,
                    TotalRoms = platformRoms.Count,
                    ValidatedRoms = platformReports.Count,
                    VerifiedRoms = platformReports.Count(r => r.Status == ValidationStatus.Verified),
                    BadDumps = platformReports.Count(r => r.Status == ValidationStatus.BadDump)
                };
            }

            return Result<RomValidationStatistics>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get validation statistics");
            return Result<RomValidationStatistics>.Failure($"Statistics generation failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<string>> ExportValidationResultsAsync(
        RomValidationExportOptions options,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Exporting validation results to {OutputPath}", options.OutputPath);
            var reports = await _reportRepository.GetAllAsync(ct).ConfigureAwait(false);

            if (options.PlatformId.HasValue)
            {
                var platformRomIds = await _romRepository.GetIdsByPlatformAsync(options.PlatformId.Value, ct).ConfigureAwait(false);
                reports = reports.Where(r => platformRomIds.Contains(r.RomFileId)).ToList();
            }

            if (options.IncludeStatuses?.Any() == true)
            {
                reports = reports.Where(r => options.IncludeStatuses.Contains(r.Status)).ToList();
            }

            string content = options.Format switch
            {
                ValidationExportFormat.Json => ExportToJson(reports, options),
                ValidationExportFormat.Csv => ExportToCsv(reports, options),
                ValidationExportFormat.Html => ExportToHtml(reports, options),
                ValidationExportFormat.Markdown => ExportToMarkdown(reports, options),
                ValidationExportFormat.Dat => ExportToDat(reports, options),
                _ => throw new NotSupportedException($"Export format {options.Format} not supported")
            };

            await _fileSystem.WriteAllTextAsync(options.OutputPath, content, ct).ConfigureAwait(false);
            _logger.LogInformation("Validation results exported successfully to {OutputPath}", options.OutputPath);
            return Result<string>.Success(options.OutputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export validation results");
            return Result<string>.Failure($"Export failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<List<DatFileEntry>>> LoadDatFileAsync(
        string datFilePath,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Loading DAT file: {DatPath}", datFilePath);

            if (!await _fileSystem.FileExistsAsync(datFilePath, ct).ConfigureAwait(false))
            {
                return Result<List<DatFileEntry>>.Failure($"DAT file not found: {datFilePath}");
            }

            var extension = Path.GetExtension(datFilePath).ToLowerInvariant();
            var content = await _fileSystem.ReadAllTextAsync(datFilePath, ct).ConfigureAwait(false);

            List<DatFileEntry> entries = extension switch
            {
                ".xml" or ".dat" => ParseXmlDat(content, datFilePath),
                ".json" => ParseJsonDat(content, datFilePath),
                ".csv" => ParseCsvDat(content, datFilePath),
                _ => ParseXmlDat(content, datFilePath)
            };

            _logger.LogDebug("Loaded {EntryCount} entries from DAT file", entries.Count);
            return Result<List<DatFileEntry>>.Success(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load DAT file: {DatPath}", datFilePath);
            return Result<List<DatFileEntry>>.Failure($"DAT file loading failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<FileIntegrityResult>> VerifyFileIntegrityAsync(
        string filePath,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Verifying file integrity: {FilePath}", filePath);
            var result = new FileIntegrityResult();

            if (!await _fileSystem.FileExistsAsync(filePath, ct).ConfigureAwait(false))
            {
                result.IsIntact = false;
                result.ReadErrors.Add("File does not exist");
                return Result<FileIntegrityResult>.Success(result);
            }

            result.FileSize = await _fileSystem.GetFileSizeAsync(filePath, ct).ConfigureAwait(false);

            try
            {
                var bytes = await _fileSystem.ReadAllBytesAsync(filePath, ct).ConfigureAwait(false);
                result.IsReadable = bytes.Length == result.FileSize;
                result.HeaderInfo = AnalyzeRomHeader(bytes, Path.GetExtension(filePath));
                result.IsValidFormat = result.HeaderInfo?.IsValidHeader ?? true;
            }
            catch (Exception ex)
            {
                result.IsReadable = false;
                result.ReadErrors.Add($"Read error: {ex.Message}");
            }

            result.IsIntact = result.IsReadable && result.IsValidFormat;
            return Result<FileIntegrityResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify file integrity: {FilePath}", filePath);
            return Result<FileIntegrityResult>.Failure($"Integrity check failed: {ex.Message}");
        }
    }

    // Helper Methods

    private static string CalculateCrc32(byte[] data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (var b in data)
        {
            crc ^= b;
            for (int i = 0; i < 8; i++)
            {
                crc = (crc >> 1) ^ (0xEDB88320 & ~(crc & 1));
            }
        }
        return (~crc).ToString("X8").ToLowerInvariant();
    }

    private static bool IsPartialMatch(RomHashInfo hashInfo, DatFileEntry entry)
    {
        if (hashInfo.Crc32 != null && entry.Crc32 != null)
        {
            var hashPrefix = hashInfo.Crc32.Substring(0, Math.Min(4, hashInfo.Crc32.Length));
            var entryPrefix = entry.Crc32.Substring(0, Math.Min(4, entry.Crc32.Length));
            return hashPrefix.Equals(entryPrefix, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    private static string GetHashByType(RomHashInfo hashInfo, HashAlgorithmType type)
    {
        return type switch
        {
            HashAlgorithmType.Crc32 => hashInfo.Crc32 ?? string.Empty,
            HashAlgorithmType.Md5 => hashInfo.Md5 ?? string.Empty,
            HashAlgorithmType.Sha1 => hashInfo.Sha1 ?? string.Empty,
            HashAlgorithmType.Sha256 => hashInfo.Sha256 ?? string.Empty,
            _ => hashInfo.Sha1 ?? string.Empty
        };
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return sanitized.Trim();
    }

    private static string GetBadDumpDescription(RomValidationReport report)
    {
        if (report.MatchResult?.MatchedEntry?.DumpStatus != RomDumpStatus.Good)
        {
            return $"Identified as {report.MatchResult?.MatchedEntry?.DumpStatus} dump in DAT database";
        }
        var issue = report.Issues.FirstOrDefault(i => i.Category == IssueCategory.Database);
        return issue?.Message ?? "Unknown issue";
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "XML parsing requires multiple conditional checks")]
    private static List<DatFileEntry> ParseXmlDat(string content, string sourcePath)
    {
        var entries = new List<DatFileEntry>();
        try
        {
            var doc = XDocument.Parse(content);
            var header = doc.Root?.Element("header");
            var version = header?.Element("version")?.Value ?? header?.Element("date")?.Value ?? "Unknown";

            foreach (var game in doc.Root?.Elements("game") ?? doc.Root?.Elements("machine") ?? Enumerable.Empty<XElement>())
            {
                var rom = game.Element("rom");
                if (rom != null)
                {
                    entries.Add(new DatFileEntry
                    {
                        Name = game.Attribute("name")?.Value ?? "Unknown",
                        GameTitle = game.Element("description")?.Value,
                        Region = ExtractRegion(game.Attribute("name")?.Value),
                        Crc32 = rom.Attribute("crc")?.Value?.ToLowerInvariant(),
                        Md5 = rom.Attribute("md5")?.Value?.ToLowerInvariant(),
                        Sha1 = rom.Attribute("sha1")?.Value?.ToLowerInvariant(),
                        Size = long.TryParse(rom.Attribute("size")?.Value, out var size) ? size : 0,
                        SourceDat = Path.GetFileName(sourcePath),
                        DatVersion = version,
                        IsVerified = true,
                        CloneOf = game.Attribute("cloneof")?.Value,
                        DumpStatus = RomDumpStatus.Good
                    });
                }
            }
        }
        catch { /* If XML parsing fails, return empty list */ }
        return entries;
    }

    private static List<DatFileEntry> ParseJsonDat(string content, string sourcePath)
    {
        return new List<DatFileEntry>();
    }

    private static List<DatFileEntry> ParseCsvDat(string content, string sourcePath)
    {
        return new List<DatFileEntry>();
    }

    private static string? ExtractRegion(string? name)
    {
        if (string.IsNullOrEmpty(name)) return null;

        var regionPatterns = new Dictionary<string, string>
        {
            [@"\(USA\)"] = "USA",
            [@"\(Europe\)"] = "EUR",
            [@"\(Japan\)"] = "JPN",
            [@"\(World\)"] = "WLD"
        };

        foreach (var pattern in regionPatterns)
        {
            if (Regex.IsMatch(name, pattern.Key, RegexOptions.IgnoreCase))
                return pattern.Value;
        }
        return null;
    }

    private static RomHeaderInfo? AnalyzeRomHeader(byte[] data, string extension)
    {
        if (data.Length < 16) return null;
        var header = new RomHeaderInfo { HasHeader = false };

        if (extension == ".nes" && data[0] == 'N' && data[1] == 'E' && data[2] == 'S' && data[3] == 0x1A)
        {
            header.HasHeader = true;
            header.HeaderSize = 16;
            header.HeaderType = "iNES";
            header.IsValidHeader = true;
        }
        else if (extension == ".smc" && data.Length % 1024 == 512)
        {
            header.HasHeader = true;
            header.HeaderSize = 512;
            header.HeaderType = "SMC";
            header.IsValidHeader = true;
        }
        else if (extension == ".smd" && data.Length > 8 && data[8] == 0xAA && data[9] == 0xBB)
        {
            header.HasHeader = true;
            header.HeaderSize = 512;
            header.HeaderType = "SMD";
            header.IsValidHeader = true;
        }
        return header;
    }

    private static string ExportToJson(IEnumerable<RomValidationReport> reports, RomValidationExportOptions options)
    {
        var lines = new List<string> { "{" };
        lines.Add("  \"reports\": [");
        lines.AddRange(reports.Select(r => $"    {{ \"romId\": \"{r.RomFileId}\", \"status\": \"{r.Status}\" }},"));
        lines.Add("  ]");
        lines.Add("}");
        return string.Join("\n", lines);
    }

    private static string ExportToCsv(IEnumerable<RomValidationReport> reports, RomValidationExportOptions options)
    {
        var lines = new List<string> { "RomFileId,Status,ValidatedAt" };
        lines.AddRange(reports.Select(r => $"{r.RomFileId},{r.Status},{r.ValidatedAt:yyyy-MM-dd HH:mm:ss}"));
        return string.Join("\n", lines);
    }

    private static string ExportToHtml(IEnumerable<RomValidationReport> reports, RomValidationExportOptions options)
    {
        var html = "<!DOCTYPE html><html><head><title>ROM Validation Report</title></head><body>";
        html += "<h1>ROM Validation Report</h1><table border='1'><tr><th>ROM ID</th><th>Status</th><th>Validated At</th></tr>";
        foreach (var report in reports)
        {
            html += $"<tr><td>{report.RomFileId}</td><td>{report.Status}</td><td>{report.ValidatedAt}</td></tr>";
        }
        html += "</table></body></html>";
        return html;
    }

    private static string ExportToMarkdown(IEnumerable<RomValidationReport> reports, RomValidationExportOptions options)
    {
        var lines = new List<string> { "# ROM Validation Report", "", "| ROM ID | Status | Validated At |", "|--------|--------|-------------|" };
        lines.AddRange(reports.Select(r => $"| {r.RomFileId} | {r.Status} | {r.ValidatedAt:yyyy-MM-dd HH:mm:ss} |"));
        return string.Join("\n", lines);
    }

    private static string ExportToDat(IEnumerable<RomValidationReport> reports, RomValidationExportOptions options)
    {
        var xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<!DOCTYPE datafile>\n<datafile>\n  <header>\n    <name>Exported ROM Validation</name>\n    <description>ROM Validation Export</description>\n    <version>1.0</version>\n  </header>";
        foreach (var report in reports.Where(r => r.HashInfo != null))
        {
            xml += $"\n  <game name=\"ROM_{report.RomFileId}\">\n    <rom name=\"ROM_{report.RomFileId}.rom\" size=\"0\" crc=\"{report.HashInfo?.Crc32 ?? ""}\" md5=\"{report.HashInfo?.Md5 ?? ""}\" sha1=\"{report.HashInfo?.Sha1 ?? ""}\"/>\n  </game>";
        }
        xml += "\n</datafile>";
        return xml;
    }
}
