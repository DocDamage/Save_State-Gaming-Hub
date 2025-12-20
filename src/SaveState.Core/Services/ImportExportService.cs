using SaveState.Core.Data;
using SaveState.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text.Json;

namespace SaveState.Core.Services;

public class ImportExportService
{
    private readonly SaveStateDbContext _dbContext;
    private readonly ILogger _logger = Log.ForContext<ImportExportService>();

    public ImportExportService(SaveStateDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> ExportLibraryAsync()
    {
        var exportData = new LibraryExport
        {
            ExportDate = DateTime.UtcNow,
            Version = "1.0",
            Games = await _dbContext.Games
                .Include(g => g.Platform)
                .Select(g => new GameExport
                {
                    Title = g.Title,
                    SortTitle = g.SortTitle,
                    Description = g.Description,
                    ReleaseDate = g.ReleaseDate,
                    PlatformName = g.Platform != null ? g.Platform.Name : null,
                    CoverImage = g.CoverImage,
                    Source = g.Source,
                    SourceId = g.SourceId,
                    IsInstalled = g.IsInstalled,
                    InstallPath = g.InstallPath,
                    PlayTimeMinutes = (int)g.PlayTime.TotalMinutes
                })
                .ToListAsync(),
            Collections = await _dbContext.Collections
                .Include(c => c.Games)
                .Select(c => new CollectionExport
                {
                    Name = c.Name,
                    Description = c.Description,
                    GameTitles = c.Games.Select(g => g.Title).ToList()
                })
                .ToListAsync(),
            Emulators = await _dbContext.Emulators
                .Select(e => new EmulatorExport
                {
                    Name = e.Name,
                    ExecutablePath = e.ExecutablePath,
                    Arguments = e.Arguments,
                    SupportedPlatforms = e.SupportedPlatforms,
                    IsDefault = e.IsDefault
                })
                .ToListAsync()
        };

        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        
        _logger.Information("Exported {Count} games, {Collections} collections", 
            exportData.Games.Count, exportData.Collections.Count);
        
        return json;
    }

    public async Task<int> ImportLibraryAsync(string json, bool mergeExisting = true)
    {
        var importData = JsonSerializer.Deserialize<LibraryExport>(json, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        if (importData == null)
            throw new InvalidDataException("Invalid import data");

        int imported = 0;

        // Import games
        foreach (var gameExport in importData.Games)
        {
            // Check if game already exists
            var existingGame = await _dbContext.Games
                .FirstOrDefaultAsync(g => g.Title == gameExport.Title && g.Source == gameExport.Source);
            
            if (existingGame != null && mergeExisting)
            {
                // Update existing
                existingGame.Description ??= gameExport.Description;
                existingGame.CoverImage ??= gameExport.CoverImage;
            }
            else if (existingGame == null)
            {
                // Get or create platform
                Platform? platform = null;
                if (!string.IsNullOrEmpty(gameExport.PlatformName))
                {
                    platform = await _dbContext.Platforms.FirstOrDefaultAsync(p => p.Name == gameExport.PlatformName);
                    if (platform == null)
                    {
                        platform = new Platform { Id = Guid.NewGuid(), Name = gameExport.PlatformName };
                        _dbContext.Platforms.Add(platform);
                    }
                }

                var game = new Game
                {
                    Id = Guid.NewGuid(),
                    Title = gameExport.Title,
                    SortTitle = gameExport.SortTitle,
                    Description = gameExport.Description,
                    ReleaseDate = gameExport.ReleaseDate,
                    PlatformId = platform?.Id ?? Guid.Empty,
                    CoverImage = gameExport.CoverImage,
                    Source = gameExport.Source,
                    SourceId = gameExport.SourceId,
                    IsInstalled = gameExport.IsInstalled,
                    InstallPath = gameExport.InstallPath,
                    PlayTime = TimeSpan.FromMinutes(gameExport.PlayTimeMinutes)
                };
                
                _dbContext.Games.Add(game);
                imported++;
            }
        }

        // Import collections
        foreach (var collectionExport in importData.Collections)
        {
            var existing = await _dbContext.Collections
                .FirstOrDefaultAsync(c => c.Name == collectionExport.Name);
            
            if (existing == null)
            {
                var collection = new Collection
                {
                    Id = Guid.NewGuid(),
                    Name = collectionExport.Name,
                    Description = collectionExport.Description
                };
                _dbContext.Collections.Add(collection);
            }
        }

        // Import emulators
        foreach (var emulatorExport in importData.Emulators)
        {
            var existing = await _dbContext.Emulators
                .FirstOrDefaultAsync(e => e.Name == emulatorExport.Name);
            
            if (existing == null)
            {
                var emulator = new Emulator
                {
                    Id = Guid.NewGuid(),
                    Name = emulatorExport.Name,
                    ExecutablePath = emulatorExport.ExecutablePath,
                    Arguments = emulatorExport.Arguments,
                    SupportedPlatforms = emulatorExport.SupportedPlatforms,
                    IsDefault = emulatorExport.IsDefault,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _dbContext.Emulators.Add(emulator);
            }
        }

        await _dbContext.SaveChangesAsync();
        
        _logger.Information("Imported {Count} games", imported);
        return imported;
    }
}

public class LibraryExport
{
    public DateTime ExportDate { get; set; }
    public string Version { get; set; } = "1.0";
    public List<GameExport> Games { get; set; } = new();
    public List<CollectionExport> Collections { get; set; } = new();
    public List<EmulatorExport> Emulators { get; set; } = new();
}

public class GameExport
{
    public string Title { get; set; } = string.Empty;
    public string? SortTitle { get; set; }
    public string? Description { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public string? PlatformName { get; set; }
    public string? CoverImage { get; set; }
    public string? Source { get; set; }
    public string? SourceId { get; set; }
    public bool IsInstalled { get; set; }
    public string? InstallPath { get; set; }
    public int PlayTimeMinutes { get; set; }
}

public class CollectionExport
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> GameTitles { get; set; } = new();
}

public class EmulatorExport
{
    public string Name { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string? Arguments { get; set; }
    public string? SupportedPlatforms { get; set; }
    public bool IsDefault { get; set; }
}
