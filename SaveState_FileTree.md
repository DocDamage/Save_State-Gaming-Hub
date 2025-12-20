# SaveState Reborn: Complete File Tree
## Project Structure & File Descriptions

**Document ID:** SS-FT-001  
**Revision:** 1.0  
**Date:** 2024-12-20

---

## Legend

| Icon | Meaning |
|------|---------|
| 📁 | Directory |
| 📄 | File |
| 🔧 | Configuration File |
| 🧪 | Test File |
| 📦 | Project File |

---

## Complete Project Tree

```
SaveState/
├── 📁 src/                                    # Source code root
│   │
│   ├── 📁 SaveState.Core/                     # Core business logic library
│   │   ├── 📦 SaveState.Core.csproj           # Project file (classlib, .NET 9, Native AOT)
│   │   │
│   │   ├── 📁 Entities/                       # Database entity models
│   │   │   ├── 📄 Game.cs                     # Game entity (title, platform, playtime, metadata)
│   │   │   ├── 📄 Platform.cs                 # Platform entity (name, icon, emulator config)
│   │   │   ├── 📄 GameImage.cs                # Image entity (cover, background, icon, logo)
│   │   │   ├── 📄 Achievement.cs              # Achievement entity (RetroAchievements support)
│   │   │   ├── 📄 Collection.cs               # User collection groupings
│   │   │   └── 📄 PlaySession.cs              # Play session tracking data
│   │   │
│   │   ├── 📁 Data/                           # Data access layer
│   │   │   ├── 📄 SaveStateDbContext.cs       # EF Core DbContext with SQLite configuration
│   │   │   ├── 📄 DesignTimeDbContextFactory.cs # Migration factory for design-time tooling
│   │   │   └── 📁 Migrations/                 # EF Core migration files (auto-generated)
│   │   │       └── 📄 InitialCreate.cs        # Initial database schema migration
│   │   │
│   │   ├── 📁 Interfaces/                     # Provider and service contracts
│   │   │   ├── 📄 IGameProvider.cs            # Base interface for all store providers
│   │   │   ├── 📄 IMetadataProvider.cs        # Interface for metadata fetching (IGDB, SteamGridDB)
│   │   │   ├── 📄 IGameService.cs             # Game CRUD operations contract
│   │   │   └── 📄 IRomManager.cs              # ROM scanning and organization contract
│   │   │
│   │   ├── 📁 Services/                       # Business logic services
│   │   │   ├── 📄 GameService.cs              # CRUD operations for games
│   │   │   ├── 📄 ProviderManager.cs          # Registers and manages all game providers
│   │   │   ├── 📄 MetadataService.cs          # Coordinates metadata fetching from multiple sources
│   │   │   ├── 📄 RomScannerService.cs        # Scans directories for ROMs, matches to database
│   │   │   ├── 📄 BiosManagerService.cs       # Manages BIOS files for emulator configuration
│   │   │   ├── 📄 AchievementService.cs       # RetroAchievements integration and tracking
│   │   │   └── 📄 PlaytimeTracker.cs          # Tracks active play sessions and total playtime
│   │   │
│   │   ├── 📁 Providers/                      # Store-specific provider implementations
│   │   │   ├── 📄 SteamProvider.cs            # Steam client integration via Steam Web API
│   │   │   ├── 📄 GogProvider.cs              # GOG Galaxy database parsing
│   │   │   ├── 📄 EpicProvider.cs             # Epic Games Store manifest reading
│   │   │   ├── 📄 XboxProvider.cs             # Xbox/Game Pass UWP API integration
│   │   │   ├── 📄 EaProvider.cs               # EA App registry/database parsing
│   │   │   ├── 📄 UbisoftProvider.cs          # Ubisoft Connect registry parsing
│   │   │   ├── 📄 AmazonProvider.cs           # Amazon Games library scanning
│   │   │   ├── 📄 ItchProvider.cs             # itch.io Butler API integration
│   │   │   ├── 📄 HumbleProvider.cs           # Humble Bundle Trove scanning
│   │   │   └── 📄 PlayStationProvider.cs      # PlayStation PC launcher integration
│   │   │
│   │   ├── 📁 Metadata/                       # Metadata provider implementations
│   │   │   ├── 📄 IgdbMetadataProvider.cs     # IGDB API client for game metadata
│   │   │   ├── 📄 SteamGridDbProvider.cs      # SteamGridDB API for artwork
│   │   │   └── 📄 RetroAchievementsClient.cs  # RetroAchievements.org API client
│   │   │
│   │   └── 📁 Infrastructure/                 # Cross-cutting concerns
│   │       ├── 📄 IpcService.cs               # gRPC over Named Pipes for single-instance
│   │       ├── 📄 SingleInstanceLock.cs       # Mutex-based single-instance enforcement
│   │       ├── 📄 ConfigurationManager.cs     # App settings management
│   │       └── 📄 LoggingConfiguration.cs     # Structured logging setup (Serilog)
│   │
│   ├── 📁 SaveState.UI/                       # Avalonia UI application
│   │   ├── 📦 SaveState.UI.csproj             # Project file (Avalonia.App, .NET 9)
│   │   │
│   │   ├── 📁 Views/                          # AXAML view files
│   │   │   ├── 📄 MainWindow.axaml            # Primary application window
│   │   │   ├── 📄 MainWindow.axaml.cs         # Code-behind for MainWindow
│   │   │   ├── 📄 GameGridView.axaml          # Grid display of games with covers
│   │   │   ├── 📄 GameGridView.axaml.cs       # Code-behind for GameGridView
│   │   │   ├── 📄 GameDetailView.axaml        # Detailed game information panel
│   │   │   ├── 📄 GameDetailView.axaml.cs     # Code-behind for GameDetailView
│   │   │   ├── 📄 SettingsView.axaml          # Application settings panel
│   │   │   ├── 📄 SettingsView.axaml.cs       # Code-behind for SettingsView
│   │   │   ├── 📄 LibraryView.axaml           # Library browser with filters
│   │   │   └── 📄 LibraryView.axaml.cs        # Code-behind for LibraryView
│   │   │
│   │   ├── 📁 ViewModels/                     # MVVM ViewModels
│   │   │   ├── 📄 MainWindowViewModel.cs      # Main window state and navigation
│   │   │   ├── 📄 GameGridViewModel.cs        # Game grid data binding and commands
│   │   │   ├── 📄 GameDetailViewModel.cs      # Single game detail display
│   │   │   ├── 📄 SettingsViewModel.cs        # Settings management and persistence
│   │   │   ├── 📄 LibraryViewModel.cs         # Library browsing, filtering, sorting
│   │   │   └── 📄 ViewModelBase.cs            # Base class with INotifyPropertyChanged
│   │   │
│   │   ├── 📁 Controls/                       # Reusable custom controls
│   │   │   ├── 📄 GameCard.axaml              # Single game card with cover art
│   │   │   ├── 📄 GameCard.axaml.cs           # Code-behind for GameCard
│   │   │   ├── 📄 SidebarNavigation.axaml     # Left-side navigation menu
│   │   │   ├── 📄 SidebarNavigation.axaml.cs  # Code-behind for SidebarNavigation
│   │   │   ├── 📄 SearchBox.axaml             # Search input with autocomplete
│   │   │   └── 📄 SearchBox.axaml.cs          # Code-behind for SearchBox
│   │   │
│   │   ├── 📁 Themes/                         # Application themes
│   │   │   ├── 📄 DarkTheme.axaml             # Dark color scheme and styles
│   │   │   ├── 📄 LightTheme.axaml            # Light color scheme and styles
│   │   │   └── 📄 SharedStyles.axaml          # Common styles shared across themes
│   │   │
│   │   ├── 📁 Assets/                         # Static resources
│   │   │   ├── 📁 Images/                     # Application images
│   │   │   │   ├── 📄 logo.png                # SaveState logo
│   │   │   │   ├── 📄 default-cover.png       # Placeholder game cover
│   │   │   │   └── 📄 platform-icons/         # Platform-specific icons
│   │   │   └── 📁 Fonts/                      # Custom fonts
│   │   │       └── 📄 Inter-Variable.ttf      # Inter font for UI text
│   │   │
│   │   ├── 📄 App.axaml                       # Application resources and startup
│   │   └── 📄 App.axaml.cs                    # Application entry configuration
│   │
│   └── 📁 SaveState.App/                      # Executable entry point
│       ├── 📦 SaveState.App.csproj            # Project file (Console/WinExe, AOT enabled)
│       ├── 📄 Program.cs                      # Main() entry point, host builder
│       ├── 📄 appsettings.json                # Runtime configuration (logging, paths)
│       └── 📄 rd.xml                          # Runtime directives for AOT reflection
│
├── 📁 tests/                                  # Test projects
│   │
│   ├── 📁 SaveState.Core.Tests/               # Core library unit tests
│   │   ├── 📦 SaveState.Core.Tests.csproj     # Test project (xUnit)
│   │   ├── 📁 Services/                       # Service layer tests
│   │   │   ├── 🧪 GameServiceTests.cs         # CRUD operation tests
│   │   │   ├── 🧪 ProviderManagerTests.cs     # Provider registration tests
│   │   │   └── 🧪 MetadataServiceTests.cs     # Metadata fetching tests
│   │   ├── 📁 Providers/                      # Provider implementation tests
│   │   │   ├── 🧪 SteamProviderTests.cs       # Steam integration tests
│   │   │   ├── 🧪 GogProviderTests.cs         # GOG integration tests
│   │   │   └── 🧪 EpicProviderTests.cs        # Epic integration tests
│   │   └── 📁 Data/                           # Data layer tests
│   │       └── 🧪 DbContextTests.cs           # Database schema and query tests
│   │
│   └── 📁 SaveState.Integration.Tests/        # End-to-end integration tests
│       ├── 📦 SaveState.Integration.Tests.csproj
│       ├── 🧪 StoreImportTests.cs             # Full store import workflow tests
│       └── 🧪 LaunchTests.cs                  # Game launch verification tests
│
├── 📁 docs/                                   # Documentation
│   ├── 📄 README.md                           # Project overview and quick start
│   ├── 📄 CONTRIBUTING.md                     # Contribution guidelines
│   ├── 📄 ARCHITECTURE.md                     # System architecture overview
│   ├── 📄 API.md                              # Provider interface documentation
│   ├── 📄 BUILDING.md                         # Build and compilation instructions
│   └── 📄 CHANGELOG.md                        # Version history and release notes
│
├── 📁 build/                                  # Build scripts and configurations
│   ├── 📄 build.ps1                           # PowerShell build script (Windows)
│   ├── 📄 build.sh                            # Bash build script (Linux/macOS)
│   ├── 📄 publish-win-x64.ps1                 # Windows AOT publish script
│   ├── 📄 publish-linux-x64.sh                # Linux AOT publish script
│   └── 📄 publish-osx-arm64.sh                # macOS ARM64 publish script
│
├── 📁 installer/                              # Installer creation
│   ├── 📄 SaveState.iss                       # Inno Setup script (Windows)
│   ├── 📄 org.savestate.desktop               # Linux .desktop file
│   └── 📄 flatpak/                            # Flatpak packaging
│       ├── 📄 org.savestate.SaveState.yml     # Flatpak manifest
│       └── 📄 org.savestate.SaveState.metainfo.xml  # AppStream metadata
│
├── 📁 .github/                                # GitHub configuration
│   ├── 📁 workflows/                          # GitHub Actions CI/CD
│   │   ├── 📄 ci.yml                          # Continuous integration (build, test)
│   │   ├── 📄 release.yml                     # Release workflow (publish, installers)
│   │   └── 📄 codeql.yml                      # Code security analysis
│   └── 📄 ISSUE_TEMPLATE/                     # Issue templates
│       ├── 📄 bug_report.md                   # Bug report template
│       └── 📄 feature_request.md              # Feature request template
│
├── 🔧 SaveState.sln                           # Visual Studio solution file
├── 🔧 Directory.Build.props                   # Shared MSBuild properties (versioning, AOT)
├── 🔧 Directory.Build.targets                 # Shared MSBuild targets
├── 🔧 Directory.Packages.props                # Central Package Management (NuGet versions)
├── 🔧 global.json                             # .NET SDK version pinning
├── 🔧 nuget.config                            # NuGet package source configuration
├── 🔧 .editorconfig                           # Code style enforcement
├── 🔧 .gitignore                              # Git ignore patterns
├── 📄 LICENSE                                 # MIT License
└── 📄 README.md                               # Repository root readme
```

