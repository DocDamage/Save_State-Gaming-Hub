using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.OpenMK.Entities;

namespace SaveState.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for OpenMK progression entities.
/// </summary>
public class OpenMKProgressConfiguration :
    IEntityTypeConfiguration<OpenMKUserProgress>,
    IEntityTypeConfiguration<OpenMKCharacterUnlock>
{
    public void Configure(EntityTypeBuilder<OpenMKUserProgress> builder)
    {
        builder.ToTable("OpenMKUserProgress");

        builder.HasKey(p => p.UserId);

        builder.Property(p => p.Koins)
            .HasDefaultValue(0);

        builder.HasIndex(p => p.LastUpdatedAt)
            .HasDatabaseName("IX_OpenMKUserProgress_LastUpdatedAt");
    }

    public void Configure(EntityTypeBuilder<OpenMKCharacterUnlock> builder)
    {
        builder.ToTable("OpenMKCharacterUnlocks");

        builder.HasKey(u => u.Id);

        builder.HasIndex(u => new { u.UserId, u.CharacterId })
            .IsUnique()
            .HasDatabaseName("IX_OpenMKCharacterUnlocks_UserId_CharacterId");

        builder.HasIndex(u => u.UserId)
            .HasDatabaseName("IX_OpenMKCharacterUnlocks_UserId");
    }
}
