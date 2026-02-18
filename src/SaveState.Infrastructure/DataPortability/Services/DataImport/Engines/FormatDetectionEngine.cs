using SaveState.Core.DataPortability.Models;

namespace SaveState.Infrastructure.DataPortability.Services.DataImport.Engines;

/// <summary>
/// Implementation of format detection engine.
/// </summary>
public sealed class FormatDetectionEngine : IFormatDetectionEngine
{
    public async Task<ImportFormat> DetectFormatAsync(string filePath, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        var format = extension switch
        {
            ".json" => ImportFormat.Json,
            ".xml" => ImportFormat.Xml,
            ".csv" => ImportFormat.Csv,
            ".zip" => ImportFormat.BackupZip,
            _ => ImportFormat.Unknown
        };

        // Additional content-based detection for ambiguous formats
        if (format == ImportFormat.Unknown)
        {
            try
            {
                var content = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);
                if (content.TrimStart().StartsWith('{') || content.TrimStart().StartsWith('['))
                {
                    format = ImportFormat.Json;
                }
                else if (content.TrimStart().StartsWith('<'))
                {
                    format = ImportFormat.Xml;
                }
            }
            catch
            {
                // Ignore detection errors
            }
        }

        return format;
    }
}
