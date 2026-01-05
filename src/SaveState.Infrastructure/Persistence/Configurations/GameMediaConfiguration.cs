using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Infrastructure.Persistence.Configurations;

public class GameMediaConfiguration : IEntityTypeConfiguration<GameMedia>
{
    public void Configure(EntityTypeBuilder<GameMedia> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.FilePath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(m => m.FileFormat)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(m => m.Title)
            .HasMaxLength(200);

        builder.Property(m => m.MediaType)
            .IsRequired()
            .HasConversion<string>();

        // Value Object Conversions
        builder.Property(m => m.GameId)
            .HasConversion(
                id => id.Value,
                value => GameId.From(value))
            .IsRequired();

         builder.Property(m => m.UserId)
            .HasConversion(
                id => id.Value,
                value => UserId.From(value))
            .IsRequired();

        // Convert List<string> Tags to JSON string
        builder.Property(m => m.Tags)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());

        // Indexes
        builder.HasIndex(m => m.GameId);
        builder.HasIndex(m => m.UserId);
        builder.HasIndex(m => m.MediaType);
        builder.HasIndex(m => m.IsFavorite);
        builder.HasIndex(m => m.IsPublic);
    }
}
