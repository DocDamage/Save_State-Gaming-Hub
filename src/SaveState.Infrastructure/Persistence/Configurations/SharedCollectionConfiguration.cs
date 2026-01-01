using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.Social.Entities;

namespace SaveState.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuration for the SharedCollection entity.
/// </summary>
public class SharedCollectionConfiguration : IEntityTypeConfiguration<SharedCollection>
{
    public void Configure(EntityTypeBuilder<SharedCollection> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Description)
            .HasMaxLength(1000);

        builder.Property(c => c.ShareCode)
            .IsRequired()
            .HasMaxLength(8)
            .IsUnicode(false);

        builder.Property(c => c.IsPublic)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(c => c.DownloadCount)
            .HasDefaultValue(0);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt);

        // Relationships
        builder.HasMany(c => c.Items)
            .WithOne(i => i.Collection)
            .HasForeignKey(i => i.CollectionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(c => c.ShareCode)
            .IsUnique();

        builder.HasIndex(c => c.IsPublic);
        builder.HasIndex(c => c.CreatedAt);
        builder.HasIndex(c => c.Title);
    }
}

/// <summary>
/// Configuration for the SharedCollectionItem entity.
/// </summary>
public class SharedCollectionItemConfiguration : IEntityTypeConfiguration<SharedCollectionItem>
{
    public void Configure(EntityTypeBuilder<SharedCollectionItem> builder)
    {
        builder.HasKey(i => new { i.CollectionId, i.GameTitle });

        builder.Property(i => i.GameTitle)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.Notes)
            .HasMaxLength(500);

        builder.Property(i => i.SortOrder)
            .HasDefaultValue(0);

        builder.HasOne(i => i.Collection)
            .WithMany(c => c.Items)
            .HasForeignKey(i => i.CollectionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(i => i.GameTitle);
        builder.HasIndex(i => new { i.CollectionId, i.SortOrder });
    }
}