using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.Social.Entities;

namespace SaveState.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuration for the Friend entity.
/// </summary>
public class FriendConfiguration : IEntityTypeConfiguration<Friend>
{
    public void Configure(EntityTypeBuilder<Friend> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(f => f.AvatarUrl)
            .HasMaxLength(500);

        builder.Property(f => f.Platform)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(f => f.PlatformUserId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(f => f.IsOnline)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(f => f.CurrentGame)
            .HasMaxLength(200);

        builder.Property(f => f.LastSeenAt);

        builder.Property(f => f.UpdatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(f => new { f.Platform, f.PlatformUserId })
            .IsUnique();

        builder.HasIndex(f => f.IsOnline);
        builder.HasIndex(f => f.Platform);
        builder.HasIndex(f => f.UpdatedAt);
    }
}

/// <summary>
/// Configuration for the FriendActivity entity.
/// </summary>
public class FriendActivityConfiguration : IEntityTypeConfiguration<FriendActivity>
{
    public void Configure(EntityTypeBuilder<FriendActivity> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Type)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(a => a.GameTitle)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Details)
            .HasMaxLength(500);

        builder.Property(a => a.Timestamp)
            .IsRequired();

        builder.Property(a => a.Platform)
            .IsRequired()
            .HasConversion<string>();

        // Relationships
        builder.HasOne(a => a.Friend)
            .WithMany()
            .HasForeignKey(a => a.FriendId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(a => a.FriendId);
        builder.HasIndex(a => a.Type);
        builder.HasIndex(a => a.Platform);
        builder.HasIndex(a => a.Timestamp);
        builder.HasIndex(a => new { a.FriendId, a.Timestamp });
    }
}