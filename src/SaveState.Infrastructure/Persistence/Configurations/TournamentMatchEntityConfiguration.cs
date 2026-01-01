namespace SaveState.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.Mugen.Entities;

/// <summary>
/// Entity Framework configuration for TournamentMatchEntity.
/// </summary>
public class TournamentMatchEntityConfiguration : IEntityTypeConfiguration<TournamentMatchEntity>
{
    public void Configure(EntityTypeBuilder<TournamentMatchEntity> builder)
    {
        builder.ToTable("TournamentMatches");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.TournamentId)
            .IsRequired();

        builder.Property(m => m.Round)
            .IsRequired();

        builder.Property(m => m.MatchNumber)
            .IsRequired();

        builder.Property(m => m.Player1CharacterId)
            .IsRequired(false);

        builder.Property(m => m.Player2CharacterId)
            .IsRequired(false);

        builder.Property(m => m.WinnerId)
            .IsRequired(false);

        builder.Property(m => m.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(m => m.CompletedAt)
            .IsRequired(false);

        builder.Property(m => m.Notes)
            .HasMaxLength(1000);

        // Configure navigation properties
        builder.HasOne(m => m.Tournament)
            .WithMany(t => t.Matches)
            .HasForeignKey(m => m.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.HasIndex(m => m.TournamentId);
        builder.HasIndex(m => m.Player1CharacterId).HasFilter("[Player1CharacterId] IS NOT NULL");
        builder.HasIndex(m => m.Player2CharacterId).HasFilter("[Player2CharacterId] IS NOT NULL");
        builder.HasIndex(m => m.WinnerId).HasFilter("[WinnerId] IS NOT NULL");
        builder.HasIndex(m => new { m.TournamentId, m.Status });
    }
}