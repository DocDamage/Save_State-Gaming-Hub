using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Infrastructure.Persistence.Configurations;

public class AchievementConfiguration : IEntityTypeConfiguration<Achievement>
{
    public void Configure(EntityTypeBuilder<Achievement> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.IconPath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.Points)
            .IsRequired();

        builder.Property(a => a.Type)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(a => a.TargetValue)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(a => a.Criteria)
            .HasMaxLength(1000);

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.GameId)
            .IsRequired(false);

        // Indexes
        builder.HasIndex(a => a.GameId);
        builder.HasIndex(a => a.Name).IsUnique();
        builder.HasIndex(a => a.Type);
        builder.HasIndex(a => a.IsActive);
        builder.HasIndex(a => a.Points);
    }
}
