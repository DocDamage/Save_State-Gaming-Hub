using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Interfaces;
using SaveState.Core.RomManagement;
using SaveState.Core.RomManagement.RomValidation;
using SaveState.Core.RomManagement.RomValidation.Services;

namespace SaveState.Infrastructure.RomManagement.Validation;

internal sealed class RomValidationInsightsManager
{
    private readonly IFileSystem _fileSystem;
    private readonly IRomFileRepository _romRepository;
    private readonly IRomHashInfoRepository _hashRepository;
    private readonly IRomValidationReportRepository _reportRepository;
    private readonly ILogger<RomValidationService> _logger;
    private readonly RomDatMatchingManager _datMatchingManager;

    public RomValidationInsightsManager(
        IFileSystem fileSystem,
        IRomFileRepository romRepository,
        IRomHashInfoRepository hashRepository,
        IRomValidationReportRepository reportRepository,
        ILogger<RomValidationService> logger,
        RomDatMatchingManager datMatchingManager)
    {
        _fileSystem = fileSystem;
        _romRepository = romRepository;
        _hashRepository = hashRepository;
        _reportRepository = reportRepository;
        _logger = logger;
        _datMatchingManager = datMatchingManager;
    }

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
                var hash = RomHashCalculationManager.GetHashByType(hashInfo, hashProperty);
                if (!string.IsNullOrEmpty(hash))
                {
                    if (!grouped.ContainsKey(hash))
                    {
                        grouped[hash] = new List<RomHashInfo>();
                    }

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

            var datResult = await _datMatchingManager.LoadDatFileAsync(referenceDatPath, ct).ConfigureAwait(false);
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
                if (rom == null)
                {
                    continue;
                }

                var matchedName = report.MatchResult!.MatchedEntry!.Name;
                var currentName = Path.GetFileNameWithoutExtension(rom.FilePath.Value);
                var suggestedName = RomValidationNamingHelper.SanitizeFileName(matchedName);

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
                if (rom == null)
                {
                    continue;
                }

                if (platformId.HasValue && rom.PlatformId != platformId.Value)
                {
                    continue;
                }

                var platform = await _romRepository.GetPlatformAsync(rom.PlatformId, ct).ConfigureAwait(false);

                var badDumpInfo = new BadDumpInfo
                {
                    RomFileId = (Guid)rom.Id,
                    FileName = Path.GetFileName(rom.FilePath.Value),
                    PlatformName = platform?.Name ?? "Unknown",
                    DumpStatus = report.MatchResult?.MatchedEntry?.DumpStatus ?? RomDumpStatus.Unknown,
                    IssueDescription = RomValidationNamingHelper.GetBadDumpDescription(report),
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
                ValidationExportFormat.Json => RomValidationExportManager.ExportToJson(reports, options),
                ValidationExportFormat.Csv => RomValidationExportManager.ExportToCsv(reports, options),
                ValidationExportFormat.Html => RomValidationExportManager.ExportToHtml(reports, options),
                ValidationExportFormat.Markdown => RomValidationExportManager.ExportToMarkdown(reports, options),
                ValidationExportFormat.Dat => RomValidationExportManager.ExportToDat(reports, options),
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
}
