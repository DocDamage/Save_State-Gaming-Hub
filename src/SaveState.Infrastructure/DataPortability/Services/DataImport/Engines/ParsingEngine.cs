using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Core.DataPortability.Models;

namespace SaveState.Infrastructure.DataPortability.Services.DataImport.Engines;

/// <summary>
/// Implementation of parsing engine.
/// </summary>
public sealed class ParsingEngine : IParsingEngine
{
    private readonly ILogger<ParsingEngine> _logger;
    private readonly ITimeProvider _timeProvider;

    public ParsingEngine(ILogger<ParsingEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<ParsedData> ParseAsync(string filePath, ImportFormat format, CancellationToken ct = default)
    {
        try
        {
            var rawContent = await File.ReadAllTextAsync(filePath, ct);

            return format switch
            {
                ImportFormat.Json => ParseJson(rawContent, filePath),
                ImportFormat.Xml => ParseXml(rawContent, filePath),
                ImportFormat.Csv => ParseCsv(rawContent, filePath),
                _ => new ParsedData
                {
                    Format = ImportFormat.Unknown,
                    Errors = { new ParseError("Unsupported or unknown format") }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse file {FilePath}", filePath);
            return new ParsedData
            {
                Format = ImportFormat.Unknown,
                Errors = { new ParseError($"Parse error: {ex.Message}") }
            };
        }
    }

    public async Task<ParsedData> ParseBackupZipAsync(string backupPath, CancellationToken ct = default)
    {
        try
        {
            var sections = new Dictionary<string, JsonElement>();

            using var archive = ZipFile.OpenRead(backupPath);
            foreach (var entry in archive.Entries.Where(e => e.FullName.EndsWith(".json")))
            {
                await using var stream = entry.Open();
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
                sections[Path.GetFileNameWithoutExtension(entry.FullName)] = doc.RootElement.Clone();
            }

            // Look for manifest
            JsonElement? manifest = null;
            if (sections.TryGetValue("manifest", out var manifestElement))
            {
                manifest = manifestElement;
            }

            return new ParsedData
            {
                Format = ImportFormat.BackupZip,
                Sections = sections,
                RawContent = null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse backup ZIP {BackupPath}", backupPath);
            return new ParsedData
            {
                Format = ImportFormat.BackupZip,
                Errors = { new ParseError($"Backup parse error: {ex.Message}") }
            };
        }
    }

    private ParsedData ParseJson(string content, string filePath)
    {
        try
        {
            var document = JsonDocument.Parse(content);
            var sections = new Dictionary<string, JsonElement>();

            // Try to identify sections in the JSON
            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    sections[property.Name] = property.Value.Clone();
                }
            }

            return new ParsedData
            {
                Format = ImportFormat.Json,
                RootDocument = document,
                RawContent = content,
                Sections = sections
            };
        }
        catch (JsonException ex)
        {
            return new ParsedData
            {
                Format = ImportFormat.Json,
                RawContent = content,
                Errors = { new ParseError($"JSON parse error: {ex.Message}") }
            };
        }
    }

    private ParsedData ParseXml(string content, string filePath)
    {
        // Basic XML parsing - for now, treat as raw content
        return new ParsedData
        {
            Format = ImportFormat.Xml,
            RawContent = content,
            Sections = new Dictionary<string, JsonElement>()
        };
    }

    private ParsedData ParseCsv(string content, string filePath)
    {
        // Basic CSV parsing - for now, treat as raw content
        return new ParsedData
        {
            Format = ImportFormat.Csv,
            RawContent = content,
            Sections = new Dictionary<string, JsonElement>()
        };
    }
}
