using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.RomManagement.ValueObjects;

namespace SaveState.Infrastructure.Persistence.Configurations;

public class BackupConfiguration : IEntityTypeConfiguration<Backup>
{
    public void Configure(EntityTypeBuilder<Backup> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(b => b.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(b => b.FilePath)
            .IsRequired()
            .HasConversion(
                v => v.Value,
                v => new FilePath(v))
            .HasMaxLength(500);

        builder.Property(b => b.FileSize)
            .IsRequired();

        builder.Property(b => b.Type)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(b => b.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(b => b.Game)
            .WithMany()
            .HasForeignKey(b => b.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(b => b.GameId);
        builder.HasIndex(b => b.Type);
        builder.HasIndex(b => b.CreatedAt);
    }
}
