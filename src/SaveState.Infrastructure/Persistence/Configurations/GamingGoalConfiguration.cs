using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.Analytics.Entities;

namespace SaveState.Infrastructure.Persistence.Configurations;

public class GamingGoalConfiguration : IEntityTypeConfiguration<GamingGoal>
{
    public void Configure(EntityTypeBuilder<GamingGoal> builder)
    {
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(g => g.Type)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(g => g.TargetValue)
            .IsRequired();

        builder.Property(g => g.CurrentValue)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(g => g.StartDate)
            .IsRequired();

        builder.Property(g => g.EndDate);

        builder.Property(g => g.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasDefaultValue(GoalStatus.Active);

        builder.Property(g => g.SpecificGameId);

        // Indexes
        builder.HasIndex(g => g.Status);
        builder.HasIndex(g => g.Type);
        builder.HasIndex(g => g.StartDate);
        builder.HasIndex(g => g.EndDate);
        builder.HasIndex(g => new { g.Status, g.Type });
    }
}