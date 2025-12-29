using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.AiGaming.Entities;

namespace SaveState.Infrastructure.Persistence.Configurations;

public class AiModelConfiguration : IEntityTypeConfiguration<AiModel>
{
    public void Configure(EntityTypeBuilder<AiModel> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Provider)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.ModelId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Description)
            .HasMaxLength(500);

        builder.Property(a => a.MaxTokens)
            .IsRequired();

        builder.Property(a => a.Temperature)
            .IsRequired()
            .HasPrecision(3, 2);

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(a => a.Provider);
        builder.HasIndex(a => a.ModelId);
        builder.HasIndex(a => a.IsActive);
        builder.HasIndex(a => a.LastUsedAt);
    }
}