---

## Detailed File Descriptions

### Solution Root

| File | Description |
|------|-------------|
| `SaveState.sln` | Visual Studio solution containing all projects with proper references and build configurations |
| `Directory.Build.props` | Central MSBuild properties: target framework (.NET 9), C# 13, PublishAot=true, version info |
| `Directory.Build.targets` | Shared build targets including AOT trim warnings suppression |
| `Directory.Packages.props` | Central Package Management file listing all NuGet package versions |
| `global.json` | Pins SDK version to .NET 9.0.x for consistent builds |
| `nuget.config` | Configures NuGet.org feed and any private package sources |
| `.editorconfig` | Enforces .NET code style rules (naming, formatting, analyzers) |

---

### SaveState.Core Project

#### Entities/

| File | Purpose |
|------|---------|
| `Game.cs` | Primary entity with Id, Title, SortTitle, Description, ReleaseDate, PlatformId, CoverImage, BackgroundImage, PlayTime, Source, SourceId, IsInstalled, InstallPath, LaunchCommand |
| `Platform.cs` | Gaming platform (PC, PlayStation, Nintendo, etc.) with Name, Icon, DefaultEmulator, Specification |
| `GameImage.cs` | Image metadata with Id, GameId, Type (Cover/Background/Icon/Logo), Path, Url, Width, Height |
| `Achievement.cs` | Achievement data with Id, GameId, Title, Description, Points, IsUnlocked, UnlockedDate, IconUrl |
| `Collection.cs` | User-defined collection with Id, Name, Description, Games (many-to-many), SortOrder |
| `PlaySession.cs` | Session tracking with Id, GameId, StartTime, EndTime, Duration |

