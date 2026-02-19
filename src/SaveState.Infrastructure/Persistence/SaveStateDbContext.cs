using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SaveState.Application.Common.Events;
using SaveState.Core.Common.Base;
using SaveState.Core.Common.Configuration;
using SaveState.Core.Common.Events;
using SaveState.Core.Common.Interfaces;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.RomManagement.Entities;
using SaveState.Core.RomManagement.RomValidation;
using SaveState.Core.AiGaming.Entities;
using SaveState.Core.Ai.Knowledge;
using SaveState.Core.Analytics.Entities;
using SaveState.Core.SaveStates.Entities;
using SaveStateEntity = SaveState.Core.SaveStates.Entities.SaveState;
using SaveStateBranchEntity = SaveState.Core.SaveStates.Entities.SaveStateBranch;
using SaveState.Core.Input.Entities;
using UserManagementUser = SaveState.Core.UserManagement.Entities.User;
using UserManagementRole = SaveState.Core.UserManagement.Entities.Role;
using UserManagementUserRole = SaveState.Core.UserManagement.Entities.UserRole;
using UserManagementPermission = SaveState.Core.UserManagement.Entities.Permission;
using UserManagementRolePermission = SaveState.Core.UserManagement.Entities.RolePermission;
using UserManagementApiKey = SaveState.Core.UserManagement.Entities.ApiKey;
using MugenTournamentEntity = SaveState.Core.Mugen.Entities.MugenTournament;
using MugenMatchHistoryEntity = SaveState.Core.Mugen.Entities.MugenMatchHistory;
using MugenMatchupStatsEntity = SaveState.Core.Mugen.Entities.MugenMatchupStats;
using MugenCharacterCollectionEntity = SaveState.Core.Mugen.Entities.MugenCharacterCollection;
using MugenCollectionCharacterEntity = SaveState.Core.Mugen.Entities.MugenCollectionCharacter;
using MugenTrainingSessionEntity = SaveState.Core.Mugen.Entities.MugenTrainingSession;
using MugenDummyRecordingEntity = SaveState.Core.Mugen.Entities.MugenDummyRecording;
using TournamentMatchEntity = SaveState.Core.Mugen.Entities.TournamentMatchEntity;
using TournamentParticipantEntity = SaveState.Core.Mugen.Entities.TournamentParticipant;
using NetworkQualityHistoryEntity = SaveState.Core.Sync.Entities.NetworkQualityHistory;
using SaveState.Core.Subscriptions;
using SaveState.Core.GameDeals;
using SaveState.Core.SmartLauncher;
using SaveState.Core.SaveStateCloudSync;
using AiBattleAnalysisModel = SaveState.Core.Mugen.AiBattleAnalysis.AiBattleAnalysis;
using SaveState.Core.Mugen.CharacterFusion;
using SaveState.Core.Mugen.DeathBattle;
using SaveState.Core.Mugen.ReplayAnalysis;
using SaveState.Core.Mugen.ComboDatabase;
using SaveState.Core.Mugen.TournamentEvents;
using SaveState.Core.Mugen.AiBattleAnalysis;
using SaveState.Core.AutoSave;
using SaveState.Infrastructure.Mugen.Services;

namespace SaveState.Infrastructure.Persistence;

public class SaveStateDbContext : DbContext, ISaveStateDbContext
{
    private readonly DatabaseOptions _options;
    private readonly IEventPublisher _eventPublisher;

    // Aggregate roots from bounded contexts
    public DbSet<Game> Games { get; set; }
    public DbSet<GameFile> GameFiles { get; set; }
    public DbSet<GameNote> GameNotes { get; set; }
    public DbSet<GameMod> GameMods { get; set; }
    public DbSet<GameMedia> GameMedia { get; set; }
    public DbSet<Platform> Platforms { get; set; }
    public DbSet<Genre> Genres { get; set; }
    public DbSet<Developer> Developers { get; set; }
    public DbSet<Publisher> Publishers { get; set; }
    public DbSet<RomFile> RomFiles { get; set; }
    public DbSet<RomHashInfo> RomHashInfos { get; set; }
    public DbSet<RomValidationReport> RomValidationReports { get; set; }
    public DbSet<Emulator> Emulators { get; set; }
    public DbSet<AiModel> AiModels { get; set; }
    public DbSet<MemorySnapshot> MemorySnapshots { get; set; }
    // User Management entities (using aliases to avoid ambiguity with GameLibrary.User)
    public DbSet<UserManagementUser> Users { get; set; }
    public DbSet<UserManagementRole> Roles { get; set; }
    public DbSet<UserManagementUserRole> UserRoles { get; set; }
    public DbSet<UserManagementPermission> Permissions { get; set; }
    public DbSet<UserManagementRolePermission> RolePermissions { get; set; }
    public DbSet<UserManagementApiKey> ApiKeys { get; set; }
    public DbSet<SaveState.Core.Api.Entities.ApiKey> ExternalApiKeys { get; set; }
    public DbSet<Achievement> Achievements { get; set; }
    public DbSet<UserAchievement> UserAchievements { get; set; }

