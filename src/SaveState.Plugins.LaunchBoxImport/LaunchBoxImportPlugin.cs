using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Plugins;
using System.Xml.Linq;

namespace SaveState.Plugins.LaunchBoxImport;

public sealed class LaunchBoxImportPlugin : IPlugin, IImporter
{
    private IPluginContext? _context;

    public string Id => "launchbox-importer";
    public string Name => "LaunchBox Importer";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Import games from local LaunchBox XML files.";
    public PluginCapabilities Capabilities => PluginCapabilities.Importer;

    public string ImporterName => "LaunchBox";
    public string DisplayName => "LaunchBox";
    public IReadOnlyList<string> SupportedApplications => new[] { "LaunchBox" };

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _context.Logger.LogInformation("LaunchBox Importer initialized");
        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken ct = default) => Task.CompletedTask;

    public Task<Result<ImportAnalysis>> AnalyzeImportAsync(string filePath, CancellationToken ct = default)
    {
        try
        {
            var dataFolder = filePath;
            if (string.IsNullOrEmpty(dataFolder) || !Directory.Exists(dataFolder))
            {
                // Try to find default location relative to typical install paths if simpler
                // For LaunchBox, user usually picks the folder.
                return Task.FromResult(Result.Failure<ImportAnalysis>("LaunchBox Data folder not specified or found."));
            }

            // Look for Platforms/*.xml
            var platformsFolder = Path.Combine(dataFolder, "Platforms");
            if (!Directory.Exists(platformsFolder))
            {
                 // Maybe they pointed to the root LaunchBox folder
                 platformsFolder = Path.Combine(dataFolder, "Data", "Platforms");
                 if (!Directory.Exists(platformsFolder))
                    return Task.FromResult(Result.Failure<ImportAnalysis>("Could not find Data/Platforms folder."));
            }

            var xmlFiles = Directory.GetFiles(platformsFolder, "*.xml");
            var gameCount = 0;

            foreach (var file in xmlFiles)
            {
                // Quick read to count 'Game' elements
                // We use XDocument for simplicity
                try
                {
                    // To avoid loading massive files, could use XmlReader, but XDocument is easier for prototype
                    using var stream = File.OpenRead(file);
                    var doc = XDocument.Load(stream);
                    gameCount += doc.Descendants("Game").Count();
                }
                catch (Exception ex)
                {
                    var logger = _context?.Logger;
                    if (logger?.IsEnabled(LogLevel.Warning) == true)
                    {
                        logger.LogWarning(ex, "Failed to parse LaunchBox XML file: {File}", file);
                    }
                }
            }

            var analysis = new ImportAnalysis(
                GamesCount: gameCount,
                CollectionsCount: xmlFiles.Length, // Treat platforms as collections
                PlaytimeRecordsCount: 0,
                Warnings: new List<string>()
            );

            return Task.FromResult(Result.Success(analysis));
        }
        catch (Exception ex)
        {
            _context?.Logger.LogError(ex, "Failed to analyze LaunchBox data");
            return Task.FromResult(Result.Failure<ImportAnalysis>(ex.Message));
        }
    }

    public async Task<Result<ImportResult>> ImportAsync(string filePath, ImportOptions options, CancellationToken ct = default)
    {
        await Task.Yield(); // Ensure async context
        try
        {
            var dataFolder = filePath;
             var platformsFolder = Path.Combine(dataFolder, "Platforms");
            if (!Directory.Exists(platformsFolder))
            {
                 platformsFolder = Path.Combine(dataFolder, "Data", "Platforms");
            }

            if (!Directory.Exists(platformsFolder))
                 return Result.Failure<ImportResult>("Platforms folder not found");

            var importedCount = 0;
            var errors = new List<string>();
            var xmlFiles = Directory.GetFiles(platformsFolder, "*.xml");

            foreach (var file in xmlFiles)
            {
                try
                {
                    var doc = XDocument.Load(file);
                    var games = doc.Descendants("Game");
                    foreach (var game in games)
                    {
                        var title = game.Element("Title")?.Value;
                        if (!string.IsNullOrWhiteSpace(title))
                        {
                            importedCount++;
                        }
                    }
                    _context?.ReportProgress($"Processed {Path.GetFileNameWithoutExtension(file)}", 0);
                }
                catch (Exception ex)
                {
                   errors.Add($"Failed to parse {Path.GetFileName(file)}: {ex.Message}");
                }
            }

            return Result.Success(new ImportResult(importedCount, xmlFiles.Length, 0, errors));
        }
        catch (Exception ex)
        {
            _context?.Logger.LogError(ex, "Failed to import from LaunchBox");
            return Result.Failure<ImportResult>(ex.Message);
        }
    }
}
