using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.RomManagement.Entities;
using SaveState.Core.RomManagement.ValueObjects;

namespace SaveState.Infrastructure.Persistence.Configurations;

public class EmulatorConfiguration : IEntityTypeConfiguration<Emulator>
{
    public void Configure(EntityTypeBuilder<Emulator> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.ExecutablePath)
            .IsRequired()
            .HasConversion(
                v => v.Value,
                v => new FilePath(v))
            .HasMaxLength(500);

        builder.Property(e => e.Version)
            .HasMaxLength(50);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.CommandLineArgs)
            .HasMaxLength(1000);

        // Relationships
        builder.HasOne(e => e.Platform)
            .WithMany()
            .HasForeignKey(e => e.PlatformId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(e => e.Name);
        builder.HasIndex(e => e.PlatformId);
        builder.HasIndex(e => e.IsAvailable);
    }
}