    // MUGEN Character Management
    public DbSet<SaveState.Core.Mugen.Entities.MugenCharacter> MugenCharacters { get; set; }
    public DbSet<MugenTournamentEntity> MugenTournaments { get; set; }
    public DbSet<TournamentParticipantEntity> TournamentParticipants { get; set; }
    public DbSet<TournamentMatchEntity> TournamentMatches { get; set; }
    public DbSet<MugenMatchHistoryEntity> MugenMatchHistories { get; set; }
    public DbSet<MugenMatchupStatsEntity> MugenMatchupStats { get; set; }
    public DbSet<MugenCharacterCollectionEntity> MugenCharacterCollections { get; set; }
    public DbSet<MugenCollectionCharacterEntity> MugenCollectionCharacters { get; set; }

    // OpenMK Integration
    public DbSet<SaveState.Core.OpenMK.Entities.OpenMKCharacter> OpenMKCharacters { get; set; }
    public DbSet<SaveState.Core.OpenMK.Entities.OpenMKMatchState> OpenMKMatchStates { get; set; }
    public DbSet<SaveState.Core.OpenMK.Entities.OpenMKUserProgress> OpenMKUserProgresses { get; set; }
    public DbSet<SaveState.Core.OpenMK.Entities.OpenMKCharacterUnlock> OpenMKCharacterUnlocks { get; set; }
    public DbSet<MugenTrainingSessionEntity> MugenTrainingSessions { get; set; }
    public DbSet<MugenDummyRecordingEntity> MugenDummyRecordings { get; set; }
    public DbSet<Backup> Backups { get; set; }
    public DbSet<KnowledgeRecord> KnowledgeRecords { get; set; }
    public DbSet<GameSession> GameSessions { get; set; }
    public DbSet<BacklogEntry> BacklogEntries { get; set; }
    public DbSet<GamingGoal> GamingGoals { get; set; }
    public DbSet<VirtualCollection> VirtualCollections { get; set; }
    public DbSet<VirtualCollectionGame> VirtualCollectionGames { get; set; }
    public DbSet<SaveStateEntity> SaveStates { get; set; }
    public DbSet<SaveStateBranchEntity> SaveStateBranches { get; set; }
    public DbSet<ControllerProfile> ControllerProfiles { get; set; }
    public DbSet<SaveState.Core.Social.Entities.Challenge> Challenges { get; set; }
    public DbSet<SaveState.Core.Social.Entities.Leaderboard> Leaderboards { get; set; }
    public DbSet<Core.Social.Entities.GameReview> GameReviews { get; set; }
    public DbSet<Core.Social.Entities.SharedCollection> SharedCollections { get; set; }
    public DbSet<Core.Social.Entities.SharedCollectionItem> SharedCollectionItems { get; set; }
    public DbSet<Core.Social.Entities.Friend> Friends { get; set; }
    public DbSet<Core.Social.Entities.FriendActivity> FriendActivities { get; set; }
    public DbSet<Core.Sync.Entities.NetworkQualityHistory> NetworkQualityHistories { get; set; }

    // Subscription Management
    public DbSet<UserSubscriptionEntity> UserSubscriptions { get; set; }
    public DbSet<TrackedGameEntity> TrackedSubscriptionGames { get; set; }
    public DbSet<SubscriptionCatalogCacheEntity> SubscriptionCatalogCache { get; set; }

    // Game Deals
    public DbSet<Core.GameDeals.GameDeal> GameDeals { get; set; }
    public DbSet<Core.GameDeals.PriceHistoryEntry> PriceHistory { get; set; }
    public DbSet<Core.GameDeals.PriceAlert> PriceAlerts { get; set; }

    // Smart Launcher
    public DbSet<LaunchProfile> LaunchProfiles { get; set; }
    public DbSet<LaunchSession> LaunchSessions { get; set; }

