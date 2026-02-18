using System.IO.Compression;
using System.Text.Json;
using SaveState.Core.DataPortability.Models;

namespace SaveState.Infrastructure.DataPortability.Services.DataImport.Engines;

/// <summary>
/// Engine responsible for parsing import files into structured data.
/// </summary>
public interface IParsingEngine
{
    /// <summary>
    /// Parses an import file based on its detected format.
    /// </summary>
    Task<ParsedData> ParseAsync(string filePath, ImportFormat format, CancellationToken ct = default);

    /// <summary>
    /// Parses a backup ZIP file and extracts all contained sections.
    /// </summary>
    Task<ParsedData> ParseBackupZipAsync(string backupPath, CancellationToken ct = default);
}
