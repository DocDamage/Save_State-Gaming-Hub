# SaveState Reborn: Complete File Tree
## Project Structure & File Descriptions

**Document ID:** SS-FT-001  
**Revision:** 2.0  
**Date:** 2025-01-27  
**Last Updated:** 2025-01-27

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
│   │   │   ├── 📄 PlaySession.cs              # Play session tracking data
│   │   │   ├── 📄 Emulator.cs                 # Emulator configuration entity
│   │   │   ├── 📄 RomFolder.cs                # ROM folder scanning configuration
│   │   │   ├── 📄 GameActivity.cs              # Game activity tracking
│   │   │   ├── 📄 KnowledgeEntry.cs           # RAG knowledge base entries
│   │   │   └── 📄 AnomalyLog.cs               # Memory anomaly logging
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
│   │   │   ├── 📄 ICollectionService.cs       # Collection management contract
│   │   │   ├── 📄 IEmulatorService.cs         # Emulator management contract
│   │   │   ├── 📄 IAiService.cs               # AI service contract
│   │   │   ├── 📄 IKnowledgeService.cs        # Knowledge base service contract
│   │   │   ├── 📄 IMemoryScannerService.cs    # Memory scanning service contract
│   │   │   ├── 📄 IMemoryAnomalyService.cs    # Memory anomaly detection contract
│   │   │   ├── 📄 IProcessService.cs          # Process management contract
│   │   │   ├── 📄 ITrainerService.cs          # Trainer generation service contract
│   │   │   ├── 📄 IVectorStoreService.cs      # Vector store service contract
│   │   │   ├── 📄 IEmbeddingService.cs        # Embedding service contract
│   │   │   ├── 📄 IVoiceService.cs            # Voice service contract
│   │   │   └── 📄 IAppConfiguration.cs        # Application configuration contract
│   │   │
│   │   ├── 📁 Services/                       # Business logic services
│   │   │   ├── 📄 GameService.cs              # CRUD operations for games
│   │   │   ├── 📄 ProviderManager.cs          # Registers and manages all game providers
│   │   │   ├── 📄 CollectionService.cs        # Collection management operations
│   │   │   ├── 📄 RomScannerService.cs        # Scans directories for ROMs, matches to database
│   │   │   ├── 📄 EmulatorService.cs          # Emulator configuration and management
│   │   │   ├── 📄 GameSessionMonitor.cs       # Monitors active game sessions
│   │   │   ├── 📄 ProcessService.cs           # Process management and monitoring
│   │   │   ├── 📄 TrainerService.cs           # Trainer generation and management
│   │   │   ├── 📄 ImportExportService.cs      # Import/export functionality
│   │   │   │
│   │   │   ├── 📁 Ai/                         # AI-powered services
│   │   │   │   ├── 📄 LlmService.cs           # LLM provider abstraction
│   │   │   │   ├── 📄 RagService.cs           # Retrieval-Augmented Generation
│   │   │   │   ├── 📄 BmadService.cs          # Behavior Modulation and Adaptation
│   │   │   │   ├── 📄 AdvancedAiService.cs    # Advanced AI orchestration
│   │   │   │   ├── 📄 ProductionAiService.cs  # Production AI service
│   │   │   │   ├── 📄 UltimateAiOrchestrator.cs # Ultimate AI orchestrator
│   │   │   │   ├── 📄 ResilientAiService.cs   # Resilient AI with fallbacks
│   │   │   │   ├── 📄 StableDiffusionService.cs # Image generation service
│   │   │   │   ├── 📄 AiServiceProvider.cs    # AI service locator
│   │   │   │   ├── 📄 CheatAgentService.cs    # AI cheat detection agent
│   │   │   │   ├── 📁 Memory/                 # Memory stratification services
│   │   │   │   ├── 📁 Orchestration/          # AI orchestration services
│   │   │   │   ├── 📁 Validation/             # AI output validation
│   │   │   │   ├── 📁 Governance/             # AI governance and safety
│   │   │   │   ├── 📁 Events/                 # AI event bus
│   │   │   │   ├── 📁 Telemetry/              # AI telemetry and monitoring
│   │   │   │   ├── 📁 Testing/                 # AI testing harness
│   │   │   │   ├── 📁 Latency/                # Latency management
│   │   │   │   ├── 📁 Optimization/            # AI optimization services
│   │   │   │   ├── 📁 Prompts/                # Prompt management
│   │   │   │   ├── 📁 Persona/                # Persona management
│   │   │   │   ├── 📁 Trust/                  # Trust modeling
│   │   │   │   ├── 📁 Tools/                  # AI tools integration
│   │   │   │   ├── 📁 Resilience/             # Resilience patterns
│   │   │   │   ├── 📁 Uncertainty/             # Uncertainty handling
│   │   │   │   ├── 📁 Safety/                  # Safety mechanisms
│   │   │   │   ├── 📁 Core/                   # Core AI components
│   │   │   │   └── 📁 Emotion/                 # Emotion tagging
│   │   │   │
│   │   │   ├── 📁 Memory/                     # Memory management services
│   │   │   │   ├── 📄 MemoryProfileService.cs # Game memory profile management
│   │   │   │   ├── 📄 GameMemoryProfile.cs    # Game memory profile model
│   │   │   │   ├── 📄 GameMemoryProfiles.cs   # Predefined memory profiles
│   │   │   │   ├── 📄 IMemoryReader.cs        # Memory reading interface
│   │   │   │   └── 📄 TrainerGeneratorService.cs # Trainer generation from memory
│   │   │   │
│   │   │   ├── 📁 GameState/                  # Game state management
│   │   │   │   ├── 📄 WorldStateService.cs    # World state management
│   │   │   │   └── 📄 StateInjector.cs        # State injection service
│   │   │   │
│   │   │   ├── 📁 Rules/                      # Rules engine
│   │   │   │   ├── 📄 RuleEngine.cs           # Rules engine implementation
│   │   │   │   ├── 📄 RuleSet.cs              # Rule set definitions
│   │   │   │   ├── 📄 RuleModels.cs           # Rule model definitions
│   │   │   │   └── 📄 ActionValidator.cs      # Action validation
│   │   │   │
│   │   │   ├── 📁 Timeline/                   # Timeline services
│   │   │   │   ├── 📄 TimelineService.cs     # Timeline management
│   │   │   │   ├── 📄 StateDeltaService.cs    # State delta tracking
│   │   │   │   └── 📄 RewindService.cs        # State rewind functionality
│   │   │   │
│   │   │   ├── 📁 Player/                     # Player modeling
│   │   │   │   ├── 📄 PlayerModelService.cs  # Player model service
│   │   │   │   ├── 📄 EnhancedPlayerModelService.cs # Enhanced player modeling
│   │   │   │   └── 📄 BehaviorTracker.cs     # Behavior tracking
│   │   │   │
│   │   │   ├── 📁 Rom/                        # ROM services
│   │   │   │   ├── 📄 CheatService.cs        # Cheat code management
│   │   │   │   └── 📄 PatchService.cs         # ROM patching service
│   │   │   │
│   │   │   ├── 📁 Mugen/                      # MUGEN fighting game services
│   │   │   │   ├── 📄 MugenService.cs        # MUGEN service
│   │   │   │   ├── 📄 CharacterFusionService.cs # Character fusion
│   │   │   │   ├── 📄 CrossGameBattleService.cs # Cross-game battles
│   │   │   │   ├── 📄 MugenTournamentService.cs # Tournament management
│   │   │   │   ├── 📄 MugenFighter.cs         # Fighter model
│   │   │   │   └── 📄 MugenStage.cs           # Stage model
│   │   │   │
│   │   │   ├── 📁 EmulatorEnhancements/       # Emulator enhancement services
│   │   │   │   ├── 📄 DreamSequenceService.cs # Dream sequence generation
│   │   │   │   ├── 📄 MemoryEvolutionService.cs # Memory evolution
│   │   │   │   ├── 📄 ShaderStudioService.cs  # Shader studio
│   │   │   │   ├── 📄 TimeCapsuleService.cs  # Time capsule service
│   │   │   │   ├── 📄 LiveCommentaryService.cs # Live commentary
│   │   │   │   └── 📄 RetroRewindService.cs  # Retro rewind service
│   │   │   │
│   │   │   ├── 📁 Media/                      # Media services
│   │   │   │   ├── 📄 ScreenshotService.cs    # Screenshot capture
│   │   │   │   ├── 📄 RecordingService.cs    # Game recording
│   │   │   │   └── 📄 MontageGenerator.cs    # Montage generation
│   │   │   │
│   │   │   ├── 📁 Audio/                      # Audio services
│   │   │   │   ├── 📄 AudioService.cs        # Audio management
│   │   │   │   └── 📄 TtsService.cs          # Text-to-speech
│   │   │   │
│   │   │   ├── 📁 Input/                      # Input services
│   │   │   │   ├── 📄 GamepadService.cs      # Gamepad management
│   │   │   │   └── 📄 HotkeyService.cs       # Hotkey management
│   │   │   │
│   │   │   ├── 📁 Netplay/                    # Netplay services
│   │   │   │   ├── 📄 NetplayService.cs      # Netplay management
│   │   │   │   └── 📄 SpectatorService.cs   # Spectator mode
│   │   │   │
│   │   │   ├── 📁 Cloud/                      # Cloud services
│   │   │   │   ├── 📄 CloudSyncService.cs    # Cloud synchronization
│   │   │   │   └── 📄 BackupService.cs       # Backup service
│   │   │   │
│   │   │   ├── 📁 Account/                    # Account services
│   │   │   │   ├── 📄 AuthService.cs         # Authentication
│   │   │   │   ├── 📄 ProfileService.cs      # User profile
│   │   │   │   ├── 📄 FriendsService.cs      # Friends management
│   │   │   │   └── 📄 LeaderboardService.cs  # Leaderboards
│   │   │   │
│   │   │   ├── 📁 Gamification/               # Gamification services
│   │   │   │   ├── 📄 AchievementService.cs  # Achievement tracking
│   │   │   │   └── 📄 ChallengeService.cs    # Challenge management
│   │   │   │
│   │   │   ├── 📁 Accessibility/              # Accessibility services
│   │   │   │   ├── 📄 AccessibilityService.cs # Accessibility features
│   │   │   │   ├── 📄 ThemeService.cs        # Theme management
│   │   │   │   └── 📄 NotificationService.cs # Notifications
│   │   │   │
│   │   │   ├── 📁 Mods/                       # Mod SDK services
│   │   │   │   ├── 📄 ModGateway.cs          # Mod gateway
│   │   │   │   ├── 📄 ModValidator.cs         # Mod validation
│   │   │   │   └── 📄 SandboxEnvironment.cs  # Sandbox environment
│   │   │   │
│   │   │   ├── 📄 IgdbService.cs              # IGDB API client
│   │   │   ├── 📄 SteamGridDbService.cs       # SteamGridDB API client
│   │   │   ├── 📄 OpenAiService.cs            # OpenAI integration
│   │   │   ├── 📄 GeminiService.cs            # Google Gemini integration
│   │   │   ├── 📄 KnowledgeService.cs         # Knowledge base service
│   │   │   ├── 📄 MemoryScannerService.cs     # Memory scanning
│   │   │   ├── 📄 MemoryAnomalyService.cs     # Memory anomaly detection
│   │   │   ├── 📄 EmbeddingService.cs         # Embedding service
│   │   │   ├── 📄 VectorStoreService.cs        # Vector store service
│   │   │   └── 📄 VoiceService.cs             # Voice service
│   │   │
│   │   ├── 📁 Providers/                      # Store-specific provider implementations
│   │   │   ├── 📄 SteamProvider.cs            # Steam client integration via Steam Web API
│   │   │   ├── 📄 GogProvider.cs                # GOG Galaxy database parsing
│   │   │   ├── 📄 EpicProvider.cs               # Epic Games Store manifest reading
│   │   │   ├── 📄 XboxProvider.cs               # Xbox/Game Pass UWP API integration
│   │   │   ├── 📄 EaProvider.cs                 # EA App registry/database parsing
│   │   │   └── 📄 UbisoftProvider.cs            # Ubisoft Connect registry parsing
│   │   │   # Note: Amazon, itch.io, Humble, PlayStation providers planned but not yet implemented
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
│   │   │   ├── 📄 GameGridView.axaml         # Grid display of games with covers
│   │   │   ├── 📄 GameGridView.axaml.cs       # Code-behind for GameGridView
│   │   │   ├── 📄 GameDetailsView.axaml       # Detailed game information panel
│   │   │   ├── 📄 GameDetailsView.axaml.cs   # Code-behind for GameDetailsView
│   │   │   ├── 📄 SettingsView.axaml          # Application settings panel
│   │   │   ├── 📄 SettingsView.axaml.cs       # Code-behind for SettingsView
│   │   │   ├── 📄 CollectionsView.axaml       # Collections management
│   │   │   ├── 📄 CollectionsView.axaml.cs   # Code-behind for CollectionsView
│   │   │   ├── 📄 RomManagerView.axaml        # ROM management view
│   │   │   ├── 📄 RomManagerView.axaml.cs     # Code-behind for RomManagerView
│   │   │   ├── 📄 AiAssistantView.axaml       # AI assistant interface
│   │   │   ├── 📄 AiAssistantView.axaml.cs    # Code-behind for AiAssistantView
│   │   │   ├── 📄 AiSettingsView.axaml        # AI settings configuration
│   │   │   ├── 📄 AiSettingsView.axaml.cs     # Code-behind for AiSettingsView
│   │   │   ├── 📄 AchievementsView.axaml      # Achievements display
│   │   │   ├── 📄 AchievementsView.axaml.cs   # Code-behind for AchievementsView
│   │   │   ├── 📄 ChallengesView.axaml        # Challenges view
│   │   │   ├── 📄 ChallengesView.axaml.cs    # Code-behind for ChallengesView
│   │   │   ├── 📄 StatisticsView.axaml         # Statistics dashboard
│   │   │   ├── 📄 StatisticsView.axaml.cs     # Code-behind for StatisticsView
│   │   │   ├── 📄 KnowledgeView.axaml         # Knowledge base view
│   │   │   ├── 📄 KnowledgeView.axaml.cs      # Code-behind for KnowledgeView
│   │   │   ├── 📄 TrainerGeneratorView.axaml  # Trainer generator interface
│   │   │   ├── 📄 TrainerGeneratorView.axaml.cs # Code-behind for TrainerGeneratorView
│   │   │   ├── 📄 MugenPlayerView.axaml       # MUGEN player view
│   │   │   ├── 📄 MugenPlayerView.axaml.cs    # Code-behind for MugenPlayerView
│   │   │   ├── 📄 CharacterFusionView.axaml   # Character fusion interface
│   │   │   ├── 📄 CharacterFusionView.axaml.cs # Code-behind for CharacterFusionView
│   │   │   ├── 📄 CrossGameBattleView.axaml    # Cross-game battle view
│   │   │   ├── 📄 CrossGameBattleView.axaml.cs # Code-behind for CrossGameBattleView
│   │   │   ├── 📄 DreamSequenceView.axaml     # Dream sequence view
│   │   │   ├── 📄 DreamSequenceView.axaml.cs   # Code-behind for DreamSequenceView
│   │   │   ├── 📄 MemoryEvolutionView.axaml   # Memory evolution view
│   │   │   ├── 📄 MemoryEvolutionView.axaml.cs # Code-behind for MemoryEvolutionView
│   │   │   ├── 📄 ShaderStudioView.axaml       # Shader studio view
│   │   │   ├── 📄 ShaderStudioView.axaml.cs    # Code-behind for ShaderStudioView
│   │   │   ├── 📄 TimeCapsuleView.axaml        # Time capsule view
│   │   │   ├── 📄 TimeCapsuleView.axaml.cs     # Code-behind for TimeCapsuleView
│   │   │   ├── 📄 LiveCommentaryView.axaml     # Live commentary view
│   │   │   ├── 📄 LiveCommentaryView.axaml.cs # Code-behind for LiveCommentaryView
│   │   │   └── 📄 RetroRewindView.axaml        # Retro rewind view
│   │   │   └── 📄 RetroRewindView.axaml.cs      # Code-behind for RetroRewindView
│   │   │
│   │   ├── 📁 ViewModels/                     # MVVM ViewModels
│   │   │   ├── 📄 MainWindowViewModel.cs      # Main window state and navigation
│   │   │   ├── 📄 GameGridViewModel.cs        # Game grid data binding and commands
│   │   │   ├── 📄 GameDetailsViewModel.cs     # Single game detail display
│   │   │   ├── 📄 SettingsViewModel.cs        # Settings management and persistence
│   │   │   ├── 📄 CollectionsViewModel.cs     # Collections management
│   │   │   ├── 📄 RomManagerViewModel.cs      # ROM management
│   │   │   ├── 📄 AiAssistantViewModel.cs     # AI assistant interface
│   │   │   ├── 📄 AiSettingsViewModel.cs      # AI settings configuration
│   │   │   ├── 📄 AchievementsViewModel.cs    # Achievements display
│   │   │   ├── 📄 ChallengesViewModel.cs      # Challenges view
│   │   │   ├── 📄 StatisticsViewModel.cs      # Statistics dashboard
│   │   │   ├── 📄 KnowledgeViewModel.cs       # Knowledge base view
│   │   │   ├── 📄 TrainerGeneratorViewModel.cs # Trainer generator
│   │   │   ├── 📄 MugenPlayerViewModel.cs     # MUGEN player
│   │   │   ├── 📄 CharacterFusionViewModel.cs # Character fusion
│   │   │   ├── 📄 CrossGameBattleViewModel.cs # Cross-game battle
│   │   │   ├── 📄 DreamSequenceViewModel.cs    # Dream sequence
│   │   │   ├── 📄 MemoryEvolutionViewModel.cs # Memory evolution
│   │   │   ├── 📄 ShaderStudioViewModel.cs     # Shader studio
│   │   │   ├── 📄 TimeCapsuleViewModel.cs      # Time capsule
│   │   │   ├── 📄 LiveCommentaryViewModel.cs   # Live commentary
│   │   │   ├── 📄 RetroRewindViewModel.cs      # Retro rewind
│   │   │   ├── 📄 ViewModelBase.cs            # Base class with INotifyPropertyChanged
│   │   │   └── 📄 Converters.cs                # Value converters
│   │   │
│   │   ├── 📁 Controls/                       # Reusable custom controls
│   │   │   └── (Custom controls defined inline in views)
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
| `Emulator.cs` | Emulator configuration with Id, Name, ExecutablePath, Configuration |
| `RomFolder.cs` | ROM folder scanning configuration with Id, Path, PlatformId |
| `GameActivity.cs` | Game activity tracking with Id, GameId, ActivityType, Timestamp, Data |
| `KnowledgeEntry.cs` | RAG knowledge base entry with Id, Title, Content, Embeddings, Metadata |
| `AnomalyLog.cs` | Memory anomaly logging with Id, GameId, AnomalyType, Timestamp, Details |

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

