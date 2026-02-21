# Wave 2 Game Provider Plugins - Implementation Guide

**Created:** 2026-01-17
**Status:** Ready to Implement
**Estimated Time:** 15-20 hours total

---

## Overview

Wave 2 focuses on **Game Provider** plugins that expand SaveState's library detection by importing games from external sources and launchers.

---

## Plugin 1: itch.io Importer

### Features

- Scan local itch.io app installations (`%APPDATA%\itch\apps`)
- OAuth2 authentication with itch.io
- Pull owned games from API (`https://itch.io/api/1/{key}/my-games`)
- Import game metadata (description, tags, screenshots)
- Track itch.io game jams

### Implementation Steps

1. Add reference to SaveState.Core
2. Install `System.Data.SQLite.Core` for local DB parsing
3. Implement `IGameProvider` interface
4. Create OAuth flow with local HTTP server callback
5. Parse itch.io's local SQLite database
6. Map itch.io games to SaveState `Game` entities

### Key Code Structure

```csharp
public class ItchIOPlugin : IPlugin, IGameProvider
{
    public string ProviderName => "itch.io";

    // Scan %APPDATA%\itch\apps for installed games
    private async Task<List<Game>> ScanLocalInstallations();

    // OAuth2 flow
    private async Task<string> AuthenticateAsync();

    // API call to get owned games
    private async Task<List<Game>> FetchOwnedGamesAsync(string apiKey);
}
```

### API Endpoints

- **Auth:** `https://itch.io/user/oauth`
- **Games:** `https://itch.io/api/1/{key}/my-games`
- **Local DB:** `%APPDATA%\itch\db\butler.db`

---

## Plugin 2: Humble Bundle Library

### Features

- OAuth2 login via Humble Bundle
- Pull complete purchase history
- Detect redeemed vs. unclaimed keys
- Link to direct download (DRM-free)
- Track Humble Choice subscription

### Implementation Steps

1. Add reference to SaveState.Core
2. Implement OAuth2 flow
3. Parse Humble Bundle API responses
4. Map bundle purchases to games
5. Track key redemption status

### Key Code Structure

```csharp
public class HumbleBundlePlugin : IPlugin, IGameProvider
{
    public string ProviderName => "Humble Bundle";

    private async Task<string> AuthenticateAsync();
    private async Task<List<HumbleOrder>> FetchOrdersAsync();
    private async Task<List<Game>> ParseOrdersToGames(List<HumbleOrder> orders);
}
```

### API Endpoints

- **Auth:** `https://www.humblebundle.com/login`
- **Orders:** `https://www.humblebundle.com/api/v1/user/order`
- **Downloads:** `https://www.humblebundle.com/api/v1/order/{order_id}`

---

## Plugin 3: Amazon/Prime Gaming

### Features

- Detect Amazon Games Launcher
- Parse local game installations
- Pull claimed Prime Gaming offers
- Show in-game loot availability
- Notify of new claims

### Implementation Steps

1. Add reference to SaveState.Core
2. Install `Microsoft.Data.Sqlite` for local DB
3. Locate Amazon Games Launcher DB
4. Parse game installations
5. Web scrape Prime Gaming claims (no official API)

### Key Code Structure

```csharp
public class PrimeGamingPlugin : IPlugin, IGameProvider
{
    public string ProviderName => "Prime Gaming";

    private string GetAmazonGamesDbPath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Amazon Games", "Data", "Games", "Sql", "GameInstallInfo.sqlite");

    private async Task<List<Game>> ParseLocalDatabase();
    private async Task<List<PrimeOffer>> ScrapeClaimedOffers();
}
```

### Local Paths

- **DB:** `%LOCALAPPDATA%\Amazon Games\Data\Games\Sql\GameInstallInfo.sqlite`
- **Install:** `%PROGRAMFILES%\Amazon Games\App\`

---

## Plugin 4: Playnite Import

### Features

- Read Playnite's SQLite database
- Import games, playtime, completion status
- Map platforms and categories
- Import custom metadata fields
- Preserve Playnite IDs for future sync

### Implementation Steps

1. Add reference to SaveState.Core
2. Install `Microsoft.Data.Sqlite`
3. Implement `IImporter` interface
4. Parse Playnite database schema
5. Map Playnite fields to SaveState entities

### Key Code Structure

```csharp
public class PlayniteImportPlugin : IPlugin, IImporter
{
    public string ImporterName => "Playnite";
    public IReadOnlyList<string> SupportedApplications => new[] { "Playnite" };

    private string GetPlayniteDbPath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Playnite", "library", "games.db");

