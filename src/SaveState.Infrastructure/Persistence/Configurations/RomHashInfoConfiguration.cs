using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.RomManagement.RomValidation;

namespace SaveState.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for RomHashInfo entity.
/// </summary>
public class RomHashInfoConfiguration : IEntityTypeConfiguration<RomHashInfo>
{
    public void Configure(EntityTypeBuilder<RomHashInfo> builder)
    {
        builder.ToTable("RomHashInfos");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.RomFileId)
            .IsRequired();

        builder.Property(h => h.Crc32)
            .HasMaxLength(8);

        builder.Property(h => h.Md5)
            .HasMaxLength(32);

        builder.Property(h => h.Sha1)
            .HasMaxLength(40);

        builder.Property(h => h.Sha256)
            .HasMaxLength(64);

        builder.Property(h => h.CalculatedAt)
            .IsRequired();

        builder.Property(h => h.IsComplete)
            .IsRequired();

        // Index for efficient lookup by RomFileId
        builder.HasIndex(h => h.RomFileId)
            .HasDatabaseName("IX_RomHashInfos_RomFileId");

        // Indexes for hash lookups (used in duplicate detection)
        builder.HasIndex(h => h.Crc32)
            .HasDatabaseName("IX_RomHashInfos_Crc32");

        builder.HasIndex(h => h.Md5)
            .HasDatabaseName("IX_RomHashInfos_Md5");

        builder.HasIndex(h => h.Sha1)
            .HasDatabaseName("IX_RomHashInfos_Sha1");

        // Composite index for common query patterns
        builder.HasIndex(h => new { h.RomFileId, h.IsComplete })
            .HasDatabaseName("IX_RomHashInfos_RomFileId_IsComplete");
    }
}