**Core Services:**
| File | Purpose |
|------|---------|
| `GameService.cs` | Implements IGameService using EF Core, handles caching, validation |
| `ProviderManager.cs` | Registry pattern for IGameProvider implementations, discovery, aggregation |
| `CollectionService.cs` | Collection management operations |
| `RomScannerService.cs` | File system scanning with extension filtering, hash matching, database lookup |
| `EmulatorService.cs` | Emulator configuration and management |
| `GameSessionMonitor.cs` | Monitors active game sessions and process tracking |
| `ProcessService.cs` | Process management and monitoring |
| `TrainerService.cs` | Trainer generation and management |
| `ImportExportService.cs` | Import/export functionality |

**AI Services:**
| File | Purpose |
|------|---------|
| `LlmService.cs` | LLM provider abstraction (OpenAI, Gemini, Ollama) |
| `RagService.cs` | Retrieval-Augmented Generation for game knowledge |
| `BmadService.cs` | Behavior Modulation and Adaptation |
| `AdvancedAiService.cs` | Advanced AI orchestration service |
| `ProductionAiService.cs` | Production-ready AI service |
| `UltimateAiOrchestrator.cs` | Ultimate AI orchestrator with pipeline |
| `ResilientAiService.cs` | Resilient AI with fallbacks and error handling |
| `StableDiffusionService.cs` | Image generation service |
| `CheatAgentService.cs` | AI cheat detection and trainer generation agent |
| `AiServiceProvider.cs` | AI service locator and dependency injection |

