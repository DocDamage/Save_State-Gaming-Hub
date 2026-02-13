using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.Performance.Entities;
using SaveState.Core.Performance.ValueObjects;
using System.Text.Json;

namespace SaveState.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for MemoryWatch entity.
/// </summary>
public sealed class MemoryWatchConfiguration : IEntityTypeConfiguration<MemoryWatch>
{
    public void Configure(EntityTypeBuilder<MemoryWatch> builder)
    {
        builder.ToTable("MemoryWatches");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.GameId)
            .IsRequired();

        builder.Property(w => w.Label)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(w => w.Description)
            .HasMaxLength(500);

        builder.Property(w => w.CurrentValue)
            .HasMaxLength(1000);

        builder.Property(w => w.PreviousValue)
            .HasMaxLength(1000);

        builder.Property(w => w.DataType)
            .IsRequired()
            .HasConversion<string>();

        // Store MemoryAddress as JSON
        builder.Property(w => w.Address)
            .IsRequired()
            .HasConversion(
                v => JsonSerializer.Serialize(new
                {
                    v.BaseAddress,
                    v.Offsets
                }, (JsonSerializerOptions?)null),
                v => DeserializeAddress(v))
            .HasColumnType("TEXT");

        builder.Property(w => w.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(w => w.IsFrozen)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(w => w.ChangeCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(w => w.CreatedAt)
            .IsRequired();

        builder.Property(w => w.UpdatedAt)
            .IsRequired();

        builder.HasIndex(w => w.GameId);
        builder.HasIndex(w => new { w.GameId, w.IsActive });
    }

    private static MemoryAddress DeserializeAddress(string json)
    {
        var data = JsonSerializer.Deserialize<AddressData>(json);
        if (data == null)
        {
            return MemoryAddress.Create(0);
        }

        return data.Offsets.Length > 0
            ? MemoryAddress.CreatePointerChain(data.BaseAddress, data.Offsets)
            : MemoryAddress.Create(data.BaseAddress);
    }

    private sealed class AddressData
    {
        public long BaseAddress { get; set; }
        public int[] Offsets { get; set; } = Array.Empty<int>();
    }
}
