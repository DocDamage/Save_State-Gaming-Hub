using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.OpenMK.Entities;

namespace SaveState.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for OpenMK match state persistence.
/// </summary>
public class OpenMKMatchStateConfiguration : IEntityTypeConfiguration<OpenMKMatchState>
{
    public void Configure(EntityTypeBuilder<OpenMKMatchState> builder)
    {
        builder.ToTable("OpenMKMatchStates");

        builder.HasKey(ms => ms.MatchId);

        builder.Property(ms => ms.Player1CostumeName)
            .HasMaxLength(200);

        builder.Property(ms => ms.Player2CostumeName)
            .HasMaxLength(200);

        builder.HasIndex(ms => ms.Player1CharacterId)
            .HasDatabaseName("IX_OpenMKMatchStates_Player1CharacterId");

        builder.HasIndex(ms => ms.Player2CharacterId)
            .HasDatabaseName("IX_OpenMKMatchStates_Player2CharacterId");
    }
}
