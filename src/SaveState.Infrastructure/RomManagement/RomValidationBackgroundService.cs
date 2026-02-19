using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Application.RomManagement.RomValidation.Commands;
using SaveState.Core.Configuration;
using SaveState.Core.RomManagement;
using SaveState.Core.RomManagement.RomValidation;
using SaveState.Core.RomManagement.RomValidation.Services;

namespace SaveState.Infrastructure.RomManagement;

/// <summary>
/// Background service for automatic ROM validation.
/// Validates ROMs on import and performs periodic re-validation.
/// </summary>
public class RomValidationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RomValidationBackgroundService> _logger;
    private readonly IOptions<RomValidationOptions> _options;

    public RomValidationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<RomValidationBackgroundService> logger,
        IOptions<RomValidationOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ROM Validation Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformScheduledValidationAsync(stoppingToken);

                // Wait for the next scheduled interval (e.g., 24 hours)
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("ROM Validation Background Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ROM Validation Background Service");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    /// <summary>
    /// Performs scheduled validation of unvalidated ROMs.
    /// </summary>
    private async Task PerformScheduledValidationAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting scheduled ROM validation");

        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
        var romRepository = scope.ServiceProvider.GetRequiredService<IRomFileRepository>();
        var validationService = scope.ServiceProvider.GetRequiredService<IRomValidationService>();

        try
        {
            // Get ROMs that need validation (e.g., newly added or not validated recently)
            var allRoms = await romRepository.GetAllAsync(ct);
            var unvalidatedRoms = allRoms.Where(r => string.IsNullOrEmpty(r.Hash)).ToList();

            if (!unvalidatedRoms.Any())
            {
                _logger.LogInformation("No unvalidated ROMs found");
                return;
            }

            _logger.LogInformation("Found {Count} unvalidated ROMs", unvalidatedRoms.Count);

            // Batch validate with default options
            var options = new RomValidationOptions
            {
                CalculateCrc32 = true,
                CalculateMd5 = true,
                CalculateSha1 = true,
                CalculateSha256 = false,
                MatchAgainstDatFiles = false,
                SkipValidated = true
            };

            var job = new RomValidationJob
            {
                Name = "Scheduled Validation",
                RomFileIds = unvalidatedRoms.Select(r => (Guid)r.Id).ToList(),
                TotalRoms = unvalidatedRoms.Count
            };

            var progress = new Progress<RomValidationProgress>(p =>
            {
                _logger.LogDebug(
                    "Validation progress: {Processed}/{Total} - {CurrentFile}",
                    p.ProcessedCount,
                    p.TotalCount,
                    p.CurrentFile);
            });

            var result = await validationService.ValidateBatchAsync(job, options, progress, ct);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Scheduled validation completed: {Processed}/{Total} ROMs validated",
                    result.Value.ProcessedRoms,
                    result.Value.TotalRoms);

                // Log any errors
                foreach (var error in result.Value.Errors)
                {
                    _logger.LogWarning("Validation error: {Error}", error);
                }
            }
            else
            {
                _logger.LogError("Scheduled validation failed: {Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during scheduled validation");
        }
    }

    /// <summary>
    /// Validates a ROM file immediately (called when a new ROM is imported).
    /// </summary>
    public async Task ValidateRomOnImportAsync(Guid romFileId, CancellationToken ct = default)
    {
        _logger.LogInformation("Validating ROM on import: {RomFileId}", romFileId);

        using var scope = _serviceProvider.CreateScope();
        var validationService = scope.ServiceProvider.GetRequiredService<IRomValidationService>();
        var romRepository = scope.ServiceProvider.GetRequiredService<IRomFileRepository>();

        try
        {
            var rom = await romRepository.GetByIdAsync(romFileId, ct);
            if (rom == null)
            {
                _logger.LogWarning("ROM not found for validation: {RomFileId}", romFileId);
                return;
            }

            // Quick validation with basic hashes
            var options = new RomValidationOptions
            {
                CalculateCrc32 = true,
                CalculateMd5 = true,
                CalculateSha1 = true,
                CalculateSha256 = false,
                MatchAgainstDatFiles = false
            };

            var result = await validationService.ValidateRomAsync(rom, options, ct);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "ROM validation on import completed: {RomFileId} - {Status}",
                    romFileId,
                    result.Value.Status);
            }
            else
            {
                _logger.LogWarning(
                    "ROM validation on import failed: {RomFileId} - {Error}",
                    romFileId,
                    result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating ROM on import: {RomFileId}", romFileId);
        }
    }

    /// <summary>
    /// Performs duplicate scan across the library.
    /// </summary>
    public async Task ScanForDuplicatesAsync(Guid? platformId = null, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting duplicate scan");

        using var scope = _serviceProvider.CreateScope();
        var validationService = scope.ServiceProvider.GetRequiredService<IRomValidationService>();

        try
        {
            var result = await validationService.FindDuplicatesAsync(platformId, HashAlgorithmType.Sha1, ct);

            if (result.IsSuccess)
            {
                var duplicates = result.Value;
                var totalWasted = duplicates.Sum(d => d.WastedSpace);

                _logger.LogInformation(
                    "Duplicate scan completed: {Count} sets found, {WastedMB:F1} MB wasted",
                    duplicates.Count,
                    totalWasted / (1024.0 * 1024));

                // Could trigger notifications here
            }
            else
            {
                _logger.LogError("Duplicate scan failed: {Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during duplicate scan");
        }
    }
}

/// <summary>
/// Configuration options for ROM validation background service.
/// </summary>
public class RomValidationBackgroundOptions
{
    /// <summary>
    /// Enable automatic validation on ROM import.
    /// </summary>
    public bool ValidateOnImport { get; set; } = true;

    /// <summary>
    /// Enable scheduled re-validation.
    /// </summary>
    public bool EnableScheduledValidation { get; set; } = true;

    /// <summary>
    /// Interval for scheduled validation (hours).
    /// </summary>
    public int ValidationIntervalHours { get; set; } = 24;

    /// <summary>
    /// Enable automatic duplicate scanning.
    /// </summary>
    public bool EnableDuplicateScanning { get; set; } = true;

    /// <summary>
    /// Interval for duplicate scanning (hours).
    /// </summary>
    public int DuplicateScanIntervalHours { get; set; } = 168; // Weekly
}
