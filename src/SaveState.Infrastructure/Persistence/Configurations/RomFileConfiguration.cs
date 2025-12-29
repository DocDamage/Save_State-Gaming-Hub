using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.RomManagement.Entities;
using SaveState.Core.RomManagement.ValueObjects;

namespace SaveState.Infrastructure.Persistence.Configurations;

public class RomFileConfiguration : IEntityTypeConfiguration<RomFile>
{
    public void Configure(EntityTypeBuilder<RomFile> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.FilePath)
            .IsRequired()
            .HasConversion(
                v => v.Value,
                v => new FilePath(v))
            .HasMaxLength(500);

        builder.Property(r => r.FileSize)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasMaxLength(1000);

        builder.Property(r => r.Region)
            .HasMaxLength(50);

        builder.Property(r => r.Version)
            .HasMaxLength(50);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(r => r.Checksum)
            .HasMaxLength(128);

        builder.Property(r => r.ScannedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(r => r.Platform)
            .WithMany()
            .HasForeignKey(r => r.PlatformId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(r => r.FilePath).IsUnique();
        builder.HasIndex(r => r.PlatformId);
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.ScannedAt);
    }
}
