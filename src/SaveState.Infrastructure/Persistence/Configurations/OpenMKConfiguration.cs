using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.OpenMK.Entities;

namespace SaveState.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for OpenMK entities.
/// </summary>
public class OpenMKConfiguration : IEntityTypeConfiguration<OpenMKCharacter>
{
    /// <summary>
    /// Configures the OpenMKCharacter entity.
    /// </summary>
    public void Configure(EntityTypeBuilder<OpenMKCharacter> builder)
    {
        builder.ToTable("OpenMKCharacters");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.DisplayName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Bio)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(c => c.Realm)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(c => c.FightingStyle)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(c => c.Alignment)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(c => c.SpritePath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(c => c.SoundPath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(c => c.DefinitionPath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(c => c.Ending)
            .HasMaxLength(2000);

        builder.Property(c => c.IsDefaultUnlocked)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Ignore(c => c.SpecialMoves);
        builder.Ignore(c => c.Fatalities);
        builder.Ignore(c => c.Friendships);
        builder.Ignore(c => c.Brutalities);
        builder.Ignore(c => c.BabalityMoves);
        builder.Ignore(c => c.Costumes);

        builder.HasIndex(c => c.Name)
            .IsUnique()
            .HasDatabaseName("IX_OpenMKCharacters_Name");
    }
}
