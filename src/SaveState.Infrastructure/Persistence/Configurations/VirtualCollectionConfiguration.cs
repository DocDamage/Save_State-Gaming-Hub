using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Infrastructure.Persistence.Configurations;

public class VirtualCollectionConfiguration : IEntityTypeConfiguration<VirtualCollection>
{
    public void Configure(EntityTypeBuilder<VirtualCollection> builder)
    {
        builder.HasKey(vc => vc.Id);

        builder.Property(vc => vc.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(vc => vc.Icon)
            .HasMaxLength(50);

        builder.Property(vc => vc.Type)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(vc => vc.FilterExpression)
            .HasMaxLength(2000);

        builder.Property(vc => vc.SortOrder)
            .HasDefaultValue(0);

        builder.Property(vc => vc.IsSystemCollection)
            .HasDefaultValue(false);

        builder.HasMany(vc => vc.Games)
            .WithOne(vcg => vcg.Collection)
            .HasForeignKey(vcg => vcg.CollectionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(vc => vc.Type);
        builder.HasIndex(vc => vc.IsSystemCollection);
        builder.HasIndex(vc => new { vc.IsSystemCollection, vc.SortOrder, vc.Name });
    }
}

public class VirtualCollectionGameConfiguration : IEntityTypeConfiguration<VirtualCollectionGame>
{
    public void Configure(EntityTypeBuilder<VirtualCollectionGame> builder)
    {
        builder.HasKey(vcg => new { vcg.CollectionId, vcg.GameId });

        builder.Property(vcg => vcg.SortOrder)
            .HasDefaultValue(0);

        builder.Property(vcg => vcg.AddedAt)
            .IsRequired();

        builder.HasOne(vcg => vcg.Collection)
            .WithMany(vc => vc.Games)
            .HasForeignKey(vcg => vcg.CollectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(vcg => vcg.Game)
            .WithMany()
            .HasForeignKey(vcg => vcg.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(vcg => vcg.GameId);
        builder.HasIndex(vcg => new { vcg.CollectionId, vcg.SortOrder });
    }
}