**Memory & Game State:**
| File | Purpose |
|------|---------|
| `MemoryProfileService.cs` | Game memory profile management |
| `GameMemoryProfile.cs` | Game memory profile model |
| `GameMemoryProfiles.cs` | Predefined memory profiles for popular games |
| `IMemoryReader.cs` | Memory reading interface |
| `TrainerGeneratorService.cs` | Trainer generation from memory scans |
| `WorldStateService.cs` | World state management |
| `StateInjector.cs` | State injection service |

**Rules & Timeline:**
| File | Purpose |
|------|---------|
| `RuleEngine.cs` | Rules engine implementation |
| `RuleSet.cs` | Rule set definitions |
| `TimelineService.cs` | Timeline management |
| `StateDeltaService.cs` | State delta tracking |
| `RewindService.cs` | State rewind functionality |

**MUGEN Services:**
| File | Purpose |
|------|---------|
| `MugenService.cs` | MUGEN service |
| `CharacterFusionService.cs` | Character fusion |
| `CrossGameBattleService.cs` | Cross-game battles |
| `MugenTournamentService.cs` | Tournament management |

**Emulator Enhancements:**
| File | Purpose |
|------|---------|
| `DreamSequenceService.cs` | Dream sequence generation |
| `MemoryEvolutionService.cs` | Memory evolution |
| `ShaderStudioService.cs` | Shader studio |
| `TimeCapsuleService.cs` | Time capsule service |
| `LiveCommentaryService.cs` | Live commentary |
| `RetroRewindService.cs` | Retro rewind service |

