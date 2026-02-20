using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Services;
using SaveState.Infrastructure.GameLibrary.Models;

namespace SaveState.Infrastructure.GameLibrary.Services;

/// <summary>
/// Imports Cheat Engine table files (.CT) and converts them to memory signatures.
/// Supports both plain XML and compressed CT formats.
/// </summary>
public class CheatEngineTableImporter : ICheatEngineImporter
{
    private readonly ILogger<CheatEngineTableImporter> _logger;
    private readonly IMemoryPatternDatabase? _patternDatabase;

    public CheatEngineTableImporter(
        ILogger<CheatEngineTableImporter> logger,
        IMemoryPatternDatabase? patternDatabase = null)
    {
        _logger = logger;
        _patternDatabase = patternDatabase;
    }

    /// <inheritdoc />
    public bool CanParseFile(string filePath)
    {
        if (!File.Exists(filePath))
            return false;

        try
        {
            var content = ReadCtFileContent(filePath);
            if (string.IsNullOrWhiteSpace(content))
                return false;

            // Check for CheatTable XML element
            return content.Contains("<CheatTable", StringComparison.OrdinalIgnoreCase) ||
                   content.Contains("<?xml", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to check if file is parseable: {FilePath}", filePath);
            return false;
        }
    }

    /// <inheritdoc />
    public Result<CheatEngineImportResult> ImportFromFile(string filePath, CheatEngineImportOptions? options = null)
    {
        options ??= new CheatEngineImportOptions();

        if (!File.Exists(filePath))
        {
            return Result<CheatEngineImportResult>.Failure($"File not found: {filePath}", ErrorType.NotFound);
        }

        try
        {
            var content = ReadCtFileContent(filePath);
            if (string.IsNullOrWhiteSpace(content))
            {
                return Result<CheatEngineImportResult>.Failure("File is empty or could not be read", ErrorType.Validation);
            }

            var cheatTable = ParseCheatTable(content);
            if (cheatTable == null)
            {
                return Result<CheatEngineImportResult>.Failure("Failed to parse Cheat Engine table", ErrorType.Validation);
            }

            var result = new CheatEngineImportResult();
            var gameTitle = DetermineGameTitle(filePath, cheatTable, options);

            // Process all entries recursively
            var allEntries = FlattenEntries(cheatTable.CheatEntries);
            result.TotalEntries = allEntries.Count;

            foreach (var entry in allEntries)
            {
                ProcessEntry(entry, gameTitle, options, result);
            }

            result.ProcessedFiles.Add(filePath);

            _logger.LogInformation(
                "Imported Cheat Engine table '{File}': {Imported} imported, {Skipped} skipped, {Failed} failed",
                filePath, result.SuccessfullyImported, result.Skipped, result.Failed);

            return Result<CheatEngineImportResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing Cheat Engine table: {FilePath}", filePath);
            return Result<CheatEngineImportResult>.Failure($"Import failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public Result<CheatEngineImportResult> ImportFromDirectory(string directoryPath, bool recursive = false, CheatEngineImportOptions? options = null)
    {
        options ??= new CheatEngineImportOptions();

        if (!Directory.Exists(directoryPath))
        {
            return Result<CheatEngineImportResult>.Failure($"Directory not found: {directoryPath}", ErrorType.NotFound);
        }

        try
        {
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var ctFiles = Directory.GetFiles(directoryPath, "*.ct", searchOption);

            if (ctFiles.Length == 0)
            {
                return Result<CheatEngineImportResult>.Failure("No .CT files found in directory", ErrorType.NotFound);
            }

            var combinedResult = new CheatEngineImportResult
            {
                TotalEntries = 0,
                SuccessfullyImported = 0,
                Skipped = 0,
                Failed = 0
            };

            for (int i = 0; i < ctFiles.Length; i++)
            {
                var file = ctFiles[i];
                options.ProgressCallback?.Invoke(new CheatEngineImportProgress
                {
                    CurrentFile = file,
                    CurrentFileIndex = i + 1,
                    TotalFiles = ctFiles.Length,
                    StatusMessage = $"Processing {Path.GetFileName(file)}..."
                });

                var fileResult = ImportFromFile(file, options);
                if (fileResult.IsSuccess && fileResult.Value != null)
                {
                    var r = fileResult.Value;
                    combinedResult.TotalEntries += r.TotalEntries;
                    combinedResult.SuccessfullyImported += r.SuccessfullyImported;
                    combinedResult.Skipped += r.Skipped;
                    combinedResult.Failed += r.Failed;
                    combinedResult.ImportedSignatures.AddRange(r.ImportedSignatures);
                    combinedResult.Errors.AddRange(r.Errors);
                    combinedResult.SkippedEntries.AddRange(r.SkippedEntries);
                    combinedResult.ProcessedFiles.AddRange(r.ProcessedFiles);
                }
                else
                {
                    combinedResult.Failed++;
                    combinedResult.Errors.Add(new ImportError
                    {
                        EntryName = Path.GetFileName(file),
                        Message = fileResult.Error ?? "Unknown error",
                        ErrorType = ImportErrorType.FileError
                    });
                }
            }

            return Result<CheatEngineImportResult>.Success(combinedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing from directory: {DirectoryPath}", directoryPath);
            return Result<CheatEngineImportResult>.Failure($"Directory import failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public Result<CheatEngineTablePreview> PreviewFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return Result<CheatEngineTablePreview>.Failure($"File not found: {filePath}", ErrorType.NotFound);
        }

        try
        {
            var content = ReadCtFileContent(filePath);
            var cheatTable = ParseCheatTable(content);

            if (cheatTable == null)
            {
                return Result<CheatEngineTablePreview>.Failure("Failed to parse Cheat Engine table", ErrorType.Validation);
            }

            var preview = new CheatEngineTablePreview
            {
                FilePath = filePath,
                GameTitle = ExtractGameTitleFromFileName(filePath),
                IsCompressed = IsCompressedCtFile(filePath)
            };

            var allEntries = FlattenEntries(cheatTable.CheatEntries);

            foreach (var entry in allEntries)
            {
                var entryPreview = CreateEntryPreview(entry);
                preview.Entries.Add(entryPreview);

                if (entryPreview.IsScript)
                {
                    preview.HasScripts = true;
                    preview.ScriptCount++;
                }
            }

            return Result<CheatEngineTablePreview>.Success(preview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing Cheat Engine table: {FilePath}", filePath);
            return Result<CheatEngineTablePreview>.Failure($"Preview failed: {ex.Message}", ErrorType.Internal);
        }
    }

    private string ReadCtFileContent(string filePath)
    {
        // Check if file is compressed (some CT files are gzip compressed)
        if (IsCompressedCtFile(filePath))
        {
            return ReadCompressedCtFile(filePath);
        }

        return File.ReadAllText(filePath, Encoding.UTF8);
    }

    private bool IsCompressedCtFile(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            var header = new byte[2];
            _ = stream.Read(header, 0, 2);
            // GZip magic number: 0x1f 0x8b
            return header[0] == 0x1f && header[1] == 0x8b;
        }
        catch
        {
            return false;
        }
    }

    private string ReadCompressedCtFile(string filePath)
    {
        using var fileStream = File.OpenRead(filePath);
        using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
        using var reader = new StreamReader(gzipStream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private CheatTable? ParseCheatTable(string content)
    {
        try
        {
            var serializer = new XmlSerializer(typeof(CheatTable));
            using var reader = new StringReader(content);
            
            // Use secure XML reading settings
            using var xmlReader = XmlReader.Create(reader, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            });
            
            return serializer.Deserialize(xmlReader) as CheatTable;
        }
        catch (InvalidOperationException)
        {
            // Try with different XML settings for malformed XML
            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(content);
                return ConvertXmlDocumentToCheatTable(xmlDoc);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse CheatTable XML with fallback method");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse CheatTable XML");
            return null;
        }
    }

    private CheatTable ConvertXmlDocumentToCheatTable(XmlDocument xmlDoc)
    {
        var table = new CheatTable();
        var root = xmlDoc.DocumentElement;

        if (root?.SelectSingleNode("CheatEntries") is XmlNode entriesNode)
        {
            foreach (XmlNode entryNode in entriesNode.SelectNodes("CheatEntry")!)
            {
                var entry = ParseEntryNode(entryNode);
                if (entry != null)
                    table.CheatEntries.Add(entry);
            }
        }

        return table;
    }

    private CheatEntry? ParseEntryNode(XmlNode node)
    {
        var entry = new CheatEntry();

        var idNode = node.SelectSingleNode("ID");
        if (idNode != null && int.TryParse(idNode.InnerText, out var id))
            entry.Id = id;

        var descNode = node.SelectSingleNode("Description");
        if (descNode != null)
            entry.Description = descNode.InnerText;

        var addrNode = node.SelectSingleNode("Address");
        if (addrNode != null)
            entry.Address = addrNode.InnerText;

        var typeNode = node.SelectSingleNode("VariableType");
        if (typeNode != null)
            entry.VariableType = typeNode.InnerText;

        var offsetsNode = node.SelectSingleNode("Offsets");
        if (offsetsNode != null)
        {
            entry.Offsets = new List<string>();
            foreach (XmlNode offsetNode in offsetsNode.SelectNodes("Offset")!)
            {
                entry.Offsets.Add(offsetNode.InnerText);
            }
        }

        var luaNode = node.SelectSingleNode("LuaScript");
        if (luaNode != null)
            entry.LuaScript = luaNode.InnerText;

        var asmNode = node.SelectSingleNode("AssemblerScript");
        if (asmNode != null)
            entry.AssemblerScript = asmNode.InnerText;

        // Parse nested entries recursively
        var nestedEntriesNode = node.SelectSingleNode("CheatEntries");
        if (nestedEntriesNode != null)
        {
            entry.CheatEntries = new List<CheatEntry>();
            foreach (XmlNode nestedNode in nestedEntriesNode.SelectNodes("CheatEntry")!)
            {
                var nested = ParseEntryNode(nestedNode);
                if (nested != null)
                    entry.CheatEntries.Add(nested);
            }
        }

        return entry;
    }

    private List<CheatEntry> FlattenEntries(List<CheatEntry> entries)
    {
        var result = new List<CheatEntry>();

        foreach (var entry in entries)
        {
            // Skip group headers - they don't have actual memory addresses
            if (!entry.GroupHeader && !string.IsNullOrEmpty(entry.Address))
            {
                result.Add(entry);
            }

            // Process nested entries recursively
            if (entry.CheatEntries != null)
            {
                result.AddRange(FlattenEntries(entry.CheatEntries));
            }
        }

        return result;
    }

    private void ProcessEntry(CheatEntry entry, string gameTitle, CheatEngineImportOptions options, CheatEngineImportResult result)
    {
        try
        {
            // Check if this is a script entry
            if (entry.IsScript)
            {
                if (!options.IncludeScripts)
                {
                    result.Skipped++;
                    result.SkippedEntries.Add(new SkippedEntry
                    {
                        EntryName = entry.GetDisplayName(),
                        Reason = "Script entries excluded by options"
                    });
                    return;
                }

                // Scripts are imported with a warning flag
                _logger.LogWarning("Importing script entry '{Entry}' - scripts require manual review", entry.GetDisplayName());
            }

            // Check variable type support
            var internalType = VariableTypeMappings.GetInternalType(entry.VariableType ?? "4 Bytes");
            if (internalType == null)
            {
                result.Skipped++;
                result.SkippedEntries.Add(new SkippedEntry
                {
                    EntryName = entry.GetDisplayName(),
                    Reason = $"Unsupported variable type: {entry.VariableType}"
                });
                return;
            }

            // Check for duplicates if database is available
            if (options.SkipDuplicates && _patternDatabase != null)
            {
                var existing = _patternDatabase.GetSignaturesForGame(gameTitle);
                if (existing.IsSuccess && existing.Value.Any(s => s.Name == entry.GetDisplayName()))
                {
                    if (!options.OverwriteExisting)
                    {
                        result.Skipped++;
                        result.SkippedEntries.Add(new SkippedEntry
                        {
                            EntryName = entry.GetDisplayName(),
                            Reason = "Duplicate entry (already exists)"
                        });
                        return;
                    }
                }
            }

            // Convert to GameMemorySignature
            var signature = ConvertToSignature(entry, gameTitle, options);
            if (signature == null)
            {
                result.Failed++;
                result.Errors.Add(new ImportError
                {
                    EntryName = entry.GetDisplayName(),
                    Message = "Failed to convert entry to signature",
                    ErrorType = ImportErrorType.ConversionError
                });
                return;
            }

            // Add to database if available
            if (_patternDatabase != null)
            {
                var addResult = _patternDatabase.AddSignature(signature);
                if (addResult.IsFailure)
                {
                    result.Failed++;
                    result.Errors.Add(new ImportError
                    {
                        EntryName = entry.GetDisplayName(),
                        Message = addResult.Error ?? "Failed to add to database",
                        ErrorType = ImportErrorType.ValidationError
                    });
                    return;
                }
            }

            result.SuccessfullyImported++;
            result.ImportedSignatures.Add(signature);
        }
        catch (Exception ex)
        {
            result.Failed++;
            result.Errors.Add(new ImportError
            {
                EntryName = entry.GetDisplayName(),
                Message = ex.Message,
                ErrorType = ImportErrorType.ParseError,
                ExceptionDetails = ex.ToString()
            });
        }
    }

    private GameMemorySignature? ConvertToSignature(CheatEntry entry, string gameTitle, CheatEngineImportOptions options)
    {
        var parsedAddress = ParsedAddress.Parse(entry.Address ?? "");
        var internalType = VariableTypeMappings.GetInternalType(entry.VariableType ?? "4 Bytes");

        if (internalType == null)
            return null;

        // Create a pattern from the address
        // For module+offset: we'll store the module name and create a placeholder pattern
        // The actual pattern matching will need to be resolved at runtime
        string pattern;
        int offset;

        if (parsedAddress.IsPointer)
        {
            // For pointers, we store the base address pattern
            // The actual pointer chain resolution happens at scan time
            pattern = $"{parsedAddress.BaseAddress:X8}";
            offset = 0;
        }
        else
        {
            pattern = $"{parsedAddress.Offset:X8}";
            offset = 0;
        }

        var signature = new GameMemorySignature
        {
            GameTitle = gameTitle,
            Name = entry.GetDisplayName(),
            Pattern = pattern,
            Offset = offset,
            ValueType = internalType,
            Description = $"Imported from Cheat Engine: {entry.Description}",
            ModuleName = parsedAddress.ModuleName,
            IsEnabled = true,
            Tags = new List<string>(options.DefaultTags),
            CreatedAt = DateTime.UtcNow
        };

        // Add special tags for script entries
        if (entry.IsScript)
        {
            signature.Tags.Add("script");
            signature.Tags.Add("requires-review");
        }

        // Add pointer tag
        if (parsedAddress.IsPointer)
        {
            signature.Tags.Add("pointer");
            // Store pointer offsets in description since we don't have a dedicated field
            signature.Description += $" | Pointer chain: {string.Join(" -> ", parsedAddress.PointerOffsets.Select(o => $"0x{o:X}"))}";
        }

        return signature;
    }

    private CheatEngineEntryPreview CreateEntryPreview(CheatEntry entry)
    {
        var parsedAddress = ParsedAddress.Parse(entry.Address ?? "");
        var internalType = VariableTypeMappings.GetInternalType(entry.VariableType ?? "4 Bytes");

        var preview = new CheatEngineEntryPreview
        {
            Description = entry.GetDisplayName(),
            Address = entry.Address ?? "",
            VariableType = entry.VariableType ?? "Unknown",
            IsPointer = parsedAddress.IsPointer,
            IsScript = entry.IsScript,
            CanImport = internalType != null && !entry.GroupHeader,
            ConvertedValueType = internalType
        };

        if (internalType == null)
        {
            preview.ImportRestriction = $"Unsupported type: {entry.VariableType}";
        }
        else if (entry.IsScript)
        {
            preview.ImportRestriction = "Script entry - requires manual review";
        }

        return preview;
    }

    private string DetermineGameTitle(string filePath, CheatTable table, CheatEngineImportOptions options)
    {
        // Use explicit game title if provided
        if (!string.IsNullOrWhiteSpace(options.GameTitle))
            return options.GameTitle;

        // Try to extract from filename
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        
        // Common patterns: "GameName.ct", "GameName_v1.0.ct", "GameName_Cheats.ct"
        fileName = Regex.Replace(fileName, @"_+(cheats?|table|v?\d+\.?\d*).*$", "", RegexOptions.IgnoreCase);
        
        if (!string.IsNullOrWhiteSpace(fileName))
            return fileName;

        return "Unknown Game";
    }

    private string? ExtractGameTitleFromFileName(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        
        // Remove common suffixes
        fileName = Regex.Replace(fileName, @"_+(cheats?|table|v?\d+\.?\d*).*$", "", RegexOptions.IgnoreCase);
        
        return string.IsNullOrWhiteSpace(fileName) ? null : fileName;
    }
}
