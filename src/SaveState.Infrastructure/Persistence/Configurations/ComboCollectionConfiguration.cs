using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.Mugen.ComboDatabase;

namespace SaveState.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for ComboCollection.
/// </summary>
public class ComboCollectionConfiguration : IEntityTypeConfiguration<ComboCollection>
{
    public void Configure(EntityTypeBuilder<ComboCollection> builder)
    {
        builder.ToTable("ComboCollections");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Description)
            .HasMaxLength(1000);

        builder.Property(c => c.CharacterName)
            .HasMaxLength(100);

        builder.Property(c => c.Creator)
            .IsRequired()
            .HasMaxLength(100);

        // JSON serialization for combo IDs
        builder.Property(c => c.ComboIds)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<Guid>());

        // Indexes
        builder.HasIndex(c => c.CharacterName)
            .HasDatabaseName("IX_ComboCollections_CharacterName");

        builder.HasIndex(c => c.Creator)
            .HasDatabaseName("IX_ComboCollections_Creator");

        builder.HasIndex(c => c.IsPublic)
            .HasDatabaseName("IX_ComboCollections_IsPublic");
    }
}
