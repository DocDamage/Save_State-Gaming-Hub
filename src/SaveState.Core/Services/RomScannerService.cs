using SaveState.Core.Entities;
using SaveState.Core.Models;
using Serilog;

namespace SaveState.Core.Services;

public class RomScannerService
{
    private readonly ILogger _logger = Log.ForContext<RomScannerService>();

    public Task<List<Game>> ScanFolderAsync(string folderPath, string? platformOverride = null, bool recursive = true)
    {
        var games = new List<Game>();

        if (!Directory.Exists(folderPath))
        {
            _logger.Warning("ROM folder not found: {Path}", folderPath);
            return Task.FromResult(games);
        }

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        
        // Get all ROM extensions from platform definitions
        var allExtensions = PlatformDefinitions.Platforms
            .SelectMany(p => p.Value.Extensions)
            .Distinct()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var files = Directory.GetFiles(folderPath, "*.*", searchOption)
            .Where(f => allExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));

        foreach (var file in files)
        {
            try
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();
                var platformInfo = PlatformDefinitions.GetByExtension(ext);
                
                // Use override if specified
                var platformName = platformOverride ?? platformInfo?.ShortName ?? "Unknown";

                var game = new Game
                {
                    Id = Guid.NewGuid(),
                    Title = CleanRomTitle(Path.GetFileNameWithoutExtension(file)),
                    SortTitle = CleanRomTitle(Path.GetFileNameWithoutExtension(file)),
                    InstallPath = file,
                    IsInstalled = true,
                    Source = "ROM",
                    SourceId = ComputeFileHash(file)
                };

                games.Add(game);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to process ROM: {File}", file);
            }
        }

        _logger.Information("Found {Count} ROMs in {Path}", games.Count, folderPath);
        return Task.FromResult(games);
    }

    public Task<Dictionary<string, List<Game>>> ScanFolderByPlatformAsync(string folderPath, bool recursive = true)
    {
        var result = new Dictionary<string, List<Game>>();

        if (!Directory.Exists(folderPath))
            return Task.FromResult(result);

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        foreach (var platformKvp in PlatformDefinitions.Platforms)
        {
            var platformName = platformKvp.Key;
            var extensions = platformKvp.Value.Extensions;

            var files = Directory.GetFiles(folderPath, "*.*", searchOption)
                .Where(f => extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToList();

            if (files.Count > 0)
            {
                result[platformName] = files.Select(file => new Game
                {
                    Id = Guid.NewGuid(),
                    Title = CleanRomTitle(Path.GetFileNameWithoutExtension(file)),
                    SortTitle = CleanRomTitle(Path.GetFileNameWithoutExtension(file)),
                    InstallPath = file,
                    IsInstalled = true,
                    Source = "ROM",
                    SourceId = file.GetHashCode().ToString()
                }).ToList();
            }
        }

        return Task.FromResult(result);
    }

    private string CleanRomTitle(string filename)
    {
        // Remove common ROM naming conventions
        var title = filename;
        
        // Remove region codes like (USA), [USA], (E), (J), etc.
        title = System.Text.RegularExpressions.Regex.Replace(title, @"\s*[\[\(][A-Za-z,!\s]+[\]\)]\s*", " ");
        
        // Remove version numbers like (Rev 1), (v1.0)
        title = System.Text.RegularExpressions.Regex.Replace(title, @"\s*[\[\(][Vv]?\d[\.\d]*[\]\)]\s*", " ");
        
        // Remove dump info like [!], [b], [h], etc.
        title = System.Text.RegularExpressions.Regex.Replace(title, @"\s*\[[!\w]+\]\s*", " ");
        
        // Clean up whitespace
        title = System.Text.RegularExpressions.Regex.Replace(title, @"\s+", " ").Trim();
        
        return title;
    }

    private string ComputeFileHash(string filePath)
    {
        // Simple hash based on file size and first bytes for speed
        try
        {
            var fileInfo = new FileInfo(filePath);
            using var stream = File.OpenRead(filePath);
            var buffer = new byte[Math.Min(1024, fileInfo.Length)];
            _ = stream.Read(buffer, 0, buffer.Length); // Read may return fewer bytes; for hashing purposes, partial read is acceptable
            
            var hash = $"{fileInfo.Length}-{BitConverter.ToInt64(buffer, 0)}";
            return hash;
        }
        catch
        {
            return filePath.GetHashCode().ToString();
        }
    }
}