**Media & Audio:**
| File | Purpose |
|------|---------|
| `ScreenshotService.cs` | Screenshot capture |
| `RecordingService.cs` | Game recording |
| `MontageGenerator.cs` | Montage generation |
| `AudioService.cs` | Audio management |
| `TtsService.cs` | Text-to-speech |

**Other Services:**
| File | Purpose |
|------|---------|
| `IgdbService.cs` | IGDB API client for game metadata |
| `SteamGridDbService.cs` | SteamGridDB API for artwork |
| `OpenAiService.cs` | OpenAI integration |
| `GeminiService.cs` | Google Gemini integration |
| `KnowledgeService.cs` | Knowledge base service |
| `MemoryScannerService.cs` | Memory scanning |
| `MemoryAnomalyService.cs` | Memory anomaly detection |
| `EmbeddingService.cs` | Embedding service |
| `VectorStoreService.cs` | Vector store service |
| `VoiceService.cs` | Voice service |
| `AchievementService.cs` | Achievement tracking |
| `ChallengeService.cs` | Challenge management |
| `CloudSyncService.cs` | Cloud synchronization |
| `BackupService.cs` | Backup service |
| `AuthService.cs` | Authentication |
| `ProfileService.cs` | User profile |
| `FriendsService.cs` | Friends management |
| `LeaderboardService.cs` | Leaderboards |
| `AccessibilityService.cs` | Accessibility features |
| `ThemeService.cs` | Theme management |
| `NotificationService.cs` | Notifications |
| `GamepadService.cs` | Gamepad management |
| `HotkeyService.cs` | Hotkey management |
| `NetplayService.cs` | Netplay management |
| `SpectatorService.cs` | Spectator mode |
| `ModGateway.cs` | Mod gateway |
| `ModValidator.cs` | Mod validation |
| `SandboxEnvironment.cs` | Sandbox environment |

