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
using SaveState.Core.AiGaming.Entities;
using SaveState.Core.Ai.Knowledge;

namespace SaveState.Infrastructure.Persistence;

public class SaveStateDbContext : DbContext, ISaveStateDbContext
{
    private readonly DatabaseOptions _options;
    private readonly IEventPublisher _eventPublisher;

    // Aggregate roots from bounded contexts
    public DbSet<Game> Games { get; set; }
    public DbSet<GameFile> GameFiles { get; set; }
    public DbSet<Platform> Platforms { get; set; }
    public DbSet<Genre> Genres { get; set; }
    public DbSet<Developer> Developers { get; set; }
    public DbSet<Publisher> Publishers { get; set; }
    public DbSet<RomFile> RomFiles { get; set; }
    public DbSet<Emulator> Emulators { get; set; }
    public DbSet<AiModel> AiModels { get; set; }
    public DbSet<MemorySnapshot> MemorySnapshots { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Achievement> Achievements { get; set; }
    public DbSet<UserAchievement> UserAchievements { get; set; }

    // MUGEN Character Management
    public DbSet<SaveState.Core.Mugen.Entities.MugenCharacter> MugenCharacters { get; set; }
    public DbSet<Backup> Backups { get; set; }
    public DbSet<KnowledgeRecord> KnowledgeRecords { get; set; }


    public SaveStateDbContext(DbContextOptions<SaveStateDbContext> options)
        : base(options)
    {
        // Simplified constructor for walking skeleton
        _options = new DatabaseOptions();
        _eventPublisher = null!;
    }

    public SaveStateDbContext(
        DbContextOptions<SaveStateDbContext> options,
        IOptions<DatabaseOptions> dbOptions,
        IEventPublisher eventPublisher)
        : base(options)
    {
        _options = dbOptions.Value;
        _eventPublisher = eventPublisher;
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

        // Global configurations
        ConfigureGlobalFilters(modelBuilder);
        ConfigureConversions(modelBuilder);
        ConfigureIndexes(modelBuilder);
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
}
