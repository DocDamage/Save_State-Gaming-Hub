using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Interfaces;
using SaveState.Core.RomManagement;
using SaveState.Core.RomManagement.RomValidation;

namespace SaveState.Infrastructure.RomManagement.Validation;

internal sealed class RomDatMatchingManager
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<RomValidationService> _logger;

    public RomDatMatchingManager(
        IFileSystem fileSystem,
        ILogger<RomValidationService> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

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

                var alternatives = entries.Where(e => RomHashCalculationManager.IsPartialMatch(hashInfo, e)).ToList();
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
                ".xml" or ".dat" => RomValidationDatParser.ParseXmlDat(content, datFilePath),
                ".json" => RomValidationDatParser.ParseJsonDat(content, datFilePath),
                ".csv" => RomValidationDatParser.ParseCsvDat(content, datFilePath),
                _ => RomValidationDatParser.ParseXmlDat(content, datFilePath)
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
}
