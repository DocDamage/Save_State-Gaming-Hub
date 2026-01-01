namespace SaveState.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.Mugen.Entities;

/// <summary>
/// Entity Framework configuration for MugenCollectionCharacter.
/// </summary>
public class MugenCollectionCharacterConfiguration : IEntityTypeConfiguration<MugenCollectionCharacter>
{
    public void Configure(EntityTypeBuilder<MugenCollectionCharacter> builder)
    {
        builder.ToTable("MugenCollectionCharacters");

        builder.HasKey(cc => cc.Id);

        // Composite key to ensure uniqueness within a collection
        builder.HasAlternateKey(cc => new { cc.CollectionId, cc.CharacterId });

        builder.Property(cc => cc.CollectionId)
            .IsRequired();

        builder.Property(cc => cc.CharacterId)
            .IsRequired();

        builder.Property(cc => cc.Notes)
            .HasMaxLength(500);

        builder.Property(cc => cc.IsFavorite)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(cc => cc.AddedAt)
            .IsRequired();

        // Configure navigation properties
        builder.HasOne(cc => cc.Collection)
            .WithMany(c => c.Characters)
            .HasForeignKey(cc => cc.CollectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cc => cc.Character)
            .WithMany() // MugenCharacter doesn't have navigation back
            .HasForeignKey(cc => cc.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.HasIndex(cc => cc.CollectionId);
        builder.HasIndex(cc => cc.CharacterId);
        builder.HasIndex(cc => new { cc.CollectionId, cc.CharacterId });
        builder.HasIndex(cc => cc.IsFavorite);
    }
}