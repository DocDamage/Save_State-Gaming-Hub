using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Infrastructure.Persistence.Configurations;

public class GameNoteConfiguration : IEntityTypeConfiguration<GameNote>
{
    public void Configure(EntityTypeBuilder<GameNote> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(n => n.Content)
            .IsRequired(); // SQLite allows max length implicitly for TEXT

        builder.Property(n => n.Category)
            .HasMaxLength(50);

        // Value Object Conversions
        builder.Property(n => n.GameId)
            .HasConversion(
                id => id.Value,
                value => GameId.From(value))
            .IsRequired();

        builder.Property(n => n.UserId)
            .HasConversion(
                id => id.Value,
                value => UserId.From(value))
            .IsRequired();

        // Convert List<string> Tags to JSON string
        builder.Property(n => n.Tags)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());

        // Indexes
        builder.HasIndex(n => n.GameId);
        builder.HasIndex(n => n.UserId);
        builder.HasIndex(n => n.IsPinned);
    }
}
