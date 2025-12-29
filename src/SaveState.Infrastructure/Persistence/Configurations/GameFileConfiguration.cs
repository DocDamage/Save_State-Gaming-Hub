using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Infrastructure.Persistence.Configurations;

public class GameFileConfiguration : IEntityTypeConfiguration<GameFile>
{
    public void Configure(EntityTypeBuilder<GameFile> builder)
    {
        builder.HasKey(gf => gf.Id);

        builder.Property(gf => gf.Path)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(gf => gf.FileName)
            .HasMaxLength(255);

        builder.Property(gf => gf.FileSize);

        builder.Property(gf => gf.AddedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(gf => gf.Game)
            .WithMany(g => g.Files)
            .HasForeignKey(gf => gf.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(gf => gf.GameId);
        builder.HasIndex(gf => gf.Path);
    }
}