#### Data/

| File | Purpose |
|------|---------|
| `SaveStateDbContext.cs` | EF Core DbContext with DbSet<Game>, DbSet<Platform>, etc., SQLite configuration, OnModelCreating for relationships |
| `DesignTimeDbContextFactory.cs` | IDesignTimeDbContextFactory implementation for `dotnet ef migrations` tooling |

#### Interfaces/

| File | Purpose |
|------|---------|
| `IGameProvider.cs` | Contract: `Task<IEnumerable<Game>> GetInstalledGamesAsync()`, `Task<IEnumerable<Game>> GetOwnedGamesAsync()`, `Task LaunchGameAsync(Game game)`, `string Id`, `string Name` |
| `IMetadataProvider.cs` | Contract: `Task<GameMetadata> GetMetadataAsync(string title)`, `Task<IEnumerable<GameImage>> GetImagesAsync(string title)` |
| `IGameService.cs` | CRUD contract: `GetAllAsync()`, `GetByIdAsync()`, `AddAsync()`, `UpdateAsync()`, `DeleteAsync()`, `SearchAsync()` |
| `IRomManager.cs` | ROM operations: `ScanDirectoryAsync()`, `MatchToDatabase()`, `OrganizeRoms()`, `GetRomInfo()` |

#### Services/

