using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.RomManagement.RomValidation;

namespace SaveState.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for RomValidationReport entity.
/// </summary>
public class RomValidationReportConfiguration : IEntityTypeConfiguration<RomValidationReport>
{
    public void Configure(EntityTypeBuilder<RomValidationReport> builder)
    {
        builder.ToTable("RomValidationReports");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.RomFileId)
            .IsRequired();

        builder.Property(r => r.Status)
            .IsRequired();

        builder.Property(r => r.ValidatedAt)
            .IsRequired();

        builder.Property(r => r.SuggestedName)
            .HasMaxLength(500);

        // Configure relationship to RomHashInfo (separate entity)
        builder.HasOne(r => r.HashInfo)
            .WithMany()
            .HasForeignKey(r => r.HashInfoId)
            .IsRequired(false);

        // MatchResult is currently transient analysis output and not persisted in EF.
        builder.Ignore(r => r.MatchResult);

        // Index for efficient lookup by RomFileId
        builder.HasIndex(r => r.RomFileId)
            .HasDatabaseName("IX_RomValidationReports_RomFileId");

        // Index for filtering by status
        builder.HasIndex(r => r.Status)
            .HasDatabaseName("IX_RomValidationReports_Status");

        // Composite index for common query patterns
        builder.HasIndex(r => new { r.RomFileId, r.Status })
            .HasDatabaseName("IX_RomValidationReports_RomFileId_Status");

        builder.HasIndex(r => r.ValidatedAt)
            .HasDatabaseName("IX_RomValidationReports_ValidatedAt");
    }
}
