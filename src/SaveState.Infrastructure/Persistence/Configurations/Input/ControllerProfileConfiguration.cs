using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.Input.Entities;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Infrastructure.Persistence.Configurations.Input;

public class ControllerProfileConfiguration : IEntityTypeConfiguration<ControllerProfile>
{
    public void Configure(EntityTypeBuilder<ControllerProfile> builder)
    {
        builder.HasKey(cp => cp.Id);

        builder.Property(cp => cp.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(cp => cp.GameId);

        builder.Property(cp => cp.ControllerId)
            .HasMaxLength(100);

        builder.Property(cp => cp.Type)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(cp => cp.MappingsJson)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(cp => cp.IsDefault)
            .HasDefaultValue(false);

        builder.Property(cp => cp.CreatedAt)
            .IsRequired();

        builder.Property(cp => cp.LastUsedAt);

        // Relationships
        builder.HasOne<Game>()
            .WithMany()
            .HasForeignKey(cp => cp.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(cp => cp.GameId);
        builder.HasIndex(cp => cp.Type);
        builder.HasIndex(cp => cp.ControllerId);
        builder.HasIndex(cp => new { cp.GameId, cp.IsDefault });
        builder.HasIndex(cp => new { cp.Type, cp.LastUsedAt });
    }
}