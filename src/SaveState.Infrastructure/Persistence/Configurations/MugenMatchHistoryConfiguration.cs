namespace SaveState.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.Mugen.Entities;

/// <summary>
/// Entity Framework configuration for MugenMatchHistory.
/// </summary>
public class MugenMatchHistoryConfiguration : IEntityTypeConfiguration<MugenMatchHistory>
{
    public void Configure(EntityTypeBuilder<MugenMatchHistory> builder)
    {
        builder.ToTable("MugenMatchHistories");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Player1CharacterId)
            .IsRequired();

        builder.Property(m => m.Player2CharacterId)
            .IsRequired();

        builder.Property(m => m.Result)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(m => m.RoundsWonP1)
            .IsRequired();

        builder.Property(m => m.RoundsWonP2)
            .IsRequired();

        builder.Property(m => m.MatchDuration)
            .IsRequired();

        builder.Property(m => m.PlayedAt)
            .IsRequired();

        builder.Property(m => m.Mode)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(m => m.ReplayPath)
            .HasMaxLength(500);

        // Indexes for performance
        builder.HasIndex(m => m.Player1CharacterId);
        builder.HasIndex(m => m.Player2CharacterId);
        builder.HasIndex(m => m.PlayedAt);
        builder.HasIndex(m => new { m.Player1CharacterId, m.Player2CharacterId });
    }
}