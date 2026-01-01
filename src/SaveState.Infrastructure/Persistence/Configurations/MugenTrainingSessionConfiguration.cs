namespace SaveState.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.Mugen.Entities;

/// <summary>
/// Entity Framework configuration for MugenTrainingSession.
/// </summary>
public class MugenTrainingSessionConfiguration : IEntityTypeConfiguration<MugenTrainingSession>
{
    public void Configure(EntityTypeBuilder<MugenTrainingSession> builder)
    {
        builder.ToTable("MugenTrainingSessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.CharacterId)
            .IsRequired();

        builder.Property(s => s.OpponentCharacterId)
            .IsRequired();

        builder.Property(s => s.UserId)
            .IsRequired();

        builder.Property(s => s.SessionType)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(s => s.StartedAt)
            .IsRequired();

        builder.Property(s => s.EndedAt)
            .IsRequired(false);

        builder.Property(s => s.RoundsPracticed)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(s => s.SuccessfulCombos)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(s => s.FailedCombos)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(s => s.Notes)
            .HasMaxLength(1000);

        // Configure navigation properties
        builder.HasOne(s => s.Character)
            .WithMany() // MugenCharacter doesn't have navigation back
            .HasForeignKey(s => s.CharacterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.OpponentCharacter)
            .WithMany() // MugenCharacter doesn't have navigation back
            .HasForeignKey(s => s.OpponentCharacterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Recordings)
            .WithOne(r => r.TrainingSession)
            .HasForeignKey(r => r.TrainingSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.CharacterId);
        builder.HasIndex(s => s.OpponentCharacterId);
        builder.HasIndex(s => s.SessionType);
        builder.HasIndex(s => s.StartedAt);
        builder.HasIndex(s => new { s.UserId, s.EndedAt }).HasFilter("[EndedAt] IS NULL"); // Active sessions
    }
}