    public async Task<Result<ImportAnalysis>> AnalyzeImportAsync(string filePath, CancellationToken ct);
    public async Task<Result<ImportResult>> ImportAsync(string filePath, ImportOptions options, CancellationToken ct);
}
```

### Database Schema

```sql
-- Playnite tables
SELECT * FROM games;
SELECT * FROM platforms;
SELECT * FROM game_actions;
SELECT * FROM playtime;
```

### Local Paths

- **DB:** `%APPDATA%\Playnite\library\games.db`
- **Media:** `%APPDATA%\Playnite\library\files\`

---

## Plugin 5: LaunchBox Import

### Features

- Parse LaunchBox XML database
- Import games, emulators, media
- Map platforms to SaveState cores
- Import playlists as collections
- Preserve LaunchBox IDs

### Implementation Steps

1. Add reference to SaveState.Core
2. Implement `IImporter` interface
3. Parse XML platform files
4. Map emulator configurations
5. Import media (covers, screenshots)

### Key Code Structure

```csharp
public class LaunchBoxImportPlugin : IPlugin, IImporter
{
    public string ImporterName => "LaunchBox";
    public IReadOnlyList<string> SupportedApplications => new[] { "LaunchBox", "BigBox" };

    private string GetLaunchBoxPath() =>
        // User must specify LaunchBox install directory
        _settings.LaunchBoxPath;

    public async Task<Result<ImportAnalysis>> AnalyzeImportAsync(string filePath, CancellationToken ct);
    public async Task<Result<ImportResult>> ImportAsync(string filePath, ImportOptions options, CancellationToken ct);
}
```

### XML Structure

```xml
<!-- LaunchBox\Data\Platforms\{Platform}.xml -->
<LaunchBox>
  <Game>
    <Title>Game Name</Title>
    <Platform>Nintendo Entertainment System</Platform>
    <ApplicationPath>path\to\rom.nes</ApplicationPath>
    <Emulator>RetroArch</Emulator>
  </Game>
</LaunchBox>
```

### Local Paths

- **XML:** `{LaunchBox}\Data\Platforms\*.xml`
- **Media:** `{LaunchBox}\Images\`
- **Emulators:** `{LaunchBox}\Emulators\`

---

## Common Implementation Patterns

### 1. Settings Persistence

All plugins should persist settings:

```csharp
public class PluginSettings
{
    public bool Enabled { get; set; } = true;
    public string? ApiKey { get; set; }
    public string? InstallPath { get; set; }
    public DateTime? LastSync { get; set; }
}
```

### 2. Error Handling

```csharp
try
{
    var games = await DiscoverGamesAsync(ct);
    return Result.Success(games);
}
catch (FileNotFoundException ex)
{
    _context?.Logger.LogWarning("Installation not found: {Message}", ex.Message);
    return Result.Success(Array.Empty<Game>());
}
catch (Exception ex)
{
    _context?.Logger.LogError(ex, "Failed to discover games");
    return Result.Failure<IReadOnlyList<Game>>("Discovery failed", ErrorType.Internal);
}
```

### 3. Progress Reporting

```csharp
for (int i = 0; i < games.Count; i++)
{
    var progress = (float)(i + 1) / games.Count;
    _context?.ReportProgress($"Importing {games[i].Title}...", progress);
    await ProcessGameAsync(games[i]);
}
```

---

## Dependencies

| Plugin | NuGet Packages |
|--------|----------------|
| itch.io | `System.Data.SQLite.Core` |
| Humble Bundle | None (HttpClient) |
| Prime Gaming | `Microsoft.Data.Sqlite` |
| Playnite Import | `Microsoft.Data.Sqlite` |
| LaunchBox Import | None (System.Xml) |

---

## Testing Checklist

### itch.io

- [ ] Detect local itch.io app installation
- [ ] Parse butler.db successfully
- [ ] OAuth flow completes
- [ ] API returns owned games
- [ ] Games imported with correct metadata

### Humble Bundle

- [ ] OAuth authentication works
- [ ] Purchase history retrieved
- [ ] Unredeemed keys detected
- [ ] DRM-free downloads linked

### Prime Gaming

- [ ] Amazon Games Launcher detected
- [ ] Local database parsed
- [ ] Claimed games imported
- [ ] Loot offers displayed

### Playnite

- [ ] Database located automatically
- [ ] Import analysis shows correct counts
- [ ] Games imported with playtime
- [ ] Platforms mapped correctly

### LaunchBox

- [ ] XML files parsed successfully
- [ ] Emulator configurations imported
- [ ] Media files linked
- [ ] Playlists converted to collections

---

## Estimated Effort

| Plugin | Complexity | Est. Time |
|--------|------------|-----------|
| itch.io Importer | Medium | 5 hours |
| Humble Bundle | Medium | 5 hours |
| Prime Gaming | Medium | 4 hours |
| Playnite Import | Easy | 2 hours |
| LaunchBox Import | Easy | 2 hours |

**Total:** 18 hours

---

## Next Steps

1. Implement itch.io plugin (highest value)
2. Implement Playnite import (easiest, quick win)
3. Implement LaunchBox import (similar to Playnite)
4. Implement Humble Bundle (OAuth complexity)
5. Implement Prime Gaming (web scraping required)

---

**Ready to start implementation? Begin with Playnite Import for a quick win!**
