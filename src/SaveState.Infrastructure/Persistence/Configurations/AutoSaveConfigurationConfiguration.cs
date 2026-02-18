using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.AutoSave;

namespace SaveState.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for AutoSaveConfiguration.
/// </summary>
public class AutoSaveConfigurationConfiguration : IEntityTypeConfiguration<AutoSaveConfiguration>
{
    public void Configure(EntityTypeBuilder<AutoSaveConfiguration> builder)
    {
        builder.ToTable("AutoSaveConfigurations");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.GameId)
            .IsRequired();

        builder.Property(c => c.NamingPattern)
            .IsRequired()
            .HasMaxLength(200);

        // JSON serialization for tags
        builder.Property(c => c.Tags)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());

        // Indexes
        builder.HasIndex(c => c.GameId)
            .IsUnique()
            .HasDatabaseName("IX_AutoSaveConfigurations_GameId");
    }
}
