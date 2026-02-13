using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace SaveState.Infrastructure.Common.Services;

/// <summary>
/// Implementation of IExtractionService using SharpCompress library.
/// Supports .zip, .7z, .rar, .tar, .gz, etc.
/// </summary>
public class SharpCompressExtractionService : IExtractionService
{
    private readonly ILogger<SharpCompressExtractionService> _logger;

    public SharpCompressExtractionService(ILogger<SharpCompressExtractionService> logger)
    {
        _logger = logger;
    }

    public async Task<Result> ExtractAsync(
        string archivePath,
        string destinationDirectory,
        bool overwrite = true,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(archivePath))
            {
                return Result.Failure($"Archive file not found: {archivePath}");
            }

            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            _logger.LogInformation("Extracting archive {ArchivePath} to {DestinationDirectory}", archivePath, destinationDirectory);

            await Task.Run(() =>
            {
                using var archive = ArchiveFactory.Open(archivePath);

                // For progress reporting, we need to know the total number of entries or total size
                // Calculating this can be expensive for some archive types, but let's try for better UX
                var totalEntries = archive.Entries.Count();
                var processedEntries = 0;

                var extractionOptions = new ExtractionOptions
                {
                    ExtractFullPath = true,
                    Overwrite = overwrite
                };

                foreach (var entry in archive.Entries)
                {
                    if (ct.IsCancellationRequested) break;

                    if (!entry.IsDirectory)
                    {
                        entry.WriteToDirectory(destinationDirectory, extractionOptions);
                    }

                    processedEntries++;
                    progress?.Report((double)processedEntries / totalEntries);
                }
            }, ct);

            if (ct.IsCancellationRequested)
            {
                _logger.LogWarning("Extraction cancelled for {ArchivePath}", archivePath);
                return Result.Failure("Extraction cancelled");
            }

            _logger.LogInformation("Successfully extracted archive {ArchivePath}", archivePath);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract archive {ArchivePath}", archivePath);
            return Result.Failure($"Extraction failed: {ex.Message}");
        }
    }

    public bool SupportsExtension(string extension)
    {
        if (string.IsNullOrEmpty(extension)) return false;

        var ext = extension.ToLowerInvariant();
        if (!ext.StartsWith(".")) ext = "." + ext;

        return ext switch
        {
            ".zip" or ".7z" or ".rar" or ".tar" or ".gz" or ".bz2" or ".lz" => true,
            _ => false
        };
    }
}
