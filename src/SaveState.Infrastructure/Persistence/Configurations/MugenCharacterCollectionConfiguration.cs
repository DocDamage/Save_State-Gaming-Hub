namespace SaveState.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.Mugen.Entities;

/// <summary>
/// Entity Framework configuration for MugenCharacterCollection.
/// </summary>
public class MugenCharacterCollectionConfiguration : IEntityTypeConfiguration<MugenCharacterCollection>
{
    public void Configure(EntityTypeBuilder<MugenCharacterCollection> builder)
    {
        builder.ToTable("MugenCharacterCollections");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Description)
            .HasMaxLength(1000);

        builder.Property(c => c.Icon)
            .HasMaxLength(500);

        builder.Property(c => c.UserId)
            .IsRequired();

        builder.Property(c => c.IsPublic)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.LastModified)
            .IsRequired();

        // Configure navigation properties
        builder.HasMany(c => c.Characters)
            .WithOne(cc => cc.Collection)
            .HasForeignKey(cc => cc.CollectionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.HasIndex(c => c.UserId);
        builder.HasIndex(c => c.IsPublic);
        builder.HasIndex(c => c.CreatedAt);
        builder.HasIndex(c => c.LastModified);
    }
}