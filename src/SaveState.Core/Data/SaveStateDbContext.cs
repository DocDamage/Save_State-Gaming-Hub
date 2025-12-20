using Microsoft.EntityFrameworkCore;
using SaveState.Core.Entities;

namespace SaveState.Core.Data;

public class SaveStateDbContext : DbContext
{
    public DbSet<Game> Games => Set<Game>();
    public DbSet<Platform> Platforms => Set<Platform>();
    public DbSet<GameImage> GameImages => Set<GameImage>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<Collection> Collections => Set<Collection>();
    public DbSet<PlaySession> PlaySessions => Set<PlaySession>();
    public DbSet<Emulator> Emulators => Set<Emulator>();
    public DbSet<RomFolder> RomFolders => Set<RomFolder>();
    public DbSet<GameActivity> GameActivities => Set<GameActivity>();

    public SaveStateDbContext(DbContextOptions<SaveStateDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Many-to-Many for Collections and Games
        modelBuilder.Entity<Collection>()
            .HasMany(c => c.Games)
            .WithMany(g => g.Collections)
            .UsingEntity(j => j.ToTable("GameCollections"));

        // Platform relationship
        modelBuilder.Entity<Game>()
            .HasOne(g => g.Platform)
            .WithMany(p => p.Games)
            .HasForeignKey(g => g.PlatformId)
            .OnDelete(DeleteBehavior.Restrict);

        // Images relationship
        modelBuilder.Entity<GameImage>()
            .HasOne(i => i.Game)
            .WithMany(g => g.Images)
            .HasForeignKey(i => i.GameId);

        // Achievements relationship
        modelBuilder.Entity<Achievement>()
            .HasOne(a => a.Game)
            .WithMany(g => g.Achievements)
            .HasForeignKey(a => a.GameId);

        // PlaySessions relationship
        modelBuilder.Entity<PlaySession>()
            .HasOne(s => s.Game)
            .WithMany(g => g.PlaySessions)
            .HasForeignKey(s => s.GameId);
            
        // Additional indexes
        modelBuilder.Entity<Game>()
            .HasIndex(g => g.Title);
            
        modelBuilder.Entity<Game>()
            .HasIndex(g => g.SourceId);
    }
}