| File | Purpose |
|------|---------|
| `GameService.cs` | Implements IGameService using EF Core, handles caching, validation |
| `ProviderManager.cs` | Registry pattern for IGameProvider implementations, discovery, aggregation |
| `MetadataService.cs` | Orchestrates metadata fetching from IGDB → SteamGridDB fallback chain |
| `RomScannerService.cs` | File system scanning with extension filtering, hash matching, database lookup |
| `BiosManagerService.cs` | BIOS file detection, validation, emulator configuration linking |
| `AchievementService.cs` | RetroAchievements API integration, unlock tracking, points calculation |
| `PlaytimeTracker.cs` | Process monitoring for active games, session recording, aggregation |

#### Providers/

| File | Purpose |
|------|---------|
| `SteamProvider.cs` | Parses `steamapps/libraryfolders.vdf`, uses Steam Web API for owned games |
| `GogProvider.cs` | Reads GOG Galaxy database (`galaxy-2.0.db`), game file detection |
| `EpicProvider.cs` | Parses `.egstore` manifests, Epic Games Launcher registry |
| `XboxProvider.cs` | Uses Windows.Gaming.XboxLive APIs, Game Pass catalog |
| `EaProvider.cs` | Reads EA App/Origin registry keys, local game database |
| `UbisoftProvider.cs` | Parses Ubisoft Connect SQLite database, registry detection |
| `AmazonProvider.cs` | Scans Amazon Games installation directory, manifest parsing |
| `ItchProvider.cs` | Butler receipt parsing, itch app database integration |
| `HumbleProvider.cs` | Humble Bundle Trove directory scanning, API integration |
| `PlayStationProvider.cs` | PlayStation PC app installation detection |

#### Metadata/

| File | Purpose |
|------|---------|
| `IgdbMetadataProvider.cs` | IGDB Twitch API client with OAuth, game search, genre/theme fetching |
| `SteamGridDbProvider.cs` | SteamGridDB REST API for covers, heroes, logos, icons |
| `RetroAchievementsClient.cs` | RetroAchievements.org API for achievement lists, user progress |

#### Infrastructure/

| File | Purpose |
|------|---------|
| `IpcService.cs` | gRPC service definition over Named Pipes, handles URI/command passing |
| `SingleInstanceLock.cs` | Named Mutex for single instance, IPC client fallback |
| `ConfigurationManager.cs` | JSON configuration loading, environment variable support |
| `LoggingConfiguration.cs` | Serilog configuration with file and console sinks |

---

### SaveState.UI Project

#### Views/

