using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.InputRecording;
using InputRecordingEntity = SaveState.Core.InputRecording.InputRecording;

namespace SaveState.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for InputRecording entity.
/// </summary>
public class InputRecordingConfiguration : IEntityTypeConfiguration<InputRecordingEntity>
{
    public void Configure(EntityTypeBuilder<InputRecordingEntity> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .HasMaxLength(2000);

        builder.Property(e => e.FilePath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.StartingState)
            .HasMaxLength(500);

        builder.Property(e => e.RomHash)
            .HasMaxLength(100);

        builder.Property(e => e.EmulatorCore)
            .HasMaxLength(100);

        builder.Property(e => e.Region)
            .HasMaxLength(50);

        builder.Property(e => e.Type)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.DeviceType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.Tags)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());

        builder.Property(e => e.Authors)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());

        builder.Property(e => e.Bookmarks)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<RecordingBookmark>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<RecordingBookmark>());

        builder.Property(e => e.Duration)
            .HasConversion(
                v => v.Ticks,
                v => TimeSpan.FromTicks(v));

        builder.Property(e => e.PersonalBestTime)
            .HasConversion(
                v => v.HasValue ? v.Value.Ticks : (long?)null,
                v => v.HasValue ? TimeSpan.FromTicks(v.Value) : null);

        builder.HasIndex(e => e.GameId);
        builder.HasIndex(e => e.Type);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.IsBookmarked);
        builder.HasIndex(e => e.RecordedAt);
        builder.HasIndex(e => e.IsVerifiedTAS);
    }
}
