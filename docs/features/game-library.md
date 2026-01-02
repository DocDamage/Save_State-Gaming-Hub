# 📚 Game Library Management

**Status**: ✅ Implemented
**Last Updated**: January 2, 2026
**Layer**: Core + Application + Infrastructure + Presentation
**Related**: [Analytics](analytics.md), [AI_MASTER_CONTEXT](../AI_MASTER_CONTEXT.md)

---

## Overview

The Game Library is the core feature of SaveState Reborn, managing your entire game collection.

### Key Features

- **Multi-Platform Support**: Steam, GOG, Epic, Itch.io, emulators
- **Automatic Detection**: Scan and import installed games
- **Metadata Enrichment**: IGDB, SteamGridDB integration
- **Statistics Tracking**: Playtime, sessions, achievements
- **Smart Categories**: AI-powered automatic categorization

## Architecture

### Domain Model

```csharp
public class Game : EntityBase, IAuditableEntity
{
    public GameTitle Title { get; private set; }
    public string? Description { get; private set; }
    public Guid PlatformId { get; private set; }
    public Platform Platform { get; private set; }
    public GameStatus Status { get; private set; }
    public TimeSpan TotalPlaytime { get; private set; }
}
```

### CQRS Commands/Queries

| Type | Name | Purpose |
|------|------|---------|
| Command | `ImportGameCommand` | Add new game |
| Command | `UpdateGameCommand` | Modify game |
| Command | `DeleteGameCommand` | Remove game |
| Query | `GetGamesQuery` | List games (paginated) |
| Query | `GetGameDetailsQuery` | Single game details |

## Implementation Files

| Component | File |
|-----------|------|
| Entity | `Core/GameLibrary/Entities/Game.cs` |
| Repository | `Infrastructure/Repositories/GameRepository.cs` |
| Import Handler | `Application/GameLibrary/Commands/Handlers/ImportGameCommandHandler.cs` |
| ViewModel | `Presentation/ViewModels/GameLibraryViewModel.cs` |

## Platform Support

| Platform | Detection | Metadata | Status |
|----------|-----------|----------|--------|
| Steam | ✅ Registry | ✅ Steam API | Active |
| GOG | ✅ Galaxy | ✅ IGDB | Active |
| Epic | ✅ Launcher | ✅ IGDB | Active |
| Itch.io | ✅ Butler | ✅ Itch API | Active |
| RetroArch | ✅ Playlist | ✅ IGDB | Active |
| Custom | ✅ Manual | ✅ IGDB | Active |

## API Usage

```csharp
// Import a game
var command = new ImportGameCommand(
    Title: "Super Mario World",
    PlatformId: snesId,
    ExecutablePath: "/roms/smw.sfc"
);
await mediator.Send(command);

// Get games (paginated)
var query = new GetGamesQuery(Page: 1, PageSize: 50);
var games = await mediator.Send(query);
```

## UI Features

- **Grid/List Views**: Switch between display modes
- **Filtering**: By platform, status, genre
- **Sorting**: By title, playtime, last played
- **Search**: Full-text search across library
- **Quick Actions**: Launch, edit, delete

## Configuration

```json
{
  "GameLibrary": {
    "AutoScan": true,
    "ScanInterval": "01:00:00",
    "MetadataProviders": ["IGDB", "SteamGridDB"],
    "DefaultPlatformId": "pc"
  }
}
```

---

**Related**: [Analytics](analytics.md) for playtime tracking
