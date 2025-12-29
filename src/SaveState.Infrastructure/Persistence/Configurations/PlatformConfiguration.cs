using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.ValueObjects;

namespace SaveState.Infrastructure.Persistence.Configurations;

public class PlatformConfiguration : IEntityTypeConfiguration<Platform>
{
    public void Configure(EntityTypeBuilder<Platform> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasConversion(
                v => v.Value,
                v => PlatformName.From(v))
            .HasMaxLength(100);

        builder.Property(p => p.ShortName)
            .IsRequired()
            .HasConversion(
                v => v.Value,
                v => PlatformShortName.From(v))
            .HasMaxLength(20);

        builder.Property(p => p.Type)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(p => p.Manufacturer)
            .HasMaxLength(100);

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(p => p.Name).IsUnique();
        builder.HasIndex(p => p.ShortName).IsUnique();
        builder.HasIndex(p => p.Type);
    }
}
