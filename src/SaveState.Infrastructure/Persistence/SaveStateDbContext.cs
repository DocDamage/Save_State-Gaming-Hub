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
        // Global indexes will be added as needed
        modelBuilder.Entity<Game>()
            .HasIndex(g => g.CreatedAt);

        modelBuilder.Entity<Game>()
            .HasIndex(g => g.Title);
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
