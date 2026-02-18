using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.Mugen.ComboDatabase;

namespace SaveState.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for ComboSubmission.
/// </summary>
public class ComboSubmissionConfiguration : IEntityTypeConfiguration<ComboSubmission>
{
    public void Configure(EntityTypeBuilder<ComboSubmission> builder)
    {
        builder.ToTable("ComboSubmissions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.ComboId)
            .IsRequired();

        builder.Property(s => s.SubmitterName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.SubmitterId)
            .HasMaxLength(100);

        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(s => s.ReviewerNotes)
            .HasMaxLength(1000);

        builder.Property(s => s.ReviewedBy)
            .HasMaxLength(100);

        // JSON serialization for verification videos
        builder.Property(s => s.VerificationVideos)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());

        // Indexes
        builder.HasIndex(s => s.ComboId)
            .HasDatabaseName("IX_ComboSubmissions_ComboId");

        builder.HasIndex(s => s.Status)
            .HasDatabaseName("IX_ComboSubmissions_Status");

        builder.HasIndex(s => s.SubmittedAt)
            .HasDatabaseName("IX_ComboSubmissions_SubmittedAt");
    }
}
