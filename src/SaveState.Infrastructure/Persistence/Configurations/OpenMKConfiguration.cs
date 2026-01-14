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

        // Configure UnlockRequirements as owned entity on the character
        builder.OwnsOne(c => c.UnlockRequirements, ur =>
        {
            ur.Property(u => u.Description).HasMaxLength(500).HasColumnName("UnlockDescription");
            ur.Property(u => u.Type).HasConversion<string>().HasColumnName("UnlockType");
            ur.Property(u => u.RequiredValue).HasColumnName("UnlockRequiredValue");
            ur.Property(u => u.RequiredCharacter).HasMaxLength(100).HasColumnName("UnlockRequiredCharacter");
            ur.Property(u => u.RequiredStage).HasMaxLength(100).HasColumnName("UnlockRequiredStage");
        });

        // Configure owned entities for collections
        builder.OwnsMany(c => c.SpecialMoves, sm =>
        {
            sm.ToTable("OpenMKCharacterSpecialMoves");
            sm.WithOwner().HasForeignKey("CharacterId");
            sm.HasKey("Id");
            sm.Property(sm => sm.Name).IsRequired().HasMaxLength(100);
            sm.Property(sm => sm.DisplayName).IsRequired().HasMaxLength(100);
            sm.Property(sm => sm.Description).HasMaxLength(500);
            sm.Property(sm => sm.InputCommand).IsRequired().HasMaxLength(100);
            sm.Property(sm => sm.Type).HasConversion<string>();
            sm.Property(sm => sm.AnimationName).HasMaxLength(100);
            sm.Property(sm => sm.SoundEffect).HasMaxLength(100);
        });

        builder.OwnsMany(c => c.Fatalities, f =>
        {
            f.ToTable("OpenMKCharacterFatalities");
            f.WithOwner().HasForeignKey("CharacterId");
            f.HasKey("Id");
            f.Property(f => f.Name).IsRequired().HasMaxLength(100);
            f.Property(f => f.DisplayName).IsRequired().HasMaxLength(100);
            f.Property(f => f.Description).HasMaxLength(500);
            f.Property(f => f.InputCommand).IsRequired().HasMaxLength(100);
            f.Property(f => f.Type).HasConversion<string>();
            f.Property(f => f.AnimationSequence).IsRequired().HasMaxLength(200);
            f.Property(f => f.SoundEffect).HasMaxLength(100);
            f.Property(f => f.VoiceLine).HasMaxLength(100);
        });

        builder.OwnsMany(c => c.Friendships, fr =>
        {
            fr.ToTable("OpenMKCharacterFriendships");
            fr.WithOwner().HasForeignKey("CharacterId");
            fr.HasKey("Id");
            fr.Property(fr => fr.Name).IsRequired().HasMaxLength(100);
            fr.Property(fr => fr.DisplayName).IsRequired().HasMaxLength(100);
            fr.Property(fr => fr.Description).HasMaxLength(500);
            fr.Property(fr => fr.InputCommand).IsRequired().HasMaxLength(100);
            fr.Property(fr => fr.AnimationSequence).IsRequired().HasMaxLength(200);
            fr.Property(fr => fr.SoundEffect).HasMaxLength(100);
            fr.Property(fr => fr.VoiceLine).HasMaxLength(100);
            fr.Property(fr => fr.ItemUsed).HasMaxLength(100);
        });

        builder.OwnsMany(c => c.Brutalities, br =>
        {
            br.ToTable("OpenMKCharacterBrutalities");
            br.WithOwner().HasForeignKey("CharacterId");
            br.HasKey("Id");
            br.Property(br => br.Name).IsRequired().HasMaxLength(100);
            br.Property(br => br.DisplayName).IsRequired().HasMaxLength(100);
            br.Property(br => br.Description).HasMaxLength(500);
            br.Property(br => br.InputCommand).IsRequired().HasMaxLength(100);
            br.Property(br => br.AnimationSequence).IsRequired().HasMaxLength(200);
            br.Property(br => br.SoundEffect).HasMaxLength(100);
            br.Property(br => br.VoiceLine).HasMaxLength(100);
        });

        builder.OwnsMany(c => c.Babalities, bb =>
        {
            bb.ToTable("OpenMKCharacterBabalities");
            bb.WithOwner().HasForeignKey("CharacterId");
            bb.HasKey("Id");
            bb.Property(bb => bb.Name).IsRequired().HasMaxLength(100);
            bb.Property(bb => bb.DisplayName).IsRequired().HasMaxLength(100);
            bb.Property(bb => bb.Description).HasMaxLength(500);
            bb.Property(bb => bb.InputCommand).IsRequired().HasMaxLength(100);
            bb.Property(bb => bb.AnimationSequence).IsRequired().HasMaxLength(200);
            bb.Property(bb => bb.SoundEffect).HasMaxLength(100);
            bb.Property(bb => bb.VoiceLine).HasMaxLength(100);
            bb.Property(bb => bb.BabyItem).HasMaxLength(100);
        });

        builder.OwnsMany(c => c.Costumes, co =>
        {
            co.ToTable("OpenMKCharacterCostumes");
            co.WithOwner().HasForeignKey("CharacterId");
            co.HasKey("Id");
            co.Property(co => co.Name).IsRequired().HasMaxLength(100);
            co.Property(co => co.DisplayName).IsRequired().HasMaxLength(100);
            co.Property(co => co.Description).HasMaxLength(500);
            co.Property(co => co.SpritePath).IsRequired().HasMaxLength(500);
            co.Property(co => co.IsDefault).IsRequired().HasDefaultValue(false);

            // Configure nested UnlockRequirements as owned entity
            co.OwnsOne(costume => costume.UnlockRequirements, ur =>
            {
                ur.Property(u => u.Description).HasMaxLength(500).HasColumnName("UnlockDescription");
                ur.Property(u => u.Type).HasConversion<string>().HasColumnName("UnlockType");
                ur.Property(u => u.RequiredValue).HasColumnName("UnlockRequiredValue");
                ur.Property(u => u.RequiredCharacter).HasMaxLength(100).HasColumnName("UnlockRequiredCharacter");
                ur.Property(u => u.RequiredStage).HasMaxLength(100).HasColumnName("UnlockRequiredStage");
            });
        });

        // Indexes for performance
        builder.HasIndex(c => c.Name).IsUnique().HasDatabaseName("IX_OpenMKCharacters_Name");
        builder.HasIndex(c => c.Realm).HasDatabaseName("IX_OpenMKCharacters_Realm");
        builder.HasIndex(c => c.FightingStyle).HasDatabaseName("IX_OpenMKCharacters_FightingStyle");
        builder.HasIndex(c => c.Alignment).HasDatabaseName("IX_OpenMKCharacters_Alignment");
        builder.HasIndex(c => c.IsDefaultUnlocked).HasDatabaseName("IX_OpenMKCharacters_IsDefaultUnlocked");
    }
}
