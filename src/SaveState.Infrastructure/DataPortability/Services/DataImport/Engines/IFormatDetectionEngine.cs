using SaveState.Core.DataPortability.Models;

namespace SaveState.Infrastructure.DataPortability.Services.DataImport.Engines;

/// <summary>
/// Engine responsible for detecting the format of import files.
/// </summary>
public interface IFormatDetectionEngine
{
    /// <summary>
    /// Detects the format of an import file based on its content and extension.
    /// </summary>
    Task<ImportFormat> DetectFormatAsync(string filePath, CancellationToken ct = default);
}
