using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.Mugen.ComboDatabase;

namespace SaveState.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for ComboEntry.
/// </summary>
public class ComboEntryConfiguration : IEntityTypeConfiguration<ComboEntry>
{
    public void Configure(EntityTypeBuilder<ComboEntry> builder)
    {
        builder.ToTable("ComboEntries");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CharacterName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Description)
            .HasMaxLength(1000);

        builder.Property(c => c.StartingPosition)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.EndingPosition)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.InputNotation)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(c => c.VideoUrl)
            .HasMaxLength(500);

        builder.Property(c => c.ImagePath)
            .HasMaxLength(500);

        builder.Property(c => c.Creator)
            .HasMaxLength(100);

        builder.Property(c => c.Source)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.GameVersion)
            .HasMaxLength(50);

        // JSON serialization for complex types
        builder.Property(c => c.Tags)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());

        builder.Property(c => c.Moves)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<ComboMoveEntry>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<ComboMoveEntry>());

        builder.Property(c => c.FrameData)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<ComboFrameData>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new ComboFrameData());

        builder.Property(c => c.Timing)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<ComboTiming>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new ComboTiming());

        builder.Property(c => c.UsageStats)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<ComboUsageStats>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new ComboUsageStats());

        builder.Property(c => c.Ratings)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<ComboRatings>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new ComboRatings());

        builder.Property(c => c.RelatedComboIds)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<Guid>());

        builder.Property(c => c.Prerequisites)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());

        builder.Property(c => c.Tips)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());

        builder.Property(c => c.OkizemeOptions)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());

        builder.Property(c => c.CharacterExceptions)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());

        // Indexes
        builder.HasIndex(c => c.CharacterName)
            .HasDatabaseName("IX_ComboEntries_CharacterName");

        builder.HasIndex(c => new { c.CharacterName, c.Difficulty })
            .HasDatabaseName("IX_ComboEntries_CharacterName_Difficulty");

        builder.HasIndex(c => new { c.CharacterName, c.IsOptimal })
            .HasDatabaseName("IX_ComboEntries_CharacterName_IsOptimal");

        builder.HasIndex(c => c.IsTouchOfDeath)
            .HasDatabaseName("IX_ComboEntries_IsTouchOfDeath");

        builder.HasIndex(c => c.CreatedAt)
            .HasDatabaseName("IX_ComboEntries_CreatedAt");
    }
}
