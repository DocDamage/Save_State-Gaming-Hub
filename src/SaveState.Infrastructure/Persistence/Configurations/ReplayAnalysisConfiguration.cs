using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.Mugen.ReplayAnalysis;

namespace SaveState.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for ReplayAnalysis.
/// </summary>
public class ReplayAnalysisConfiguration : IEntityTypeConfiguration<ReplayAnalysis>
{
    public void Configure(EntityTypeBuilder<ReplayAnalysis> builder)
    {
        builder.ToTable("ReplayAnalyses");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReplayFilePath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.Description)
            .HasMaxLength(1000);

        builder.Property(r => r.Platform)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.Player1Character)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.Player2Character)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.Player1Name)
            .HasMaxLength(100);

        builder.Property(r => r.Player2Name)
            .HasMaxLength(100);

        builder.Property(r => r.FileHash)
            .HasMaxLength(64);

        builder.Property(r => r.AnalysisVersion)
            .IsRequired()
            .HasMaxLength(10);

        // JSON serialization for complex types
        builder.Property(r => r.Tags)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());

        builder.Property(r => r.Combos)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<DetectedCombo>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<DetectedCombo>());

        builder.Property(r => r.Highlights)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<HighlightMoment>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<HighlightMoment>());

        builder.Property(r => r.Comebacks)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<ComebackMoment>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<ComebackMoment>());

        builder.Property(r => r.FrameData)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<FrameSnapshot>>(v, (System.Text.Json.JsonSerializerOptions?)null));

        builder.Property(r => r.Player1Stats)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<PlayerCombatStats>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new PlayerCombatStats());

        builder.Property(r => r.Player2Stats)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<PlayerCombatStats>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new PlayerCombatStats());

        // Indexes
        builder.HasIndex(r => r.FileHash)
            .IsUnique()
            .HasDatabaseName("IX_ReplayAnalyses_FileHash");

        builder.HasIndex(r => r.Player1Character)
            .HasDatabaseName("IX_ReplayAnalyses_Player1Character");

        builder.HasIndex(r => r.Player2Character)
            .HasDatabaseName("IX_ReplayAnalyses_Player2Character");

        builder.HasIndex(r => r.AnalyzedAt)
            .HasDatabaseName("IX_ReplayAnalyses_AnalyzedAt");

        builder.HasIndex(r => r.ReplayDate)
            .HasDatabaseName("IX_ReplayAnalyses_ReplayDate");

        builder.HasIndex(r => new { r.Player1Character, r.Player2Character })
            .HasDatabaseName("IX_ReplayAnalyses_CharacterMatchup");
    }
}
