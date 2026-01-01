using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.SaveStates.Entities;
using SaveState.Core.GameLibrary.Entities;
using SaveStateEntity = SaveState.Core.SaveStates.Entities.SaveState;

namespace SaveState.Infrastructure.Persistence.Configurations.SaveStates;

public class SaveStateConfiguration : IEntityTypeConfiguration<SaveStateEntity>
{
    public void Configure(EntityTypeBuilder<SaveStateEntity> builder)
    {
        builder.HasKey(ss => ss.Id);

        builder.Property(ss => ss.GameId)
            .IsRequired();

        builder.Property(ss => ss.FilePath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(ss => ss.ThumbnailPath)
            .HasMaxLength(500);

        builder.Property(ss => ss.CreatedAt)
            .IsRequired();

        builder.Property(ss => ss.Description)
            .HasMaxLength(500);

        builder.Property(ss => ss.PlaytimeAtSave)
            .IsRequired();

        builder.Property(ss => ss.GameLocation)
            .HasMaxLength(200);

        builder.Property(ss => ss.ParentStateId);

        builder.Property(ss => ss.IsFavorite)
            .HasDefaultValue(false);

        builder.Property(ss => ss.IsAutoSave)
            .HasDefaultValue(false);

        builder.Property(ss => ss.FileSizeBytes)
            .HasDefaultValue(0L);

        // Relationships
        builder.HasOne<Game>()
            .WithMany()
            .HasForeignKey(ss => ss.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(ss => ss.GameId);
        builder.HasIndex(ss => ss.CreatedAt);
        builder.HasIndex(ss => ss.IsFavorite);
        builder.HasIndex(ss => new { ss.GameId, ss.CreatedAt });
        builder.HasIndex(ss => new { ss.GameId, ss.IsAutoSave });
    }
}