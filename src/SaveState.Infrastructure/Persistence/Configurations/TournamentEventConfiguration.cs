using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.Mugen.TournamentEvents;

namespace SaveState.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for TournamentEvent.
/// </summary>
public class TournamentEventConfiguration : IEntityTypeConfiguration<TournamentEvent>
{
    public void Configure(EntityTypeBuilder<TournamentEvent> builder)
    {
        builder.ToTable("TournamentEvents");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .HasMaxLength(2000);

        builder.Property(t => t.Format)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(t => t.Organizer)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.StreamUrl)
            .HasMaxLength(500);

        builder.Property(t => t.DiscordWebhook)
            .HasMaxLength(500);

        // JSON serialization for complex types
        builder.Property(t => t.Tags)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());

        builder.Property(t => t.Participants)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<TournamentParticipant>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<TournamentParticipant>());

        builder.Property(t => t.Matches)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<TournamentMatch>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<TournamentMatch>());

        builder.Property(t => t.Rounds)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<TournamentRound>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<TournamentRound>());

        builder.Property(t => t.Rules)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<TournamentRules>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new TournamentRules());

        builder.Property(t => t.Settings)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<TournamentSettings>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new TournamentSettings());

        builder.Property(t => t.PrizePool)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<PrizePool>(v, (System.Text.Json.JsonSerializerOptions?)null));

        builder.Property(t => t.Statistics)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<TournamentStatistics>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new TournamentStatistics());

        // Indexes
        builder.HasIndex(t => t.Status)
            .HasDatabaseName("IX_MugenTournaments_Status");

        builder.HasIndex(t => t.Format)
            .HasDatabaseName("IX_MugenTournaments_Format");

        builder.HasIndex(t => t.Organizer)
            .HasDatabaseName("IX_MugenTournaments_Organizer");

        builder.HasIndex(t => t.ScheduledStart)
            .HasDatabaseName("IX_MugenTournaments_ScheduledStart");

        builder.HasIndex(t => new { t.IsPublic, t.Status })
            .HasDatabaseName("IX_MugenTournaments_IsPublic_Status");
    }
}
