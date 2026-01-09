using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.Api.Entities;

namespace SaveState.Infrastructure.Persistence.Configurations;

public class ApiKeyConfiguration : IEntityTypeConfiguration<Core.Api.Entities.ApiKey>
{
    public void Configure(EntityTypeBuilder<Core.Api.Entities.ApiKey> builder)
    {
        builder.ToTable("ExternalApiKeys");

        builder.HasKey(ak => ak.Id);

        builder.Property(ak => ak.Key)
            .IsRequired()
            .HasMaxLength(256); // URL-safe base64 encoded key

        builder.Property(ak => ak.AppName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(ak => ak.Scopes)
            .IsRequired()
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries));

        builder.Property(ak => ak.CreatedAt)
            .IsRequired();

        builder.Property(ak => ak.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Indexes
        builder.HasIndex(ak => ak.Key)
            .IsUnique()
            .HasDatabaseName("IX_ExternalApiKeys_Key");

        builder.HasIndex(ak => ak.AppName)
            .HasDatabaseName("IX_ExternalApiKeys_AppName");

        builder.HasIndex(ak => ak.IsActive)
            .HasDatabaseName("IX_ExternalApiKeys_IsActive");

        builder.HasIndex(ak => ak.LastUsedAt)
            .HasDatabaseName("IX_ExternalApiKeys_LastUsedAt");
    }
}