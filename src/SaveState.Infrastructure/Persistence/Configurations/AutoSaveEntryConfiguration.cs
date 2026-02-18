using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.AutoSave;

namespace SaveState.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for AutoSaveEntry.
/// </summary>
public class AutoSaveEntryConfiguration : IEntityTypeConfiguration<AutoSaveEntry>
{
    public void Configure(EntityTypeBuilder<AutoSaveEntry> builder)
    {
        builder.ToTable("AutoSaveEntries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.GameId)
            .IsRequired();

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.FilePath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.TriggerType)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(e => e.Level)
            .HasMaxLength(100);

        builder.Property(e => e.Checkpoint)
            .HasMaxLength(100);

        builder.Property(e => e.ThumbnailPath)
            .HasMaxLength(500);

        // JSON serialization for complex types
        builder.Property(e => e.Tags)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());

        builder.Property(e => e.Metadata)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, string>());

        // Indexes
        builder.HasIndex(e => e.GameId)
            .HasDatabaseName("IX_AutoSaveEntries_GameId");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_AutoSaveEntries_CreatedAt");

        builder.HasIndex(e => new { e.GameId, e.TriggerType })
            .HasDatabaseName("IX_AutoSaveEntries_GameId_TriggerType");
    }
}