#### Providers/

| File | Purpose |
|------|---------|
| `SteamProvider.cs` | Parses `steamapps/libraryfolders.vdf`, uses Steam Web API for owned games |
| `GogProvider.cs` | Reads GOG Galaxy database (`galaxy-2.0.db`), game file detection |
| `EpicProvider.cs` | Parses `.egstore` manifests, Epic Games Launcher registry |
| `XboxProvider.cs` | Uses Windows.Gaming.XboxLive APIs, Game Pass catalog |
| `EaProvider.cs` | Reads EA App/Origin registry keys, local game database |
| `UbisoftProvider.cs` | Parses Ubisoft Connect SQLite database, registry detection |
| *Note: Amazon, itch.io, Humble, PlayStation providers are planned but not yet implemented* |

#### Metadata Services/

| File | Purpose |
|------|---------|
| `IgdbService.cs` | IGDB Twitch API client with OAuth, game search, genre/theme fetching |
| `SteamGridDbService.cs` | SteamGridDB REST API for covers, heroes, logos, icons |
| *Note: Metadata services are located in Services/ directory, not a separate Metadata/ folder* |

#### Infrastructure/

| File | Purpose |
|------|---------|
| *Note: Infrastructure components are integrated into services. gRPC proto definitions are in Protos/ directory* |

