using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.AiGaming.Entities;

namespace SaveState.Infrastructure.Persistence.Configurations;

public class MemorySnapshotConfiguration : IEntityTypeConfiguration<MemorySnapshot>
{
    public void Configure(EntityTypeBuilder<MemorySnapshot> builder)
    {
        builder.HasKey(ms => ms.Id);

        builder.Property(ms => ms.Address)
            .IsRequired();

        builder.Property(ms => ms.Data)
            .IsRequired();

        builder.Property(ms => ms.CapturedAt)
            .IsRequired();

        builder.Property(ms => ms.ProcessName)
            .HasMaxLength(255);

        builder.Property(ms => ms.ProcessId)
            .IsRequired();

        // Indexes
        builder.HasIndex(ms => ms.Address);
        builder.HasIndex(ms => ms.ProcessId);
        builder.HasIndex(ms => ms.CapturedAt);
    }
}
