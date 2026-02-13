using System;
using Microsoft.EntityFrameworkCore.Migrations;

#pragma warning disable CA1505

#nullable disable

namespace SaveState.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
public partial class OpenMKProgressAndMatchState : Migration
{
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Achievements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GameId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IconPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Points = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    TargetValue = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    Criteria = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Achievements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiModels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ModelId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    MaxTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    Temperature = table.Column<float>(type: "REAL", precision: 3, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiModels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Challenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Challenges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Developers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FoundedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Website = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Developers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalApiKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    AppName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Scopes = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalApiKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Friends",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AvatarUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Platform = table.Column<string>(type: "TEXT", nullable: false),
                    PlatformUserId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsOnline = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CurrentGame = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    LastSeenAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Friends", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GameMedia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GameId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MediaType = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    FileFormat = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Width = table.Column<int>(type: "INTEGER", nullable: true),
                    Height = table.Column<int>(type: "INTEGER", nullable: true),
                    DurationSeconds = table.Column<int>(type: "INTEGER", nullable: true),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    IsFavorite = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsPublic = table.Column<bool>(type: "INTEGER", nullable: false),
                    ThumbnailPath = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameMedia", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GameMods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GameId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Author = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    InstallPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    SourceUrl = table.Column<string>(type: "TEXT", nullable: true),
                    LoadOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    HasConfiguration = table.Column<bool>(type: "INTEGER", nullable: false),
                    InstalledAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameMods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GameNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GameId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    IsPinned = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameNotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GamingGoals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    TargetValue = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentValue = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    SpecificGameId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GamingGoals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Genres",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genres", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeRecords",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Embedding = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Content = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: false),
                    Metadata = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    IndexedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastAccessedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AccessCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    RelevanceScore = table.Column<float>(type: "REAL", precision: 4, scale: 3, nullable: false, defaultValue: 1f)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Leaderboards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    Scope = table.Column<int>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leaderboards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MemorySnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Address = table.Column<long>(type: "INTEGER", nullable: false),
                    Data = table.Column<byte[]>(type: "BLOB", nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProcessName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    ProcessId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemorySnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MemoryWatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GameId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "TEXT", nullable: false),
                    DataType = table.Column<string>(type: "TEXT", nullable: false),
                    CurrentValue = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    PreviousValue = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    LastChangedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    IsFrozen = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ChangeCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemoryWatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MugenCharacterCollections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsPublic = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MugenCharacterCollections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MugenCharacters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: false),
                    Author = table.Column<string>(type: "TEXT", nullable: false),
                    DefinitionFilePath = table.Column<string>(type: "TEXT", nullable: false),
                    CharacterDirectory = table.Column<string>(type: "TEXT", nullable: false),
                    CommandFile = table.Column<string>(type: "TEXT", nullable: true),
                    ConstantsFile = table.Column<string>(type: "TEXT", nullable: true),
                    StatesFile = table.Column<string>(type: "TEXT", nullable: true),
                    CommonStatesFile = table.Column<string>(type: "TEXT", nullable: true),
                    Directories_SpriteDirectory = table.Column<string>(type: "TEXT", nullable: true),
                    Directories_SoundDirectory = table.Column<string>(type: "TEXT", nullable: true),
                    Directories_PaletteDirectory = table.Column<string>(type: "TEXT", nullable: true),
                    PaletteInfo_PaletteCount = table.Column<int>(type: "INTEGER", nullable: false),
                    PaletteInfo_PaletteFile = table.Column<string>(type: "TEXT", nullable: true),
                    ArcadeInfo_IntroStoryboard = table.Column<int>(type: "INTEGER", nullable: false),
                    ArcadeInfo_EndingStoryboard = table.Column<int>(type: "INTEGER", nullable: false),
                    Health = table.Column<int>(type: "INTEGER", nullable: false),
                    Attack = table.Column<int>(type: "INTEGER", nullable: false),
                    Defense = table.Column<int>(type: "INTEGER", nullable: false),
                    Speed = table.Column<int>(type: "INTEGER", nullable: false),
                    IsProjectileCharacter = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsRushdownCharacter = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsZoningCharacter = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasSuperArts = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasThrows = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasCommandGrab = table.Column<bool>(type: "INTEGER", nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    LastScannedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsValid = table.Column<bool>(type: "INTEGER", nullable: false),
                    ValidationErrors = table.Column<string>(type: "TEXT", nullable: true),
                    IsFavorite = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MugenCharacters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MugenMatchHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Player1CharacterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Player2CharacterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Result = table.Column<string>(type: "TEXT", nullable: false),
                    RoundsWonP1 = table.Column<int>(type: "INTEGER", nullable: false),
                    RoundsWonP2 = table.Column<int>(type: "INTEGER", nullable: false),
                    MatchDuration = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    PlayedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Mode = table.Column<string>(type: "TEXT", nullable: false),
                    ReplayPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MugenMatchHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MugenTournaments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Format = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    WinnerId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MugenTournaments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NetworkQualityHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LatencyMs = table.Column<int>(type: "INTEGER", nullable: false),
                    JitterMs = table.Column<int>(type: "INTEGER", nullable: false),
                    PacketLossPercent = table.Column<int>(type: "INTEGER", nullable: false),
                    BandwidthMbps = table.Column<int>(type: "INTEGER", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    MeasuredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SessionId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetworkQualityHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OpenMKCharacters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Bio = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Realm = table.Column<string>(type: "TEXT", nullable: false),
                    FightingStyle = table.Column<string>(type: "TEXT", nullable: false),
                    Alignment = table.Column<string>(type: "TEXT", nullable: false),
                    SpritePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    SoundPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DefinitionPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Ending = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    IsDefaultUnlocked = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    UnlockDescription = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    UnlockType = table.Column<string>(type: "TEXT", nullable: false),
                    UnlockRequiredValue = table.Column<int>(type: "INTEGER", nullable: true),
                    UnlockRequiredCharacter = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UnlockRequiredStage = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenMKCharacters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OpenMKCharacterUnlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CharacterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UnlockedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenMKCharacterUnlocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OpenMKMatchStates",
                columns: table => new
                {
                    MatchId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Player1CharacterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Player2CharacterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RoundNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Player1Health = table.Column<int>(type: "INTEGER", nullable: false),
                    Player2Health = table.Column<int>(type: "INTEGER", nullable: false),
                    Player1SuperBar = table.Column<int>(type: "INTEGER", nullable: false),
                    Player2SuperBar = table.Column<int>(type: "INTEGER", nullable: false),
                    Player1Wins = table.Column<int>(type: "INTEGER", nullable: false),
                    Player2Wins = table.Column<int>(type: "INTEGER", nullable: false),
                    RoundTimeRemaining = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    Phase = table.Column<int>(type: "INTEGER", nullable: false),
                    Player1CostumeName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Player2CostumeName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenMKMatchStates", x => x.MatchId);
                });

            migrationBuilder.CreateTable(
                name: "OpenMKUserProgress",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Koins = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenMKUserProgress", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Resource = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Platforms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ShortName = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Manufacturer = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ReleasedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Platforms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Publishers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FoundedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Website = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Publishers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsSystemRole = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SharedCollections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ShareCode = table.Column<string>(type: "TEXT", unicode: false, maxLength: 8, nullable: false),
                    IsPublic = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    DownloadCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedCollections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    PasswordSalt = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    IsEmailVerified = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VirtualCollections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    FilterExpression = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    IsSystemCollection = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VirtualCollections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserAchievements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AchievementId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CurrentProgress = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetProgress = table.Column<int>(type: "INTEGER", nullable: false),
                    IsUnlocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    UnlockedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Metadata = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAchievements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAchievements_Achievements_AchievementId",
                        column: x => x.AchievementId,
                        principalTable: "Achievements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChallengeParticipant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Progress = table.Column<double>(type: "REAL", nullable: false),
                    CurrentProgress = table.Column<double>(type: "REAL", nullable: false),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ChallengeId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeParticipant", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChallengeParticipant_Challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "Challenges",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ChallengeRequirement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    MetricType = table.Column<string>(type: "TEXT", nullable: false),
                    TargetValue = table.Column<double>(type: "REAL", nullable: false),
                    TargetMetric = table.Column<string>(type: "TEXT", nullable: false),
                    ChallengeId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeRequirement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChallengeRequirement_Challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "Challenges",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FriendActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FriendId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    GameTitle = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Details = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Platform = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FriendActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FriendActivities_Friends_FriendId",
                        column: x => x.FriendId,
                        principalTable: "Friends",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeaderboardRanking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LeaderboardId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: false),
                    Score = table.Column<double>(type: "REAL", nullable: false),
                    Rank = table.Column<int>(type: "INTEGER", nullable: false),
                    Metadata = table.Column<string>(type: "TEXT", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaderboardRanking", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaderboardRanking_Leaderboards_LeaderboardId",
                        column: x => x.LeaderboardId,
                        principalTable: "Leaderboards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MugenCollectionCharacters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CollectionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CharacterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsFavorite = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    AddedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MugenCollectionCharacters", x => x.Id);
                    table.UniqueConstraint("AK_MugenCollectionCharacters_CollectionId_CharacterId", x => new { x.CollectionId, x.CharacterId });
                    table.ForeignKey(
                        name: "FK_MugenCollectionCharacters_MugenCharacterCollections_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "MugenCharacterCollections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MugenCollectionCharacters_MugenCharacters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "MugenCharacters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MugenMatchupStats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Character1Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Character2Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TotalMatches = table.Column<int>(type: "INTEGER", nullable: false),
                    Character1Wins = table.Column<int>(type: "INTEGER", nullable: false),
                    Character2Wins = table.Column<int>(type: "INTEGER", nullable: false),
                    Draws = table.Column<int>(type: "INTEGER", nullable: false),
                    AverageMatchDuration = table.Column<TimeSpan>(type: "decimal(18,2)", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MugenMatchupStats", x => x.Id);
                    table.UniqueConstraint("AK_MugenMatchupStats_Character1Id_Character2Id", x => new { x.Character1Id, x.Character2Id });
                    table.ForeignKey(
                        name: "FK_MugenMatchupStats_MugenCharacters_Character1Id",
                        column: x => x.Character1Id,
                        principalTable: "MugenCharacters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MugenMatchupStats_MugenCharacters_Character2Id",
                        column: x => x.Character2Id,
                        principalTable: "MugenCharacters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MugenTrainingSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CharacterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OpponentCharacterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionType = table.Column<string>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RoundsPracticed = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    SuccessfulCombos = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    FailedCombos = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MugenTrainingSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MugenTrainingSessions_MugenCharacters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "MugenCharacters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MugenTrainingSessions_MugenCharacters_OpponentCharacterId",
                        column: x => x.OpponentCharacterId,
                        principalTable: "MugenCharacters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TournamentParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TournamentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CharacterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Seed = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Score = table.Column<int>(type: "INTEGER", nullable: false),
                    EliminatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentParticipants_MugenCharacters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "MugenCharacters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentParticipants_MugenTournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "MugenTournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Emulators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ExecutablePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Version = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PlatformId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CommandLineArgs = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    IsAvailable = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Emulators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Emulators_Platforms_PlatformId",
                        column: x => x.PlatformId,
                        principalTable: "Platforms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CoverImagePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    InstallPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    PlatformId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    SourceId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastPlayedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TotalPlayTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    ReleaseDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    UserRating = table.Column<double>(type: "REAL", nullable: true),
                    LaunchArguments = table.Column<string>(type: "TEXT", nullable: true),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Tags = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Games_Platforms_PlatformId",
                        column: x => x.PlatformId,
                        principalTable: "Platforms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RomFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    PlatformId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Region = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Version = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Checksum = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    ScannedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RomFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RomFiles_Platforms_PlatformId",
                        column: x => x.PlatformId,
                        principalTable: "Platforms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PermissionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SharedCollectionItems",
                columns: table => new
                {
                    CollectionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GameTitle = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedCollectionItems", x => new { x.CollectionId, x.GameTitle });
                    table.ForeignKey(
                        name: "FK_SharedCollectionItems_SharedCollections_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "SharedCollections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    KeyHash = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    KeyPrefix = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiKeys_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RoleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MugenDummyRecordings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TrainingSessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BehaviorType = table.Column<string>(type: "TEXT", nullable: false),
                    ActionSequence = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    IsSuccessful = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    ReplayPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ComboHits = table.Column<int>(type: "INTEGER", nullable: false),
                    ComboDamage = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MugenDummyRecordings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MugenDummyRecordings_MugenTrainingSessions_TrainingSessionId",
                        column: x => x.TrainingSessionId,
                        principalTable: "MugenTrainingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TournamentMatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TournamentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Round = table.Column<int>(type: "INTEGER", nullable: false),
                    MatchNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Player1CharacterId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Player2CharacterId = table.Column<Guid>(type: "TEXT", nullable: true),
                    WinnerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    TournamentParticipantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentMatches_MugenTournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "MugenTournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TournamentMatches_TournamentParticipants_TournamentParticipantId",
                        column: x => x.TournamentParticipantId,
                        principalTable: "TournamentParticipants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BacklogEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GameId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    EstimatedPlaytime = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    TargetCompletionDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BacklogEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BacklogEntries_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Backups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GameId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Backups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Backups_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ControllerProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    GameId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ControllerId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    MappingsJson = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControllerProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ControllerProfiles_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GameId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Path = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: true),
                    AddedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameFiles_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameGenre",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GenresId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameGenre", x => new { x.GameId, x.GenresId });
                    table.ForeignKey(
                        name: "FK_GameGenre_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameGenre_Genres_GenresId",
                        column: x => x.GenresId,
                        principalTable: "Genres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GameId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Rating = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Content = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    IsRecommended = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PlaytimeAtReview = table.Column<long>(type: "INTEGER", nullable: false),
                    ContainsSpoilers = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameReviews", x => x.Id);
                    table.CheckConstraint("CK_GameReview_Rating", "[Rating] >= 1 AND [Rating] <= 10");
                    table.ForeignKey(
                        name: "FK_GameReviews_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GameId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndReason = table.Column<int>(type: "INTEGER", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameSessions_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SaveStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GameId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ThumbnailPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PlaytimeAtSave = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    GameLocation = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ParentStateId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsFavorite = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    IsAutoSave = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    FileSizeBytes = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    BranchName = table.Column<string>(type: "TEXT", nullable: true),
                    IsCurrent = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaveStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaveStates_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VirtualCollectionGames",
                columns: table => new
                {
                    CollectionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GameId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    AddedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VirtualCollectionGames", x => new { x.CollectionId, x.GameId });
                    table.ForeignKey(
                        name: "FK_VirtualCollectionGames_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VirtualCollectionGames_VirtualCollections_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "VirtualCollections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SaveStateBranches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RootStateId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BranchName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaveStateBranches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaveStateBranches_SaveStates_RootStateId",
                        column: x => x.RootStateId,
                        principalTable: "SaveStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Achievements_GameId",
                table: "Achievements",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Achievements_IsActive",
                table: "Achievements",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Achievements_Name",
                table: "Achievements",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Achievements_Points",
                table: "Achievements",
                column: "Points");

            migrationBuilder.CreateIndex(
                name: "IX_Achievements_Type",
                table: "Achievements",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Achievements_Type_IsActive",
                table: "Achievements",
                columns: new[] { "Type", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AiModels_IsActive",
                table: "AiModels",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AiModels_LastUsedAt",
                table: "AiModels",
                column: "LastUsedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AiModels_ModelId",
                table: "AiModels",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_AiModels_Provider",
                table: "AiModels",
                column: "Provider");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_IsActive",
                table: "ApiKeys",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_KeyPrefix",
                table: "ApiKeys",
                column: "KeyPrefix");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_LastUsedAt",
                table: "ApiKeys",
                column: "LastUsedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_UserId",
                table: "ApiKeys",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BacklogEntries_GameId",
                table: "BacklogEntries",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Backups_CreatedAt",
                table: "Backups",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Backups_GameId",
                table: "Backups",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Backups_Type",
                table: "Backups",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeParticipant_ChallengeId",
                table: "ChallengeParticipant",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeRequirement_ChallengeId",
                table: "ChallengeRequirement",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_ControllerProfiles_ControllerId",
                table: "ControllerProfiles",
                column: "ControllerId");

            migrationBuilder.CreateIndex(
                name: "IX_ControllerProfiles_GameId",
                table: "ControllerProfiles",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_ControllerProfiles_GameId_IsDefault",
                table: "ControllerProfiles",
                columns: new[] { "GameId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_ControllerProfiles_Type",
                table: "ControllerProfiles",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_ControllerProfiles_Type_LastUsedAt",
                table: "ControllerProfiles",
                columns: new[] { "Type", "LastUsedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Developers_Name",
                table: "Developers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Emulators_IsAvailable",
                table: "Emulators",
                column: "IsAvailable");

            migrationBuilder.CreateIndex(
                name: "IX_Emulators_Name",
                table: "Emulators",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Emulators_PlatformId",
                table: "Emulators",
                column: "PlatformId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalApiKeys_AppName",
                table: "ExternalApiKeys",
                column: "AppName");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalApiKeys_IsActive",
                table: "ExternalApiKeys",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalApiKeys_Key",
                table: "ExternalApiKeys",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalApiKeys_LastUsedAt",
                table: "ExternalApiKeys",
                column: "LastUsedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FriendActivities_FriendId",
                table: "FriendActivities",
                column: "FriendId");

            migrationBuilder.CreateIndex(
                name: "IX_FriendActivities_FriendId_Timestamp",
                table: "FriendActivities",
                columns: new[] { "FriendId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_FriendActivities_Platform",
                table: "FriendActivities",
                column: "Platform");

            migrationBuilder.CreateIndex(
                name: "IX_FriendActivities_Timestamp",
                table: "FriendActivities",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_FriendActivities_Type",
                table: "FriendActivities",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Friends_IsOnline",
                table: "Friends",
                column: "IsOnline");

            migrationBuilder.CreateIndex(
                name: "IX_Friends_Platform",
                table: "Friends",
                column: "Platform");

            migrationBuilder.CreateIndex(
                name: "IX_Friends_Platform_PlatformUserId",
                table: "Friends",
                columns: new[] { "Platform", "PlatformUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Friends_UpdatedAt",
                table: "Friends",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_GameFiles_GameId",
                table: "GameFiles",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_GameFiles_Path",
                table: "GameFiles",
                column: "Path");

            migrationBuilder.CreateIndex(
                name: "IX_GameGenre_GenresId",
                table: "GameGenre",
                column: "GenresId");

            migrationBuilder.CreateIndex(
                name: "IX_GameMedia_GameId",
                table: "GameMedia",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_GameMedia_IsFavorite",
                table: "GameMedia",
                column: "IsFavorite");

            migrationBuilder.CreateIndex(
                name: "IX_GameMedia_IsPublic",
                table: "GameMedia",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_GameMedia_MediaType",
                table: "GameMedia",
                column: "MediaType");

            migrationBuilder.CreateIndex(
                name: "IX_GameMedia_UserId",
                table: "GameMedia",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GameMods_GameId",
                table: "GameMods",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_GameMods_IsEnabled",
                table: "GameMods",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_GameMods_LoadOrder",
                table: "GameMods",
                column: "LoadOrder");

            migrationBuilder.CreateIndex(
                name: "IX_GameNotes_GameId",
                table: "GameNotes",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_GameNotes_IsPinned",
                table: "GameNotes",
                column: "IsPinned");

            migrationBuilder.CreateIndex(
                name: "IX_GameNotes_UserId",
                table: "GameNotes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GameReviews_ContainsSpoilers",
                table: "GameReviews",
                column: "ContainsSpoilers");

            migrationBuilder.CreateIndex(
                name: "IX_GameReviews_CreatedAt",
                table: "GameReviews",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_GameReviews_GameId",
                table: "GameReviews",
                column: "GameId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameReviews_IsRecommended",
                table: "GameReviews",
                column: "IsRecommended");

            migrationBuilder.CreateIndex(
                name: "IX_GameReviews_Rating",
                table: "GameReviews",
                column: "Rating");

            migrationBuilder.CreateIndex(
                name: "IX_Games_CreatedAt",
                table: "Games",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Games_CreatedAt_Platform",
                table: "Games",
                columns: new[] { "CreatedAt", "PlatformId" });

            migrationBuilder.CreateIndex(
                name: "IX_Games_LastPlayedAt",
                table: "Games",
                column: "LastPlayedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Games_Platform_Status_Title",
                table: "Games",
                columns: new[] { "PlatformId", "Status", "Title" });

            migrationBuilder.CreateIndex(
                name: "IX_Games_PlatformId",
                table: "Games",
                column: "PlatformId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_PlatformId_Title",
                table: "Games",
                columns: new[] { "PlatformId", "Title" });

            migrationBuilder.CreateIndex(
                name: "IX_Games_Status",
                table: "Games",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Games_Status_LastPlayedAt",
                table: "Games",
                columns: new[] { "Status", "LastPlayedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Games_Summary_Covering",
                table: "Games",
                columns: new[] { "Id", "Title", "PlatformId", "Status", "LastPlayedAt", "TotalPlayTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Games_Title",
                table: "Games",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_Games_TotalPlayTime",
                table: "Games",
                column: "TotalPlayTime");

            migrationBuilder.CreateIndex(
                name: "IX_GameSessions_GameId",
                table: "GameSessions",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_GamingGoals_EndDate",
                table: "GamingGoals",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_GamingGoals_StartDate",
                table: "GamingGoals",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_GamingGoals_Status",
                table: "GamingGoals",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_GamingGoals_Status_Type",
                table: "GamingGoals",
                columns: new[] { "Status", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_GamingGoals_Type",
                table: "GamingGoals",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Genres_Name",
                table: "Genres",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeRecords_AccessCount",
                table: "KnowledgeRecords",
                column: "AccessCount");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeRecords_Id",
                table: "KnowledgeRecords",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeRecords_IndexedAt",
                table: "KnowledgeRecords",
                column: "IndexedAt");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeRecords_LastAccessedAt",
                table: "KnowledgeRecords",
                column: "LastAccessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeRecords_RelevanceScore",
                table: "KnowledgeRecords",
                column: "RelevanceScore");

            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardRanking_LeaderboardId",
                table: "LeaderboardRanking",
                column: "LeaderboardId");

            migrationBuilder.CreateIndex(
                name: "IX_MemorySnapshots_Address",
                table: "MemorySnapshots",
                column: "Address");

            migrationBuilder.CreateIndex(
                name: "IX_MemorySnapshots_CapturedAt",
                table: "MemorySnapshots",
                column: "CapturedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MemorySnapshots_ProcessId",
                table: "MemorySnapshots",
                column: "ProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_MemoryWatches_GameId",
                table: "MemoryWatches",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_MemoryWatches_GameId_IsActive",
                table: "MemoryWatches",
                columns: new[] { "GameId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_MugenCharacterCollections_CreatedAt",
                table: "MugenCharacterCollections",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MugenCharacterCollections_IsPublic",
                table: "MugenCharacterCollections",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_MugenCharacterCollections_LastModified",
                table: "MugenCharacterCollections",
                column: "LastModified");

            migrationBuilder.CreateIndex(
                name: "IX_MugenCharacterCollections_UserId",
                table: "MugenCharacterCollections",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MugenCharacters_Author",
                table: "MugenCharacters",
                column: "Author");

            migrationBuilder.CreateIndex(
                name: "IX_MugenCharacters_Name",
                table: "MugenCharacters",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_MugenCollectionCharacters_CharacterId",
                table: "MugenCollectionCharacters",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_MugenCollectionCharacters_CollectionId",
                table: "MugenCollectionCharacters",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_MugenCollectionCharacters_CollectionId_CharacterId",
                table: "MugenCollectionCharacters",
                columns: new[] { "CollectionId", "CharacterId" });

            migrationBuilder.CreateIndex(
                name: "IX_MugenCollectionCharacters_IsFavorite",
                table: "MugenCollectionCharacters",
                column: "IsFavorite");

            migrationBuilder.CreateIndex(
                name: "IX_MugenDummyRecordings_BehaviorType",
                table: "MugenDummyRecordings",
                column: "BehaviorType");

            migrationBuilder.CreateIndex(
                name: "IX_MugenDummyRecordings_CreatedAt",
                table: "MugenDummyRecordings",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MugenDummyRecordings_IsSuccessful",
                table: "MugenDummyRecordings",
                column: "IsSuccessful");

            migrationBuilder.CreateIndex(
                name: "IX_MugenDummyRecordings_TrainingSessionId",
                table: "MugenDummyRecordings",
                column: "TrainingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_MugenMatchHistories_PlayedAt",
                table: "MugenMatchHistories",
                column: "PlayedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MugenMatchHistories_Player1CharacterId",
                table: "MugenMatchHistories",
                column: "Player1CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_MugenMatchHistories_Player1CharacterId_Player2CharacterId",
                table: "MugenMatchHistories",
                columns: new[] { "Player1CharacterId", "Player2CharacterId" });

            migrationBuilder.CreateIndex(
                name: "IX_MugenMatchHistories_Player2CharacterId",
                table: "MugenMatchHistories",
                column: "Player2CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_MugenMatchupStats_Character1Id",
                table: "MugenMatchupStats",
                column: "Character1Id");

            migrationBuilder.CreateIndex(
                name: "IX_MugenMatchupStats_Character1Id_Character2Id",
                table: "MugenMatchupStats",
                columns: new[] { "Character1Id", "Character2Id" });

            migrationBuilder.CreateIndex(
                name: "IX_MugenMatchupStats_Character2Id",
                table: "MugenMatchupStats",
                column: "Character2Id");

            migrationBuilder.CreateIndex(
                name: "IX_MugenMatchupStats_LastUpdated",
                table: "MugenMatchupStats",
                column: "LastUpdated");

            migrationBuilder.CreateIndex(
                name: "IX_MugenTournaments_CreatedAt",
                table: "MugenTournaments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MugenTournaments_Status",
                table: "MugenTournaments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MugenTournaments_WinnerId",
                table: "MugenTournaments",
                column: "WinnerId",
                filter: "[WinnerId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MugenTrainingSessions_CharacterId",
                table: "MugenTrainingSessions",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_MugenTrainingSessions_OpponentCharacterId",
                table: "MugenTrainingSessions",
                column: "OpponentCharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_MugenTrainingSessions_SessionType",
                table: "MugenTrainingSessions",
                column: "SessionType");

            migrationBuilder.CreateIndex(
                name: "IX_MugenTrainingSessions_StartedAt",
                table: "MugenTrainingSessions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MugenTrainingSessions_UserId",
                table: "MugenTrainingSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MugenTrainingSessions_UserId_EndedAt",
                table: "MugenTrainingSessions",
                columns: new[] { "UserId", "EndedAt" },
                filter: "[EndedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NetworkQualityHistories_Level",
                table: "NetworkQualityHistories",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_NetworkQualityHistories_MeasuredAt",
                table: "NetworkQualityHistories",
                column: "MeasuredAt");

            migrationBuilder.CreateIndex(
                name: "IX_NetworkQualityHistories_SessionId",
                table: "NetworkQualityHistories",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_NetworkQualityHistories_SessionId_MeasuredAt",
                table: "NetworkQualityHistories",
                columns: new[] { "SessionId", "MeasuredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OpenMKCharacters_Name",
                table: "OpenMKCharacters",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpenMKCharacterUnlocks_UserId",
                table: "OpenMKCharacterUnlocks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OpenMKCharacterUnlocks_UserId_CharacterId",
                table: "OpenMKCharacterUnlocks",
                columns: new[] { "UserId", "CharacterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpenMKMatchStates_Player1CharacterId",
                table: "OpenMKMatchStates",
                column: "Player1CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_OpenMKMatchStates_Player2CharacterId",
                table: "OpenMKMatchStates",
                column: "Player2CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_OpenMKUserProgress_LastUpdatedAt",
                table: "OpenMKUserProgress",
                column: "LastUpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Name",
                table: "Permissions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Resource_Action",
                table: "Permissions",
                columns: new[] { "Resource", "Action" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Platforms_Name",
                table: "Platforms",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Platforms_ShortName",
                table: "Platforms",
                column: "ShortName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Platforms_Type",
                table: "Platforms",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Publishers_Name",
                table: "Publishers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId",
                table: "RolePermissions",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_IsSystemRole",
                table: "Roles",
                column: "IsSystemRole");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RomFiles_FilePath",
                table: "RomFiles",
                column: "FilePath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RomFiles_PlatformId",
                table: "RomFiles",
                column: "PlatformId");

            migrationBuilder.CreateIndex(
                name: "IX_RomFiles_PlatformId_FilePath",
                table: "RomFiles",
                columns: new[] { "PlatformId", "FilePath" });

            migrationBuilder.CreateIndex(
                name: "IX_RomFiles_ScannedAt",
                table: "RomFiles",
                column: "ScannedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RomFiles_Status",
                table: "RomFiles",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SaveStateBranches_BranchName_RootStateId",
                table: "SaveStateBranches",
                columns: new[] { "BranchName", "RootStateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SaveStateBranches_CreatedAt",
                table: "SaveStateBranches",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SaveStateBranches_RootStateId",
                table: "SaveStateBranches",
                column: "RootStateId");

            migrationBuilder.CreateIndex(
                name: "IX_SaveStateBranches_Type",
                table: "SaveStateBranches",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_SaveStates_CreatedAt",
                table: "SaveStates",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SaveStates_GameId",
                table: "SaveStates",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_SaveStates_GameId_CreatedAt",
                table: "SaveStates",
                columns: new[] { "GameId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SaveStates_GameId_IsAutoSave",
                table: "SaveStates",
                columns: new[] { "GameId", "IsAutoSave" });

            migrationBuilder.CreateIndex(
                name: "IX_SaveStates_IsFavorite",
                table: "SaveStates",
                column: "IsFavorite");

            migrationBuilder.CreateIndex(
                name: "IX_SharedCollectionItems_CollectionId_SortOrder",
                table: "SharedCollectionItems",
                columns: new[] { "CollectionId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SharedCollectionItems_GameTitle",
                table: "SharedCollectionItems",
                column: "GameTitle");

            migrationBuilder.CreateIndex(
                name: "IX_SharedCollections_CreatedAt",
                table: "SharedCollections",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SharedCollections_IsPublic",
                table: "SharedCollections",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_SharedCollections_ShareCode",
                table: "SharedCollections",
                column: "ShareCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SharedCollections_Title",
                table: "SharedCollections",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_Player1CharacterId",
                table: "TournamentMatches",
                column: "Player1CharacterId",
                filter: "[Player1CharacterId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_Player2CharacterId",
                table: "TournamentMatches",
                column: "Player2CharacterId",
                filter: "[Player2CharacterId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_TournamentId",
                table: "TournamentMatches",
                column: "TournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_TournamentId_Status",
                table: "TournamentMatches",
                columns: new[] { "TournamentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_TournamentParticipantId",
                table: "TournamentMatches",
                column: "TournamentParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_WinnerId",
                table: "TournamentMatches",
                column: "WinnerId",
                filter: "[WinnerId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentParticipants_CharacterId",
                table: "TournamentParticipants",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentParticipants_TournamentId",
                table: "TournamentParticipants",
                column: "TournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentParticipants_TournamentId_Status",
                table: "TournamentParticipants",
                columns: new[] { "TournamentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_User_Email",
                table: "User",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_IsActive",
                table: "User",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_User_LastLoginAt",
                table: "User",
                column: "LastLoginAt");

            migrationBuilder.CreateIndex(
                name: "IX_User_Username",
                table: "User",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAchievements_AchievementId",
                table: "UserAchievements",
                column: "AchievementId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAchievements_UserId",
                table: "UserAchievements",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAchievements_UserId_AchievementId",
                table: "UserAchievements",
                columns: new[] { "UserId", "AchievementId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId",
                table: "UserRoles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                table: "Users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Users_LastLoginAt",
                table: "Users",
                column: "LastLoginAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VirtualCollectionGames_CollectionId_SortOrder",
                table: "VirtualCollectionGames",
                columns: new[] { "CollectionId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_VirtualCollectionGames_GameId",
                table: "VirtualCollectionGames",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_VirtualCollections_IsSystemCollection",
                table: "VirtualCollections",
                column: "IsSystemCollection");

            migrationBuilder.CreateIndex(
                name: "IX_VirtualCollections_IsSystemCollection_SortOrder_Name",
                table: "VirtualCollections",
                columns: new[] { "IsSystemCollection", "SortOrder", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_VirtualCollections_Type",
                table: "VirtualCollections",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiModels");

            migrationBuilder.DropTable(
                name: "ApiKeys");

            migrationBuilder.DropTable(
                name: "BacklogEntries");

            migrationBuilder.DropTable(
                name: "Backups");

            migrationBuilder.DropTable(
                name: "ChallengeParticipant");

            migrationBuilder.DropTable(
                name: "ChallengeRequirement");

            migrationBuilder.DropTable(
                name: "ControllerProfiles");

            migrationBuilder.DropTable(
                name: "Developers");

            migrationBuilder.DropTable(
                name: "Emulators");

            migrationBuilder.DropTable(
                name: "ExternalApiKeys");

            migrationBuilder.DropTable(
                name: "FriendActivities");

            migrationBuilder.DropTable(
                name: "GameFiles");

            migrationBuilder.DropTable(
                name: "GameGenre");

            migrationBuilder.DropTable(
                name: "GameMedia");

            migrationBuilder.DropTable(
                name: "GameMods");

            migrationBuilder.DropTable(
                name: "GameNotes");

            migrationBuilder.DropTable(
                name: "GameReviews");

            migrationBuilder.DropTable(
                name: "GameSessions");

            migrationBuilder.DropTable(
                name: "GamingGoals");

            migrationBuilder.DropTable(
                name: "KnowledgeRecords");

            migrationBuilder.DropTable(
                name: "LeaderboardRanking");

            migrationBuilder.DropTable(
                name: "MemorySnapshots");

            migrationBuilder.DropTable(
                name: "MemoryWatches");

            migrationBuilder.DropTable(
                name: "MugenCollectionCharacters");

            migrationBuilder.DropTable(
                name: "MugenDummyRecordings");

            migrationBuilder.DropTable(
                name: "MugenMatchHistories");

            migrationBuilder.DropTable(
                name: "MugenMatchupStats");

            migrationBuilder.DropTable(
                name: "NetworkQualityHistories");

            migrationBuilder.DropTable(
                name: "OpenMKCharacters");

            migrationBuilder.DropTable(
                name: "OpenMKCharacterUnlocks");

            migrationBuilder.DropTable(
                name: "OpenMKMatchStates");

            migrationBuilder.DropTable(
                name: "OpenMKUserProgress");

            migrationBuilder.DropTable(
                name: "Publishers");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "RomFiles");

            migrationBuilder.DropTable(
                name: "SaveStateBranches");

            migrationBuilder.DropTable(
                name: "SharedCollectionItems");

            migrationBuilder.DropTable(
                name: "TournamentMatches");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "UserAchievements");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "VirtualCollectionGames");

            migrationBuilder.DropTable(
                name: "Challenges");

            migrationBuilder.DropTable(
                name: "Friends");

            migrationBuilder.DropTable(
                name: "Genres");

            migrationBuilder.DropTable(
                name: "Leaderboards");

            migrationBuilder.DropTable(
                name: "MugenCharacterCollections");

            migrationBuilder.DropTable(
                name: "MugenTrainingSessions");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "SaveStates");

            migrationBuilder.DropTable(
                name: "SharedCollections");

            migrationBuilder.DropTable(
                name: "TournamentParticipants");

            migrationBuilder.DropTable(
                name: "Achievements");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "VirtualCollections");

            migrationBuilder.DropTable(
                name: "Games");

            migrationBuilder.DropTable(
                name: "MugenCharacters");

            migrationBuilder.DropTable(
                name: "MugenTournaments");

            migrationBuilder.DropTable(
                name: "Platforms");
        }
    }
}

#pragma warning restore CA1505