---

### SaveState.UI Project

#### Views/

| File | Purpose |
|------|---------|
| `MainWindow.axaml` | Root window with sidebar navigation, content area, title bar |
| `GameGridView.axaml` | ItemsRepeater with GameCard template, virtualization for performance |
| `GameDetailsView.axaml` | Full game details: cover, background, description, launch button, achievements |
| `SettingsView.axaml` | Settings panels: General, Library, Providers, Emulators, Advanced |
| `CollectionsView.axaml` | Collections management interface |
| `RomManagerView.axaml` | ROM management interface |
| `AiAssistantView.axaml` | AI assistant chat interface |
| `AiSettingsView.axaml` | AI settings configuration |
| `AchievementsView.axaml` | Achievements display |
| `ChallengesView.axaml` | Challenges interface |
| `StatisticsView.axaml` | Statistics dashboard |
| `KnowledgeView.axaml` | Knowledge base browser |
| `TrainerGeneratorView.axaml` | Trainer generator interface |
| `MugenPlayerView.axaml` | MUGEN player interface |
| `CharacterFusionView.axaml` | Character fusion interface |
| `CrossGameBattleView.axaml` | Cross-game battle interface |
| `DreamSequenceView.axaml` | Dream sequence interface |
| `MemoryEvolutionView.axaml` | Memory evolution interface |
| `ShaderStudioView.axaml` | Shader studio interface |
| `TimeCapsuleView.axaml` | Time capsule interface |
| `LiveCommentaryView.axaml` | Live commentary interface |
| `RetroRewindView.axaml` | Retro rewind interface |