    // Frame Data
    public DbSet<CharacterFrameDataEntity> CharacterFrameData { get; set; }
    public DbSet<MoveFrameDataEntity> MoveFrameData { get; set; }

    // Cloud Sync
    public DbSet<CloudSaveState> CloudSaveStates { get; set; }

    // AI Battle Analysis
    public DbSet<AiBattleAnalysisModel> AiBattleAnalyses { get; set; }

    // Character Fusion
    public DbSet<FusedCharacter> FusedCharacters { get; set; }
    public DbSet<FusionBattleHistory> FusionBattleHistories { get; set; }

    // Death Battle
    public DbSet<DeathBattleMatch> DeathBattleMatches { get; set; }
    public DbSet<DeathBattleSuggestion> DeathBattleSuggestions { get; set; }

    // Replay Analysis
    public DbSet<ReplayAnalysis> ReplayAnalyses { get; set; }

    // Combo Database
    public DbSet<ComboEntry> ComboEntries { get; set; }
    public DbSet<ComboPracticeSession> ComboPracticeSessions { get; set; }
    public DbSet<ComboSubmission> ComboSubmissions { get; set; }
    public DbSet<ComboCollection> ComboCollections { get; set; }

    // Tournament Events
    public DbSet<TournamentEvent> TournamentEvents { get; set; }

    // Auto Save
    public DbSet<AutoSaveConfiguration> AutoSaveConfigurations { get; set; }
    public DbSet<AutoSaveEntry> AutoSaveEntries { get; set; }

    // Input Recording
    public DbSet<Core.InputRecording.InputRecording> InputRecordings { get; set; }

    public SaveStateDbContext(DbContextOptions<SaveStateDbContext> options)
        : base(options)
    {
        // Simplified constructor for walking skeleton
        _options = new DatabaseOptions();
        _eventPublisher = null!;

        // Enable WAL mode for better concurrency - Disabled in constructor to prevent breaking EnsureCreatedAsync
        // EnableWalMode();
    }

    public SaveStateDbContext(
        DbContextOptions<SaveStateDbContext> options,
        IOptions<DatabaseOptions> dbOptions,
        IEventPublisher eventPublisher)
        : base(options)
    {
        _options = dbOptions.Value;
        _eventPublisher = eventPublisher;

        // Enable WAL mode for better concurrency - Disabled in constructor to prevent breaking EnsureCreatedAsync
        // EnableWalMode();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SaveStateDbContext).Assembly);

        // Configure owned entities
        modelBuilder.Entity<SaveState.Core.Mugen.Entities.MugenCharacter>()
            .OwnsOne(e => e.Directories);
        modelBuilder.Entity<SaveState.Core.Mugen.Entities.MugenCharacter>()
            .OwnsOne(e => e.PaletteInfo);
        modelBuilder.Entity<SaveState.Core.Mugen.Entities.MugenCharacter>()
            .OwnsOne(e => e.ArcadeInfo);

        // Configure relationships
        modelBuilder.Entity<Game>()
            .HasMany(g => g.Genres)
            .WithMany();

        // Global configurations
        ConfigureGlobalFilters(modelBuilder);
        ConfigureConversions(modelBuilder);
        ConfigureIndexes(modelBuilder);
        IgnoreUnkeyedNonOwnedTypes(modelBuilder);
    }


