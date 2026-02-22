using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Interfaces;
using SaveState.Core.Common.Services;
using SaveState.Core.RomManagement;
using SaveState.Core.RomManagement.Entities;
using SaveState.Core.RomManagement.RomValidation;
using SaveState.Core.RomManagement.RomValidation.Services;
using SaveState.Infrastructure.RomManagement.Validation;

namespace SaveState.Infrastructure.RomManagement;

/// <summary>
/// Coordinator implementation for ROM validation workflows.
/// </summary>
public class RomValidationService : IRomValidationService
{
    private readonly RomHashWorkflowManager _hashWorkflowManager;
    private readonly RomDatMatchingManager _datMatchingManager;
    private readonly RomValidationOrchestrationManager _orchestrationManager;
    private readonly RomValidationInsightsManager _insightsManager;

    public RomValidationService(
        IFileSystem fileSystem,
        IRomFileRepository romRepository,
        IRomHashInfoRepository hashRepository,
        IRomValidationReportRepository reportRepository,
        ILogger<RomValidationService> logger,
        ITimeProvider timeProvider)
    {
        _hashWorkflowManager = new RomHashWorkflowManager(fileSystem, hashRepository, logger);
        _datMatchingManager = new RomDatMatchingManager(fileSystem, logger);
        _orchestrationManager = new RomValidationOrchestrationManager(
            romRepository,
            hashRepository,
            reportRepository,
            logger,
            timeProvider,
            _hashWorkflowManager,
            _datMatchingManager);
        _insightsManager = new RomValidationInsightsManager(
            fileSystem,
            romRepository,
            hashRepository,
            reportRepository,
            logger,
            _datMatchingManager);
    }

    /// <inheritdoc />
    public async Task<Result<RomHashInfo>> CalculateHashesAsync(
        RomFile romFile,
        RomValidationOptions options,
        CancellationToken ct = default)
    {
        return await _hashWorkflowManager.CalculateHashesAsync(romFile, options, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<RomValidationReport>> ValidateRomAsync(
        RomFile romFile,
        RomValidationOptions options,
        CancellationToken ct = default)
    {
        return await _orchestrationManager.ValidateRomAsync(romFile, options, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<RomValidationJob>> ValidateBatchAsync(
        RomValidationJob job,
        RomValidationOptions options,
        IProgress<RomValidationProgress>? progress = null,
        CancellationToken ct = default)
    {
        return await _orchestrationManager.ValidateBatchAsync(job, options, progress, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<RomMatchResult>> MatchAgainstDatFilesAsync(
        RomHashInfo hashInfo,
        IEnumerable<string> datFilePaths,
        CancellationToken ct = default)
    {
        return await _datMatchingManager.MatchAgainstDatFilesAsync(hashInfo, datFilePaths, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<List<DuplicateRomInfo>>> FindDuplicatesAsync(
        Guid? platformId = null,
        HashAlgorithmType? hashType = null,
        CancellationToken ct = default)
    {
        return await _insightsManager.FindDuplicatesAsync(platformId, hashType, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<MissingGameReport>> GenerateMissingGameReportAsync(
        Guid platformId,
        string referenceDatPath,
        CancellationToken ct = default)
    {
        return await _insightsManager.GenerateMissingGameReportAsync(platformId, referenceDatPath, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<List<RomRenameSuggestion>>> GetRenameSuggestionsAsync(
        Guid? platformId = null,
        CancellationToken ct = default)
    {
        return await _insightsManager.GetRenameSuggestionsAsync(platformId, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<List<BadDumpInfo>>> IdentifyBadDumpsAsync(
        Guid? platformId = null,
        CancellationToken ct = default)
    {
        return await _insightsManager.IdentifyBadDumpsAsync(platformId, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<RomValidationStatistics>> GetStatisticsAsync(
        CancellationToken ct = default)
    {
        return await _insightsManager.GetStatisticsAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<string>> ExportValidationResultsAsync(
        RomValidationExportOptions options,
        CancellationToken ct = default)
    {
        return await _insightsManager.ExportValidationResultsAsync(options, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<List<DatFileEntry>>> LoadDatFileAsync(
        string datFilePath,
        CancellationToken ct = default)
    {
        return await _datMatchingManager.LoadDatFileAsync(datFilePath, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<FileIntegrityResult>> VerifyFileIntegrityAsync(
        string filePath,
        CancellationToken ct = default)
    {
        return await _hashWorkflowManager.VerifyFileIntegrityAsync(filePath, ct).ConfigureAwait(false);
    }
}