#### ViewModels/

| File | Purpose |
|------|---------|
| `MainWindowViewModel.cs` | Navigation state, current view, sidebar toggle, search command |
| `GameGridViewModel.cs` | ObservableCollection<GameViewModel>, sort/filter logic, selection |
| `GameDetailsViewModel.cs` | Selected game binding, play command, metadata refresh |
| `SettingsViewModel.cs` | Settings model binding, save/cancel commands, provider toggles |
| `CollectionsViewModel.cs` | Collections management |
| `RomManagerViewModel.cs` | ROM management |
| `AiAssistantViewModel.cs` | AI assistant chat interface |
| `AiSettingsViewModel.cs` | AI settings configuration |
| `AchievementsViewModel.cs` | Achievements display |
| `ChallengesViewModel.cs` | Challenges interface |
| `StatisticsViewModel.cs` | Statistics dashboard |
| `KnowledgeViewModel.cs` | Knowledge base browser |
| `TrainerGeneratorViewModel.cs` | Trainer generator |
| `MugenPlayerViewModel.cs` | MUGEN player |
| `CharacterFusionViewModel.cs` | Character fusion |
| `CrossGameBattleViewModel.cs` | Cross-game battle |
| `DreamSequenceViewModel.cs` | Dream sequence |
| `MemoryEvolutionViewModel.cs` | Memory evolution |
| `ShaderStudioViewModel.cs` | Shader studio |
| `TimeCapsuleViewModel.cs` | Time capsule |
| `LiveCommentaryViewModel.cs` | Live commentary |
| `RetroRewindViewModel.cs` | Retro rewind |
| `ViewModelBase.cs` | CommunityToolkit.Mvvm ObservableObject with SetProperty helper |
| `Converters.cs` | Value converters for data binding |

#### Controls/

| File | Purpose |
|------|---------|
| *Note: Custom controls are defined inline in views rather than as separate control files* |

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
| C# Source Files | ~200+ |
| AXAML View Files | ~22 |
| Test Files | ~7 |
| Configuration Files | ~10 |
| Documentation Files | ~10 |
| Build Scripts | ~5 |
| **Total** | ~250+ files |

*Note: Actual file counts are significantly higher than originally estimated due to extensive AI services, MUGEN integration, emulator enhancements, and additional features implemented.*

---

*Generated from SaveState WhitePaper (SS-WP-001) and Implementation Guide (SS-IG-001)*
