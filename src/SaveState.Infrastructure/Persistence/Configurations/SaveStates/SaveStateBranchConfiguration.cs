using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.SaveStates.Entities;
using SaveState.Core.GameLibrary.Entities;
using SaveStateEntity = SaveState.Core.SaveStates.Entities.SaveState;

namespace SaveState.Infrastructure.Persistence.Configurations.SaveStates;

public class SaveStateBranchConfiguration : IEntityTypeConfiguration<SaveStateBranch>
{
    public void Configure(EntityTypeBuilder<SaveStateBranch> builder)
    {
        builder.HasKey(sb => sb.Id);

        builder.Property(sb => sb.RootStateId)
            .IsRequired();

        builder.Property(sb => sb.BranchName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(sb => sb.Description)
            .HasMaxLength(500);

        builder.Property(sb => sb.Type)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(sb => sb.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne<SaveStateEntity>()
            .WithMany()
            .HasForeignKey(sb => sb.RootStateId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(sb => sb.RootStateId);
        builder.HasIndex(sb => sb.CreatedAt);
        builder.HasIndex(sb => new { sb.BranchName, sb.RootStateId }).IsUnique();
        builder.HasIndex(sb => sb.Type);
    }
}