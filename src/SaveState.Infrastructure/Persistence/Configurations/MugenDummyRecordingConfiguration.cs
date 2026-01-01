namespace SaveState.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.Mugen.Entities;

/// <summary>
/// Entity Framework configuration for MugenDummyRecording.
/// </summary>
public class MugenDummyRecordingConfiguration : IEntityTypeConfiguration<MugenDummyRecording>
{
    public void Configure(EntityTypeBuilder<MugenDummyRecording> builder)
    {
        builder.ToTable("MugenDummyRecordings");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.TrainingSessionId)
            .IsRequired();

        builder.Property(r => r.BehaviorType)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(r => r.ActionSequence)
            .IsRequired(); // Store JSON as text - column type determined by database provider

        builder.Property(r => r.Description)
            .HasMaxLength(500);

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.Property(r => r.Duration)
            .IsRequired();

        builder.Property(r => r.IsSuccessful)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(r => r.ReplayPath)
            .HasMaxLength(500);

        // Configure navigation properties
        builder.HasOne(r => r.TrainingSession)
            .WithMany(s => s.Recordings)
            .HasForeignKey(r => r.TrainingSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.HasIndex(r => r.TrainingSessionId);
        builder.HasIndex(r => r.BehaviorType);
        builder.HasIndex(r => r.CreatedAt);
        builder.HasIndex(r => r.IsSuccessful);
    }
}