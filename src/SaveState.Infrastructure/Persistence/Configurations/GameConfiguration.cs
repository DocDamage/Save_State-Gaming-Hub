using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Infrastructure.Persistence.Configurations;

public class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.HasKey(g => g.Id);
        builder.Ignore(g => g.Platforms);

        builder.Property(g => g.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(g => g.Description)
            .HasMaxLength(1000);

        builder.Property(g => g.CoverImagePath)
            .HasMaxLength(500);

        builder.Property(g => g.InstallPath)
            .HasMaxLength(500);

        builder.Property(g => g.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(g => g.CreatedAt)
            .IsRequired();

        builder.Property(g => g.UpdatedAt);

        // Relationships
        builder.HasOne(g => g.Platform)
            .WithMany()
            .HasForeignKey(g => g.PlatformId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(g => g.Title);
        builder.HasIndex(g => g.CreatedAt);
        builder.HasIndex(g => g.Status);
        builder.HasIndex(g => g.PlatformId);
    }
}
