using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.Social.Entities;

namespace SaveState.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuration for the GameReview entity.
/// </summary>
public class GameReviewConfiguration : IEntityTypeConfiguration<GameReview>
{
    public void Configure(EntityTypeBuilder<GameReview> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Rating)
            .IsRequired()
            .HasAnnotation("MinValue", 1)
            .HasAnnotation("MaxValue", 10);

        builder.Property(r => r.Title)
            .HasMaxLength(200);

        builder.Property(r => r.Content)
            .HasMaxLength(2000);

        builder.Property(r => r.IsRecommended)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.Property(r => r.UpdatedAt);

        builder.Property(r => r.PlaytimeAtReview)
            .IsRequired()
            .HasConversion(
                v => v.Ticks,
                v => TimeSpan.FromTicks(v));

        builder.Property(r => r.ContainsSpoilers)
            .IsRequired()
            .HasDefaultValue(false);

        // Relationships
        builder.HasOne(r => r.Game)
            .WithMany()
            .HasForeignKey(r => r.GameId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // Constraints
        builder.HasCheckConstraint("CK_GameReview_Rating", "[Rating] >= 1 AND [Rating] <= 10");

        // Indexes
        builder.HasIndex(r => r.GameId)
            .IsUnique(); // One review per game

        builder.HasIndex(r => r.Rating);
        builder.HasIndex(r => r.IsRecommended);
        builder.HasIndex(r => r.CreatedAt);
        builder.HasIndex(r => r.ContainsSpoilers);
    }
}