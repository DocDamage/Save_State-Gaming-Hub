using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Infrastructure.Persistence.Configurations;

public class GameModConfiguration : IEntityTypeConfiguration<GameMod>
{
    public void Configure(EntityTypeBuilder<GameMod> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.InstallPath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(m => m.Version)
            .HasMaxLength(50);

        builder.Property(m => m.Author)
            .HasMaxLength(100);

        builder.Property(m => m.Category)
            .HasMaxLength(50);

        // Value Object Conversions
        builder.Property(m => m.GameId)
            .HasConversion(
                id => id.Value,
                value => GameId.From(value))
            .IsRequired();

        // Convert List<string> Tags to JSON string
        builder.Property(m => m.Tags)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());

        // Indexes
        builder.HasIndex(m => m.GameId);
        builder.HasIndex(m => m.IsEnabled);
        builder.HasIndex(m => m.LoadOrder);
    }
}