    private static void ConfigureGlobalFilters(ModelBuilder modelBuilder)
    {
        // Soft delete filter
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "p");
                var property = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
                var filter = Expression.Lambda(Expression.Not(property), parameter);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }
    }

    private static void ConfigureConversions(ModelBuilder modelBuilder)
    {
        // Value object conversions will be added as entities are created

        // Tags conversion (JSON)
        modelBuilder.Entity<Game>()
            .Property(g => g.Tags)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>(),
                new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<ICollection<string>>(
                    (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => (ICollection<string>)c.ToList()));

        // AiBattleAnalysis complex properties are stored as JSON payloads.
        modelBuilder.Entity<AiBattleAnalysisModel>()
            .Property(a => a.Stats)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<CombatStats>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new CombatStats());

        modelBuilder.Entity<AiBattleAnalysisModel>()
            .Property(a => a.Patterns)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<DetectedPattern>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<DetectedPattern>());

        modelBuilder.Entity<AiBattleAnalysisModel>()
            .Property(a => a.Weaknesses)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<PlayerWeakness>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<PlayerWeakness>());

        modelBuilder.Entity<AiBattleAnalysisModel>()
            .Property(a => a.Opportunities)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<ImprovementOpportunity>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<ImprovementOpportunity>());

        modelBuilder.Entity<AiBattleAnalysisModel>()
            .Property(a => a.Recommendations)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<CounterStrategy>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<CounterStrategy>());

        modelBuilder.Entity<AiBattleAnalysisModel>()
            .Property(a => a.Insights)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<AiInsights>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new AiInsights());

        // Ignore computed GameDeal properties that are not persisted.
        modelBuilder.Entity<Core.GameDeals.GameDeal>().Ignore(d => d.IsActive);
        modelBuilder.Entity<Core.GameDeals.GameDeal>().Ignore(d => d.DiscountPercent);
        modelBuilder.Entity<Core.GameDeals.GameDeal>().Ignore(d => d.FormattedPrice);
        modelBuilder.Entity<Core.GameDeals.GameDeal>().Ignore(d => d.FormattedRegularPrice);
        modelBuilder.Entity<Core.GameDeals.GameDeal>().Ignore(d => d.FormattedDiscount);
        modelBuilder.Entity<Core.GameDeals.GameDeal>().Ignore(d => d.Savings);

        // SmartLauncher complex settings payloads are stored as JSON.
        modelBuilder.Entity<LaunchProfile>()
            .Property(p => p.ProcessesToSuspend)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());

        modelBuilder.Entity<LaunchProfile>()
            .Property(p => p.ServicesToStop)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());

        modelBuilder.Entity<LaunchProfile>()
            .Property(p => p.DisplaySettings)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<DisplaySettings>(v, (System.Text.Json.JsonSerializerOptions?)null));

        modelBuilder.Entity<LaunchProfile>()
            .Property(p => p.PerformanceSettings)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<PerformanceSettings>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new PerformanceSettings());

        modelBuilder.Entity<LaunchSession>().Ignore(s => s.Duration);
        modelBuilder.Entity<LaunchSession>().Ignore(s => s.IsActive);

        modelBuilder.Entity<LaunchSession>()
            .Property(s => s.InitialSystemState)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<SystemState>(v, (System.Text.Json.JsonSerializerOptions?)null));

        modelBuilder.Entity<LaunchSession>()
            .Property(s => s.PerformanceMetrics)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<SessionPerformanceMetrics>(v, (System.Text.Json.JsonSerializerOptions?)null));
    }

    private static void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        // Performance indexes for common query patterns

        // Game table indexes
        modelBuilder.Entity<Game>()
            .HasIndex(g => g.CreatedAt)
            .HasDatabaseName("IX_Games_CreatedAt");

        modelBuilder.Entity<Game>()
            .HasIndex(g => g.Title)
            .HasDatabaseName("IX_Games_Title");

        modelBuilder.Entity<Game>()
            .HasIndex(g => new { g.PlatformId, g.Title })
            .HasDatabaseName("IX_Games_PlatformId_Title");

        modelBuilder.Entity<Game>()
            .HasIndex(g => g.Status)
            .HasDatabaseName("IX_Games_Status");

        modelBuilder.Entity<Game>()
            .HasIndex(g => g.LastPlayedAt)
            .HasDatabaseName("IX_Games_LastPlayedAt");

        modelBuilder.Entity<Game>()
            .HasIndex(g => g.TotalPlayTime)
            .HasDatabaseName("IX_Games_TotalPlayTime");

        // PERFORMANCE OPTIMIZATION: Composite indexes for common query patterns
        modelBuilder.Entity<Game>()
            .HasIndex(g => new { g.Status, g.LastPlayedAt })
            .HasDatabaseName("IX_Games_Status_LastPlayedAt");

        modelBuilder.Entity<Game>()
            .HasIndex(g => new { g.PlatformId, g.Status, g.Title })
            .HasDatabaseName("IX_Games_Platform_Status_Title");

        modelBuilder.Entity<Game>()
            .HasIndex(g => new { g.CreatedAt, g.PlatformId })
            .HasDatabaseName("IX_Games_CreatedAt_Platform");

        // PERFORMANCE OPTIMIZATION: Covering index for game summaries
        modelBuilder.Entity<Game>()
            .HasIndex(g => new { g.Id, g.Title, g.PlatformId, g.Status, g.LastPlayedAt, g.TotalPlayTime })
            .HasDatabaseName("IX_Games_Summary_Covering");

        // ROM Files table indexes
        modelBuilder.Entity<RomFile>()
            .HasIndex(r => new { r.PlatformId, r.FilePath })
            .HasDatabaseName("IX_RomFiles_PlatformId_FilePath");

        modelBuilder.Entity<RomFile>()
            .HasIndex(r => r.PlatformId)
            .HasDatabaseName("IX_RomFiles_PlatformId");

        // Achievement table indexes
        modelBuilder.Entity<Achievement>()
            .HasIndex(a => new { a.Type, a.IsActive })
            .HasDatabaseName("IX_Achievements_Type_IsActive");

        modelBuilder.Entity<Achievement>()
            .HasIndex(a => a.Type)
            .HasDatabaseName("IX_Achievements_Type");

        modelBuilder.Entity<Achievement>()
            .HasIndex(a => a.IsActive)
            .HasDatabaseName("IX_Achievements_IsActive");

        // User Achievement table indexes
        modelBuilder.Entity<UserAchievement>()
            .HasIndex(ua => new { ua.UserId, ua.AchievementId })
            .HasDatabaseName("IX_UserAchievements_UserId_AchievementId");

        modelBuilder.Entity<UserAchievement>()
            .HasIndex(ua => ua.UserId)
            .HasDatabaseName("IX_UserAchievements_UserId");

        modelBuilder.Entity<UserAchievement>()
            .HasIndex(ua => ua.AchievementId)
            .HasDatabaseName("IX_UserAchievements_AchievementId");

        // MUGEN Character table indexes
        modelBuilder.Entity<SaveState.Core.Mugen.Entities.MugenCharacter>()
            .HasIndex(c => c.Name)
            .HasDatabaseName("IX_MugenCharacters_Name");

        modelBuilder.Entity<SaveState.Core.Mugen.Entities.MugenCharacter>()
            .HasIndex(c => c.Author)
            .HasDatabaseName("IX_MugenCharacters_Author");

        // Platform table indexes
        modelBuilder.Entity<Platform>()
            .HasIndex(p => p.Name)
            .HasDatabaseName("IX_Platforms_Name");

        // Knowledge Records table indexes (for AI features)
        modelBuilder.Entity<KnowledgeRecord>()
            .HasIndex(kr => kr.Id)
            .HasDatabaseName("IX_KnowledgeRecords_Id");

        modelBuilder.Entity<KnowledgeRecord>()
            .HasIndex(kr => kr.LastAccessedAt)
            .HasDatabaseName("IX_KnowledgeRecords_LastAccessedAt");

        // Network Quality History table indexes
        modelBuilder.Entity<NetworkQualityHistoryEntity>()
            .HasIndex(nqh => nqh.MeasuredAt)
            .HasDatabaseName("IX_NetworkQualityHistories_MeasuredAt");

        modelBuilder.Entity<NetworkQualityHistoryEntity>()
            .HasIndex(nqh => nqh.SessionId)
            .HasDatabaseName("IX_NetworkQualityHistories_SessionId");

        modelBuilder.Entity<NetworkQualityHistoryEntity>()
            .HasIndex(nqh => new { nqh.SessionId, nqh.MeasuredAt })
            .HasDatabaseName("IX_NetworkQualityHistories_SessionId_MeasuredAt");

        modelBuilder.Entity<NetworkQualityHistoryEntity>()
            .HasIndex(nqh => nqh.Level)
            .HasDatabaseName("IX_NetworkQualityHistories_Level");

        // Subscription Management indexes
        modelBuilder.Entity<UserSubscriptionEntity>()
            .HasIndex(us => new { us.UserId, us.ServiceId })
            .HasDatabaseName("IX_UserSubscriptions_UserId_ServiceId");

        modelBuilder.Entity<UserSubscriptionEntity>()
            .HasIndex(us => us.UserId)
            .HasDatabaseName("IX_UserSubscriptions_UserId");

        modelBuilder.Entity<UserSubscriptionEntity>()
            .HasIndex(us => us.IsActive)
            .HasDatabaseName("IX_UserSubscriptions_IsActive");

        modelBuilder.Entity<TrackedGameEntity>()
            .HasIndex(tg => new { tg.UserId, tg.GameTitle })
            .HasDatabaseName("IX_TrackedSubscriptionGames_UserId_GameTitle");

        modelBuilder.Entity<SubscriptionCatalogCacheEntity>()
            .HasIndex(cc => new { cc.ServiceId, cc.GameTitle })
            .HasDatabaseName("IX_SubscriptionCatalogCache_ServiceId_GameTitle");

        modelBuilder.Entity<SubscriptionCatalogCacheEntity>()
            .HasIndex(cc => cc.DateLeaving)
            .HasDatabaseName("IX_SubscriptionCatalogCache_DateLeaving");

        // Game Deals indexes
        modelBuilder.Entity<Core.GameDeals.GameDeal>()
            .HasIndex(d => d.Title)
            .HasDatabaseName("IX_GameDeals_Title");

        modelBuilder.Entity<Core.GameDeals.GameDeal>()
            .HasIndex(d => d.CurrentPrice)
            .HasDatabaseName("IX_GameDeals_CurrentPrice");

        modelBuilder.Entity<Core.GameDeals.GameDeal>()
            .HasIndex(d => new { d.DealEnd, d.LastUpdated })
            .HasDatabaseName("IX_GameDeals_DealEnd_LastUpdated");

        modelBuilder.Entity<Core.GameDeals.PriceHistoryEntry>()
            .HasIndex(h => new { h.GameTitle, h.StoreId })
            .HasDatabaseName("IX_PriceHistory_GameTitle_StoreId");

        modelBuilder.Entity<Core.GameDeals.PriceHistoryEntry>()
            .HasIndex(h => h.Date)
            .HasDatabaseName("IX_PriceHistory_Date");

        modelBuilder.Entity<Core.GameDeals.PriceAlert>()
            .HasIndex(a => a.UserId)
            .HasDatabaseName("IX_PriceAlerts_UserId");

        modelBuilder.Entity<Core.GameDeals.PriceAlert>()
            .HasIndex(a => new { a.IsActive, a.GameTitle })
            .HasDatabaseName("IX_PriceAlerts_IsActive_GameTitle");

        // Smart Launcher indexes
        modelBuilder.Entity<LaunchProfile>()
            .HasIndex(p => new { p.GameId, p.IsDefault })
            .HasDatabaseName("IX_LaunchProfiles_GameId_IsDefault");

        modelBuilder.Entity<LaunchProfile>()
            .HasIndex(p => p.IsActive)
            .HasDatabaseName("IX_LaunchProfiles_IsActive");

        modelBuilder.Entity<LaunchSession>()
            .HasIndex(s => new { s.GameId, s.EndedAt })
            .HasDatabaseName("IX_LaunchSessions_GameId_EndedAt");

        modelBuilder.Entity<LaunchSession>()
            .HasIndex(s => s.StartedAt)
            .HasDatabaseName("IX_LaunchSessions_StartedAt");
    }

    private static void IgnoreUnkeyedNonOwnedTypes(ModelBuilder modelBuilder)
    {
        var transientTypes = modelBuilder.Model.GetEntityTypes()
            .Where(entityType =>
                entityType.FindPrimaryKey() is null &&
                !entityType.IsOwned() &&
                !entityType.IsKeyless)
            .Select(entityType => entityType.ClrType)
            .Distinct()
            .ToList();

        foreach (var clrType in transientTypes)
        {
            modelBuilder.Ignore(clrType);
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Publish domain events before saving (if event publisher is available)
        if (_eventPublisher != null)
        {
            await PublishDomainEventsAsync().ConfigureAwait(false);
        }

        // Handle soft deletes
        HandleSoftDeletes();

        return await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task PublishDomainEventsAsync()
    {
        var entitiesWithEvents = ChangeTracker
            .Entries<EntityBase>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = entitiesWithEvents
            .SelectMany(e => e.DomainEvents)
            .ToList();

        // Clear events from entities
        entitiesWithEvents.ForEach(e => e.ClearDomainEvents());

        // Publish events
        foreach (var domainEvent in domainEvents)
        {
            await _eventPublisher.PublishAsync(domainEvent).ConfigureAwait(false);
        }
    }

    private void HandleSoftDeletes()
    {
        var entries = ChangeTracker
            .Entries<ISoftDelete>()
            .Where(e => e.State == EntityState.Deleted);

        foreach (var entry in entries)
        {
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAt = DateTime.UtcNow;
        }
    }

    public void EnableWalMode()
    {
        try
        {
            // Enable WAL mode for better concurrency support in SQLite
            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
                Database.ExecuteSqlRaw("PRAGMA synchronous=NORMAL;");
                Database.ExecuteSqlRaw("PRAGMA cache_size=10000;");
                Database.ExecuteSqlRaw("PRAGMA temp_store=MEMORY;");
            }
        }
        catch
        {
            // WAL mode is not critical for basic functionality
            // Tests will be marked as skipped for SQLite concurrency limitations
        }
    }
}