| File | Purpose |
|------|---------|
| `MainWindow.axaml` | Root window with sidebar navigation, content area, title bar |
| `GameGridView.axaml` | ItemsRepeater with GameCard template, virtualization for performance |
| `GameDetailView.axaml` | Full game details: cover, background, description, launch button, achievements |
| `SettingsView.axaml` | Settings panels: General, Library, Providers, Emulators, Advanced |
| `LibraryView.axaml` | Filterable game list with platform/source/collection filters |

#### ViewModels/

| File | Purpose |
|------|---------|
| `MainWindowViewModel.cs` | Navigation state, current view, sidebar toggle, search command |
| `GameGridViewModel.cs` | ObservableCollection<GameViewModel>, sort/filter logic, selection |
| `GameDetailViewModel.cs` | Selected game binding, play command, metadata refresh |
| `SettingsViewModel.cs` | Settings model binding, save/cancel commands, provider toggles |
| `LibraryViewModel.cs` | Advanced filtering, collection management, bulk operations |
| `ViewModelBase.cs` | CommunityToolkit.Mvvm ObservableObject with SetProperty helper |

#### Controls/

| File | Purpose |
|------|---------|
| `GameCard.axaml` | Cover image, title overlay, platform badge, play button hover |
| `SidebarNavigation.axaml` | Navigation items: Library, Collections, Statistics, Settings |
| `SearchBox.axaml` | TextBox with icon, clear button, autocomplete popup |

#### Themes/

| File | Purpose |
|------|---------|
| `DarkTheme.axaml` | Dark color palette (backgrounds, accents, text colors) |
| `LightTheme.axaml` | Light color palette with accessibility contrast ratios |
| `SharedStyles.axaml` | Button, TextBox, ListItem styles shared across themes |

---

### SaveState.App Project

| File | Purpose |
|------|---------|
| `Program.cs` | Host builder setup, DI configuration, single-instance check, Avalonia startup |
| `appsettings.json` | Configuration: database path, logging level, provider settings |
| `rd.xml` | Runtime directives for Native AOT to preserve reflection targets |

---

### Test Projects

| Project | Purpose |
|---------|---------|
| `SaveState.Core.Tests` | xUnit unit tests for services, providers, data layer |
| `SaveState.Integration.Tests` | End-to-end tests with real database, simulated store files |

---

### Build & CI

| File | Purpose |
|------|---------|
| `build.ps1` | Windows build script: restore, build, test |
| `publish-win-x64.ps1` | AOT publish for Windows x64 with trimming |
| `ci.yml` | GitHub Actions: build matrix (win/linux/mac), test, artifact upload |
| `release.yml` | Tag-triggered release: publish all platforms, create GitHub release |

---

### Installer

| File | Purpose |
|------|---------|
| `SaveState.iss` | Inno Setup script for Windows installer (.exe) |
| `org.savestate.SaveState.yml` | Flatpak manifest for Linux distribution |
| `org.savestate.desktop` | Linux desktop entry for application menu |

---

## NuGet Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `Avalonia` | 11.x | Cross-platform UI framework |
| `Avalonia.Desktop` | 11.x | Windows/Linux/macOS desktop support |
| `Avalonia.Themes.Fluent` | 11.x | Fluent design theme |
| `CommunityToolkit.Mvvm` | 8.x | MVVM infrastructure (ObservableObject, RelayCommand) |
| `Microsoft.EntityFrameworkCore` | 9.x | ORM for database access |
| `Microsoft.EntityFrameworkCore.Sqlite` | 9.x | SQLite database provider |
| `Grpc.Net.Client` | 2.x | gRPC client for IPC |
| `Grpc.AspNetCore` | 2.x | gRPC server for IPC |
| `Serilog` | 4.x | Structured logging |
| `VdfParser` | 1.x | Steam VDF file parsing |
| `Microsoft.WebView2` | Latest | Embedded browser for web-based UIs |

---

## Build Configurations

| Configuration | Purpose | AOT | Trimming |
|---------------|---------|-----|----------|
| `Debug` | Development and debugging | No | No |
| `Release` | Standard release build | No | No |
| `Release-AOT` | Production Native AOT build | Yes | Yes |

---

## File Count Summary

| Category | Count |
|----------|-------|
| C# Source Files | ~60 |
| AXAML View Files | ~12 |
| Test Files | ~12 |
| Configuration Files | ~10 |
| Documentation Files | ~6 |
| Build Scripts | ~5 |
| **Total** | ~105 files |

---

*Generated from SaveState WhitePaper (SS-WP-001) and Implementation Guide (SS-IG-001)*
