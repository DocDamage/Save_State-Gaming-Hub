namespace SaveState.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.Mugen.Entities;

/// <summary>
/// Entity Framework configuration for TournamentParticipant.
/// </summary>
public class TournamentParticipantConfiguration : IEntityTypeConfiguration<TournamentParticipant>
{
    public void Configure(EntityTypeBuilder<TournamentParticipant> builder)
    {
        builder.ToTable("TournamentParticipants");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.TournamentId)
            .IsRequired();

        builder.Property(p => p.CharacterId)
            .IsRequired();

        builder.Property(p => p.Seed)
            .IsRequired();

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(p => p.Score)
            .IsRequired();

        builder.Property(p => p.EliminatedAt)
            .IsRequired(false);

        // Configure navigation properties
        builder.HasOne(p => p.Tournament)
            .WithMany(t => t.Participants)
            .HasForeignKey(p => p.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Character)
            .WithMany() // MugenCharacter doesn't have a navigation property back
            .HasForeignKey(p => p.CharacterId)
            .OnDelete(DeleteBehavior.Restrict);


        // Indexes for performance
        builder.HasIndex(p => p.TournamentId);
        builder.HasIndex(p => p.CharacterId);
        builder.HasIndex(p => new { p.TournamentId, p.Status });
    }
}