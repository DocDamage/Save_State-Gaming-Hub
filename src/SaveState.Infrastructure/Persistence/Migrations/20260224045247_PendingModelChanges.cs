using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveState.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [SuppressMessage("Maintainability", "CA1505:Avoid unmaintainable code", Justification = "Auto-generated EF Core migration")]
    public partial class PendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_MugenTournaments_Status",
                table: "MugenTournaments",
                newName: "IX_MugenTournaments_Status1");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "EstimatedTimeToComplete",
                table: "Games",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExecutablePath",
                table: "Games",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AiBattleAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CharacterName = table.Column<string>(type: "TEXT", nullable: false),
                    OpponentName = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    BattleDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Result = table.Column<int>(type: "INTEGER", nullable: false),
                    RoundsWon = table.Column<int>(type: "INTEGER", nullable: false),
                    RoundsLost = table.Column<int>(type: "INTEGER", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    Stats = table.Column<string>(type: "TEXT", nullable: false),
                    Patterns = table.Column<string>(type: "TEXT", nullable: false),
                    Weaknesses = table.Column<string>(type: "TEXT", nullable: false),
                    Opportunities = table.Column<string>(type: "TEXT", nullable: false),
                    Recommendations = table.Column<string>(type: "TEXT", nullable: false),
                    Insights = table.Column<string>(type: "TEXT", nullable: false),
                    PerformanceRating = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiBattleAnalyses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AutoSaveConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GameId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    IntervalMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxAutoSaves = table.Column<int>(type: "INTEGER", nullable: false),
                    SaveOnLevelComplete = table.Column<bool>(type: "INTEGER", nullable: false),
                    SaveBeforeBoss = table.Column<bool>(type: "INTEGER", nullable: false),
                    SaveOnCheckpoint = table.Column<bool>(type: "INTEGER", nullable: false),
                    MinPlayTimeMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    NamingPattern = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IncludeDate = table.Column<bool>(type: "INTEGER", nullable: false),
                    IncludeTime = table.Column<bool>(type: "INTEGER", nullable: false),
                    CompressSaves = table.Column<bool>(type: "INTEGER", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoSaveConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AutoSaveEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GameId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    TriggerType = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PlayTimeSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    Level = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Checkpoint = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ThumbnailPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    IsLocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    Metadata = table.Column<string>(type: "TEXT", nullable: false),
                    IsPruned = table.Column<bool>(type: "INTEGER", nullable: false),
                    SequenceNumber = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoSaveEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CharacterFrameData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CharacterName = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Health = table.Column<int>(type: "INTEGER", nullable: false),
                    WalkSpeed = table.Column<int>(type: "INTEGER", nullable: false),
                    BackWalkSpeed = table.Column<int>(type: "INTEGER", nullable: false),
                    DashDistance = table.Column<int>(type: "INTEGER", nullable: false),
                    JumpHeight = table.Column<int>(type: "INTEGER", nullable: false),
                    PreJumpFrames = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterFrameData", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CloudSaveStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GameId = table.Column<int>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Provider = table.Column<string>(type: "TEXT", nullable: false),
                    CloudId = table.Column<string>(type: "TEXT", nullable: false),
                    DownloadUrl = table.Column<string>(type: "TEXT", nullable: true),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    FileHash = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    LocalCreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LocalModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CloudSyncedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsFavorite = table.Column<bool>(type: "INTEGER", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    PreviewImageUrl = table.Column<string>(type: "TEXT", nullable: true),
                    GamePlaytime = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    GameLocation = table.Column<string>(type: "TEXT", nullable: true),
                    Platform = table.Column<string>(type: "TEXT", nullable: true),
                    Emulator = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CloudSaveStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComboCollections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CharacterName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ComboIds = table.Column<string>(type: "TEXT", nullable: false),
                    Creator = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsPublic = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ViewCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LikeCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComboCollections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComboEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CharacterName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CharacterVersion = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Difficulty = table.Column<int>(type: "INTEGER", nullable: false),
                    HitCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Damage = table.Column<int>(type: "INTEGER", nullable: false),
                    ScalingPercentage = table.Column<decimal>(type: "TEXT", nullable: false),
                    StartingPosition = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    MeterRequired = table.Column<int>(type: "INTEGER", nullable: false),
                    MeterGained = table.Column<int>(type: "INTEGER", nullable: false),
                    Moves = table.Column<string>(type: "TEXT", nullable: false),
                    InputNotation = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    VideoUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ImagePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Creator = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Source = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsPendingApproval = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsVerified = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsOptimal = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsTouchOfDeath = table.Column<bool>(type: "INTEGER", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    FrameData = table.Column<string>(type: "TEXT", nullable: false),
                    Timing = table.Column<string>(type: "TEXT", nullable: false),
                    UsageStats = table.Column<string>(type: "TEXT", nullable: false),
                    Ratings = table.Column<string>(type: "TEXT", nullable: false),
                    RelatedComboIds = table.Column<string>(type: "TEXT", nullable: false),
                    Prerequisites = table.Column<string>(type: "TEXT", nullable: false),
                    Tips = table.Column<string>(type: "TEXT", nullable: false),
                    EndingPosition = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    OkizemeOptions = table.Column<string>(type: "TEXT", nullable: false),
                    Universal = table.Column<bool>(type: "INTEGER", nullable: false),
                    CharacterExceptions = table.Column<string>(type: "TEXT", nullable: false),
                    GameVersion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComboEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComboPracticeSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ComboId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Attempts = table.Column<int>(type: "INTEGER", nullable: false),
                    Successes = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalPracticeTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    AttemptsLog = table.Column<string>(type: "TEXT", nullable: false),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    ConsistencyRating = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComboPracticeSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComboSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ComboId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SubmitterName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SubmitterId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    ReviewerNotes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReviewedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Upvotes = table.Column<int>(type: "INTEGER", nullable: false),
                    Downvotes = table.Column<int>(type: "INTEGER", nullable: false),
                    VerificationVideos = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComboSubmissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeathBattleMatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BattleCode = table.Column<string>(type: "TEXT", nullable: false),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    Outcome = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentPhaseIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsPublic = table.Column<bool>(type: "INTEGER", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    SuggestedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Duration = table.Column<TimeSpan>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeathBattleMatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeathBattleSuggestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SuggestedCombatant1Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SuggestedCombatant1Name = table.Column<string>(type: "TEXT", nullable: false),
                    SuggestedCombatant2Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SuggestedCombatant2Name = table.Column<string>(type: "TEXT", nullable: false),
                    Reasoning = table.Column<string>(type: "TEXT", nullable: false),
                    SuggestedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Upvotes = table.Column<int>(type: "INTEGER", nullable: false),
                    IsAccepted = table.Column<bool>(type: "INTEGER", nullable: false),
                    SuggestedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeathBattleSuggestions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FusedCharacters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    Parent1Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Parent1Name = table.Column<string>(type: "TEXT", nullable: false),
                    Parent2Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Parent2Name = table.Column<string>(type: "TEXT", nullable: false),
                    FusionType = table.Column<int>(type: "INTEGER", nullable: false),
                    MugenDefContent = table.Column<string>(type: "TEXT", nullable: true),
                    CharacterFolderPath = table.Column<string>(type: "TEXT", nullable: true),
                    IsGenerated = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BattleCount = table.Column<int>(type: "INTEGER", nullable: false),
                    WinRate = table.Column<decimal>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsPreset = table.Column<bool>(type: "INTEGER", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    CompatibilityScore = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FusedCharacters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FusionBattleHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FusedCharacterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FusedCharacterName = table.Column<string>(type: "TEXT", nullable: false),
                    OpponentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OpponentName = table.Column<string>(type: "TEXT", nullable: false),
                    Won = table.Column<bool>(type: "INTEGER", nullable: false),
                    RoundsWon = table.Column<int>(type: "INTEGER", nullable: false),
                    RoundsLost = table.Column<int>(type: "INTEGER", nullable: false),
                    BattleDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DamageDealt = table.Column<int>(type: "INTEGER", nullable: false),
                    DamageReceived = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxCombo = table.Column<int>(type: "INTEGER", nullable: false),
                    SpecialMovesUsed = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FusionBattleHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GameStore",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Color = table.Column<string>(type: "TEXT", nullable: false),
                    IsOfficial = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameStore", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InputRecordings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GameId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DeviceType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TotalFrames = table.Column<long>(type: "INTEGER", nullable: false),
                    Duration = table.Column<long>(type: "INTEGER", nullable: false),
                    Fps = table.Column<int>(type: "INTEGER", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    StartingState = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RomHash = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    EmulatorCore = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Region = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    PlayCount = table.Column<int>(type: "INTEGER", nullable: false),
                    PersonalBestTime = table.Column<long>(type: "INTEGER", nullable: true),
                    IsVerifiedTAS = table.Column<bool>(type: "INTEGER", nullable: false),
                    Authors = table.Column<string>(type: "TEXT", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastPlayedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsBookmarked = table.Column<bool>(type: "INTEGER", nullable: false),
                    Bookmarks = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InputRecordings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LaunchProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    GameId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    DisableGameMode = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisableFullscreenOptimizations = table.Column<bool>(type: "INTEGER", nullable: false),
                    RunAsAdministrator = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisableWindowsDefender = table.Column<bool>(type: "INTEGER", nullable: false),
                    ProcessesToSuspend = table.Column<string>(type: "TEXT", nullable: false),
                    ServicesToStop = table.Column<string>(type: "TEXT", nullable: false),
                    DisplaySettings = table.Column<string>(type: "TEXT", nullable: true),
                    PerformanceSettings = table.Column<string>(type: "TEXT", nullable: false),
                    PowerPlanGuid = table.Column<string>(type: "TEXT", nullable: true),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EstimatedPerformanceGain = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaunchProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LaunchSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GameId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GameName = table.Column<string>(type: "TEXT", nullable: false),
                    ProfileId = table.Column<Guid>(type: "TEXT", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    InitialSystemState = table.Column<string>(type: "TEXT", nullable: true),
                    ExitCode = table.Column<int>(type: "INTEGER", nullable: true),
                    PerformanceMetrics = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaunchSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MobileDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceName = table.Column<string>(type: "TEXT", nullable: false),
                    DeviceType = table.Column<string>(type: "TEXT", nullable: false),
                    DeviceModel = table.Column<string>(type: "TEXT", nullable: true),
                    OsVersion = table.Column<string>(type: "TEXT", nullable: true),
                    AppVersion = table.Column<string>(type: "TEXT", nullable: true),
                    PairedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastConnectedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PushNotificationToken = table.Column<string>(type: "TEXT", nullable: true),
                    IsConnected = table.Column<bool>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Permissions = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MobileDevices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PriceAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GameTitle = table.Column<string>(type: "TEXT", nullable: false),
                    TargetPrice = table.Column<decimal>(type: "TEXT", nullable: true),
                    TargetDiscountPercent = table.Column<decimal>(type: "TEXT", nullable: true),
                    StoreIds = table.Column<string>(type: "TEXT", nullable: false),
                    AlertOnHistoricalLow = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastTriggeredAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MinHoursBetweenAlerts = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PriceHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GameTitle = table.Column<string>(type: "TEXT", nullable: false),
                    StoreId = table.Column<string>(type: "TEXT", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    WasOnSale = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReplayAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReplayFilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Platform = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ReplayDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AnalyzedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    Player1Character = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Player2Character = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Player1Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Player2Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Winner = table.Column<int>(type: "INTEGER", nullable: false),
                    RoundsPlayed = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalFrames = table.Column<int>(type: "INTEGER", nullable: false),
                    FrameRate = table.Column<int>(type: "INTEGER", nullable: false),
                    Player1Stats = table.Column<string>(type: "TEXT", nullable: false),
                    Player2Stats = table.Column<string>(type: "TEXT", nullable: false),
                    Combos = table.Column<string>(type: "TEXT", nullable: false),
                    Highlights = table.Column<string>(type: "TEXT", nullable: false),
                    Comebacks = table.Column<string>(type: "TEXT", nullable: false),
                    FrameData = table.Column<string>(type: "TEXT", nullable: true),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    AnalysisVersion = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    FileHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayAnalyses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RomHashInfos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RomFileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Crc32 = table.Column<string>(type: "TEXT", maxLength: 8, nullable: true),
                    Md5 = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    Sha1 = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Sha256 = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    CalculatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CalculationTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    IsComplete = table.Column<bool>(type: "INTEGER", nullable: false),
                    Errors = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RomHashInfos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionCatalogCache",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServiceId = table.Column<string>(type: "TEXT", nullable: false),
                    GameTitle = table.Column<string>(type: "TEXT", nullable: false),
                    Developer = table.Column<string>(type: "TEXT", nullable: true),
                    Publisher = table.Column<string>(type: "TEXT", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DateLeaving = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Genres = table.Column<string>(type: "TEXT", nullable: true),
                    MetacriticScore = table.Column<int>(type: "INTEGER", nullable: true),
                    CachedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionCatalogCache", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TournamentParticipant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TournamentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: true),
                    Seed = table.Column<int>(type: "INTEGER", nullable: false),
                    InitialSeed = table.Column<int>(type: "INTEGER", nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsCheckedIn = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckedInAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsEliminated = table.Column<bool>(type: "INTEGER", nullable: false),
                    EliminatedInRound = table.Column<int>(type: "INTEGER", nullable: true),
                    Placement = table.Column<int>(type: "INTEGER", nullable: true),
                    ContactInfo = table.Column<string>(type: "TEXT", nullable: true),
                    StreamUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Country = table.Column<string>(type: "TEXT", nullable: true),
                    Team = table.Column<string>(type: "TEXT", nullable: true),
                    Character = table.Column<string>(type: "TEXT", nullable: true),
                    CharacterLocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentParticipant", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrackedSubscriptionGames",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GameTitle = table.Column<string>(type: "TEXT", nullable: false),
                    PreferredServiceId = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    TrackedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    NotifyOnAvailable = table.Column<bool>(type: "INTEGER", nullable: false),
                    NotifyOnLeaving = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackedSubscriptionGames", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServiceId = table.Column<string>(type: "TEXT", nullable: false),
                    ServiceName = table.Column<string>(type: "TEXT", nullable: false),
                    SubscriptionType = table.Column<string>(type: "TEXT", nullable: false),
                    Tier = table.Column<string>(type: "TEXT", nullable: true),
                    MonthlyPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    AutoRenew = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MoveFrameData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CharacterFrameDataId = table.Column<int>(type: "INTEGER", nullable: false),
                    MoveName = table.Column<string>(type: "TEXT", nullable: false),
                    Command = table.Column<string>(type: "TEXT", nullable: false),
                    MoveType = table.Column<int>(type: "INTEGER", nullable: false),
                    StartupFrames = table.Column<int>(type: "INTEGER", nullable: false),
                    ActiveFrames = table.Column<int>(type: "INTEGER", nullable: false),
                    RecoveryFrames = table.Column<int>(type: "INTEGER", nullable: false),
                    HitAdvantage = table.Column<int>(type: "INTEGER", nullable: false),
                    BlockAdvantage = table.Column<int>(type: "INTEGER", nullable: false),
                    Damage = table.Column<int>(type: "INTEGER", nullable: false),
                    ChipDamage = table.Column<int>(type: "INTEGER", nullable: false),
                    MeterGain = table.Column<int>(type: "INTEGER", nullable: false),
                    HitLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    IsAirborne = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsInvincible = table.Column<bool>(type: "INTEGER", nullable: false),
                    InvincibilityFrames = table.Column<int>(type: "INTEGER", nullable: true),
                    Armor = table.Column<bool>(type: "INTEGER", nullable: false),
                    ArmorHits = table.Column<int>(type: "INTEGER", nullable: true),
                    IsProjectile = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsThrow = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsOverhead = table.Column<bool>(type: "INTEGER", nullable: false),
                    CausesKnockdown = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsCancelable = table.Column<bool>(type: "INTEGER", nullable: false),
                    CancelWindow = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MoveFrameData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MoveFrameData_CharacterFrameData_CharacterFrameDataId",
                        column: x => x.CharacterFrameDataId,
                        principalTable: "CharacterFrameData",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameDeals",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    TitlePlain = table.Column<string>(type: "TEXT", nullable: true),
                    ImageUrl = table.Column<string>(type: "TEXT", nullable: true),
                    CurrentPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    RegularPrice = table.Column<decimal>(type: "TEXT", nullable: true),
                    StoreId = table.Column<string>(type: "TEXT", nullable: true),
                    DealStart = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DealEnd = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsHistoricalLow = table.Column<bool>(type: "INTEGER", nullable: false),
                    StoreUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Drm = table.Column<string>(type: "TEXT", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MetacriticScore = table.Column<int>(type: "INTEGER", nullable: true),
                    SteamRating = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameDeals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameDeals_GameStore_StoreId",
                        column: x => x.StoreId,
                        principalTable: "GameStore",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RomValidationReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RomFileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    HashInfoId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ValidatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ValidationDuration = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    Recommendations = table.Column<string>(type: "TEXT", nullable: false),
                    SuggestedName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RomValidationReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RomValidationReports_RomHashInfos_HashInfoId",
                        column: x => x.HashInfoId,
                        principalTable: "RomHashInfos",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TournamentEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Format = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    MaxParticipants = table.Column<int>(type: "INTEGER", nullable: false),
                    Participants = table.Column<string>(type: "TEXT", nullable: false),
                    Matches = table.Column<string>(type: "TEXT", nullable: false),
                    Rounds = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ScheduledStart = table.Column<DateTime>(type: "TEXT", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Organizer = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Rules = table.Column<string>(type: "TEXT", nullable: false),
                    Settings = table.Column<string>(type: "TEXT", nullable: false),
                    WinnerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RunnerUpId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ThirdPlaceId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsPublic = table.Column<bool>(type: "INTEGER", nullable: false),
                    RegistrationOpen = table.Column<bool>(type: "INTEGER", nullable: false),
                    RegistrationDeadline = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    StreamUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DiscordWebhook = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CurrentRound = table.Column<int>(type: "INTEGER", nullable: false),
                    PrizePool = table.Column<string>(type: "TEXT", nullable: true),
                    Statistics = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentEvents_TournamentParticipant_RunnerUpId",
                        column: x => x.RunnerUpId,
                        principalTable: "TournamentParticipant",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TournamentEvents_TournamentParticipant_ThirdPlaceId",
                        column: x => x.ThirdPlaceId,
                        principalTable: "TournamentParticipant",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TournamentEvents_TournamentParticipant_WinnerId",
                        column: x => x.WinnerId,
                        principalTable: "TournamentParticipant",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AutoSaveConfigurations_GameId",
                table: "AutoSaveConfigurations",
                column: "GameId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AutoSaveEntries_CreatedAt",
                table: "AutoSaveEntries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AutoSaveEntries_GameId",
                table: "AutoSaveEntries",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_AutoSaveEntries_GameId_TriggerType",
                table: "AutoSaveEntries",
                columns: new[] { "GameId", "TriggerType" });

            migrationBuilder.CreateIndex(
                name: "IX_ComboCollections_CharacterName",
                table: "ComboCollections",
                column: "CharacterName");

            migrationBuilder.CreateIndex(
                name: "IX_ComboCollections_Creator",
                table: "ComboCollections",
                column: "Creator");

            migrationBuilder.CreateIndex(
                name: "IX_ComboCollections_IsPublic",
                table: "ComboCollections",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_ComboEntries_CharacterName",
                table: "ComboEntries",
                column: "CharacterName");

            migrationBuilder.CreateIndex(
                name: "IX_ComboEntries_CharacterName_Difficulty",
                table: "ComboEntries",
                columns: new[] { "CharacterName", "Difficulty" });

            migrationBuilder.CreateIndex(
                name: "IX_ComboEntries_CharacterName_IsOptimal",
                table: "ComboEntries",
                columns: new[] { "CharacterName", "IsOptimal" });

            migrationBuilder.CreateIndex(
                name: "IX_ComboEntries_CreatedAt",
                table: "ComboEntries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ComboEntries_IsTouchOfDeath",
                table: "ComboEntries",
                column: "IsTouchOfDeath");

            migrationBuilder.CreateIndex(
                name: "IX_ComboPracticeSessions_ComboId",
                table: "ComboPracticeSessions",
                column: "ComboId");

            migrationBuilder.CreateIndex(
                name: "IX_ComboPracticeSessions_StartedAt",
                table: "ComboPracticeSessions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ComboSubmissions_ComboId",
                table: "ComboSubmissions",
                column: "ComboId");

            migrationBuilder.CreateIndex(
                name: "IX_ComboSubmissions_Status",
                table: "ComboSubmissions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ComboSubmissions_SubmittedAt",
                table: "ComboSubmissions",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_GameDeals_CurrentPrice",
                table: "GameDeals",
                column: "CurrentPrice");

            migrationBuilder.CreateIndex(
                name: "IX_GameDeals_DealEnd_LastUpdated",
                table: "GameDeals",
                columns: new[] { "DealEnd", "LastUpdated" });

            migrationBuilder.CreateIndex(
                name: "IX_GameDeals_StoreId",
                table: "GameDeals",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_GameDeals_Title",
                table: "GameDeals",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_InputRecordings_GameId",
                table: "InputRecordings",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_InputRecordings_IsBookmarked",
                table: "InputRecordings",
                column: "IsBookmarked");

            migrationBuilder.CreateIndex(
                name: "IX_InputRecordings_IsVerifiedTAS",
                table: "InputRecordings",
                column: "IsVerifiedTAS");

            migrationBuilder.CreateIndex(
                name: "IX_InputRecordings_RecordedAt",
                table: "InputRecordings",
                column: "RecordedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InputRecordings_Status",
                table: "InputRecordings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_InputRecordings_Type",
                table: "InputRecordings",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_LaunchProfiles_GameId_IsDefault",
                table: "LaunchProfiles",
                columns: new[] { "GameId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_LaunchProfiles_IsActive",
                table: "LaunchProfiles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_LaunchSessions_GameId_EndedAt",
                table: "LaunchSessions",
                columns: new[] { "GameId", "EndedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LaunchSessions_StartedAt",
                table: "LaunchSessions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MobileDevices_DeviceName",
                table: "MobileDevices",
                column: "DeviceName");

            migrationBuilder.CreateIndex(
                name: "IX_MobileDevices_DeviceType",
                table: "MobileDevices",
                column: "DeviceType");

            migrationBuilder.CreateIndex(
                name: "IX_MobileDevices_IsConnected",
                table: "MobileDevices",
                column: "IsConnected");

            migrationBuilder.CreateIndex(
                name: "IX_MobileDevices_PairedAt",
                table: "MobileDevices",
                column: "PairedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MoveFrameData_CharacterFrameDataId",
                table: "MoveFrameData",
                column: "CharacterFrameDataId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceAlerts_IsActive_GameTitle",
                table: "PriceAlerts",
                columns: new[] { "IsActive", "GameTitle" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceAlerts_UserId",
                table: "PriceAlerts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistory_Date",
                table: "PriceHistory",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistory_GameTitle_StoreId",
                table: "PriceHistory",
                columns: new[] { "GameTitle", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplayAnalyses_AnalyzedAt",
                table: "ReplayAnalyses",
                column: "AnalyzedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayAnalyses_CharacterMatchup",
                table: "ReplayAnalyses",
                columns: new[] { "Player1Character", "Player2Character" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplayAnalyses_FileHash",
                table: "ReplayAnalyses",
                column: "FileHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReplayAnalyses_Player1Character",
                table: "ReplayAnalyses",
                column: "Player1Character");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayAnalyses_Player2Character",
                table: "ReplayAnalyses",
                column: "Player2Character");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayAnalyses_ReplayDate",
                table: "ReplayAnalyses",
                column: "ReplayDate");

            migrationBuilder.CreateIndex(
                name: "IX_RomHashInfos_Crc32",
                table: "RomHashInfos",
                column: "Crc32");

            migrationBuilder.CreateIndex(
                name: "IX_RomHashInfos_Md5",
                table: "RomHashInfos",
                column: "Md5");

            migrationBuilder.CreateIndex(
                name: "IX_RomHashInfos_RomFileId",
                table: "RomHashInfos",
                column: "RomFileId");

            migrationBuilder.CreateIndex(
                name: "IX_RomHashInfos_RomFileId_IsComplete",
                table: "RomHashInfos",
                columns: new[] { "RomFileId", "IsComplete" });

            migrationBuilder.CreateIndex(
                name: "IX_RomHashInfos_Sha1",
                table: "RomHashInfos",
                column: "Sha1");

            migrationBuilder.CreateIndex(
                name: "IX_RomValidationReports_HashInfoId",
                table: "RomValidationReports",
                column: "HashInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_RomValidationReports_RomFileId",
                table: "RomValidationReports",
                column: "RomFileId");

            migrationBuilder.CreateIndex(
                name: "IX_RomValidationReports_RomFileId_Status",
                table: "RomValidationReports",
                columns: new[] { "RomFileId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RomValidationReports_Status",
                table: "RomValidationReports",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RomValidationReports_ValidatedAt",
                table: "RomValidationReports",
                column: "ValidatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionCatalogCache_DateLeaving",
                table: "SubscriptionCatalogCache",
                column: "DateLeaving");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionCatalogCache_ServiceId_GameTitle",
                table: "SubscriptionCatalogCache",
                columns: new[] { "ServiceId", "GameTitle" });

            migrationBuilder.CreateIndex(
                name: "IX_MugenTournaments_Format",
                table: "TournamentEvents",
                column: "Format");

            migrationBuilder.CreateIndex(
                name: "IX_MugenTournaments_IsPublic_Status",
                table: "TournamentEvents",
                columns: new[] { "IsPublic", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MugenTournaments_Organizer",
                table: "TournamentEvents",
                column: "Organizer");

            migrationBuilder.CreateIndex(
                name: "IX_MugenTournaments_ScheduledStart",
                table: "TournamentEvents",
                column: "ScheduledStart");

            migrationBuilder.CreateIndex(
                name: "IX_MugenTournaments_Status",
                table: "TournamentEvents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentEvents_RunnerUpId",
                table: "TournamentEvents",
                column: "RunnerUpId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentEvents_ThirdPlaceId",
                table: "TournamentEvents",
                column: "ThirdPlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentEvents_WinnerId",
                table: "TournamentEvents",
                column: "WinnerId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackedSubscriptionGames_UserId_GameTitle",
                table: "TrackedSubscriptionGames",
                columns: new[] { "UserId", "GameTitle" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_IsActive",
                table: "UserSubscriptions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_UserId",
                table: "UserSubscriptions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_UserId_ServiceId",
                table: "UserSubscriptions",
                columns: new[] { "UserId", "ServiceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiBattleAnalyses");

            migrationBuilder.DropTable(
                name: "AutoSaveConfigurations");

            migrationBuilder.DropTable(
                name: "AutoSaveEntries");

            migrationBuilder.DropTable(
                name: "CloudSaveStates");

            migrationBuilder.DropTable(
                name: "ComboCollections");

            migrationBuilder.DropTable(
                name: "ComboEntries");

            migrationBuilder.DropTable(
                name: "ComboPracticeSessions");

            migrationBuilder.DropTable(
                name: "ComboSubmissions");

            migrationBuilder.DropTable(
                name: "DeathBattleMatches");

            migrationBuilder.DropTable(
                name: "DeathBattleSuggestions");

            migrationBuilder.DropTable(
                name: "FusedCharacters");

            migrationBuilder.DropTable(
                name: "FusionBattleHistories");

            migrationBuilder.DropTable(
                name: "GameDeals");

            migrationBuilder.DropTable(
                name: "InputRecordings");

            migrationBuilder.DropTable(
                name: "LaunchProfiles");

            migrationBuilder.DropTable(
                name: "LaunchSessions");

            migrationBuilder.DropTable(
                name: "MobileDevices");

            migrationBuilder.DropTable(
                name: "MoveFrameData");

            migrationBuilder.DropTable(
                name: "PriceAlerts");

            migrationBuilder.DropTable(
                name: "PriceHistory");

            migrationBuilder.DropTable(
                name: "ReplayAnalyses");

            migrationBuilder.DropTable(
                name: "RomValidationReports");

            migrationBuilder.DropTable(
                name: "SubscriptionCatalogCache");

            migrationBuilder.DropTable(
                name: "TournamentEvents");

            migrationBuilder.DropTable(
                name: "TrackedSubscriptionGames");

            migrationBuilder.DropTable(
                name: "UserSubscriptions");

            migrationBuilder.DropTable(
                name: "GameStore");

            migrationBuilder.DropTable(
                name: "CharacterFrameData");

            migrationBuilder.DropTable(
                name: "RomHashInfos");

            migrationBuilder.DropTable(
                name: "TournamentParticipant");

            migrationBuilder.DropColumn(
                name: "EstimatedTimeToComplete",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "ExecutablePath",
                table: "Games");

            migrationBuilder.RenameIndex(
                name: "IX_MugenTournaments_Status1",
                table: "MugenTournaments",
                newName: "IX_MugenTournaments_Status");
        }
    }
}
