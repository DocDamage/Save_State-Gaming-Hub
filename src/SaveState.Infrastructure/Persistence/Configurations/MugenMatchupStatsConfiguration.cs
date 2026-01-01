namespace SaveState.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.Mugen.Entities;

/// <summary>
/// Entity Framework configuration for MugenMatchupStats.
/// </summary>
public class MugenMatchupStatsConfiguration : IEntityTypeConfiguration<MugenMatchupStats>
{
    public void Configure(EntityTypeBuilder<MugenMatchupStats> builder)
    {
        builder.ToTable("MugenMatchupStats");

        builder.HasKey(s => s.Id);

        // Composite key to ensure uniqueness between character pairs
        builder.HasAlternateKey(s => new { s.Character1Id, s.Character2Id });

        builder.Property(s => s.Character1Id)
            .IsRequired();

        builder.Property(s => s.Character2Id)
            .IsRequired();

        builder.Property(s => s.TotalMatches)
            .IsRequired();

        builder.Property(s => s.Character1Wins)
            .IsRequired();

        builder.Property(s => s.Character2Wins)
            .IsRequired();

        builder.Property(s => s.Draws)
            .IsRequired();

        builder.Property(s => s.AverageMatchDuration)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(s => s.LastUpdated)
            .IsRequired();

        // Configure navigation properties
        builder.HasOne(s => s.Character1)
            .WithMany() // MugenCharacter doesn't have navigation back
            .HasForeignKey(s => s.Character1Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Character2)
            .WithMany() // MugenCharacter doesn't have navigation back
            .HasForeignKey(s => s.Character2Id)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for performance
        builder.HasIndex(s => s.Character1Id);
        builder.HasIndex(s => s.Character2Id);
        builder.HasIndex(s => new { s.Character1Id, s.Character2Id });
        builder.HasIndex(s => s.LastUpdated);
    }
}