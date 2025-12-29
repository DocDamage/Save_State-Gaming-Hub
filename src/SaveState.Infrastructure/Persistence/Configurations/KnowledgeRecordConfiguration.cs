using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.Ai.Knowledge;

namespace SaveState.Infrastructure.Persistence.Configurations;

public class KnowledgeRecordConfiguration : IEntityTypeConfiguration<KnowledgeRecord>
{
    public void Configure(EntityTypeBuilder<KnowledgeRecord> builder)
    {
        builder.HasKey(k => k.Id);

        builder.Property(k => k.Id)
            .IsRequired()
            .HasMaxLength(200);

        // Store embedding as binary data
        builder.Property(k => k.Embedding)
            .IsRequired()
            .HasConversion(
                v => SerializeFloatArray(v),
                v => DeserializeFloatArray(v));

        builder.Property(k => k.Content)
            .IsRequired()
            .HasMaxLength(8000); // Limit content size

        builder.Property(k => k.Metadata)
            .HasMaxLength(2000);

        builder.Property(k => k.IndexedAt)
            .IsRequired();

        builder.Property(k => k.AccessCount)
            .HasDefaultValue(0);

        builder.Property(k => k.RelevanceScore)
            .HasDefaultValue(1.0f)
            .HasPrecision(4, 3);

        // Indexes for performance
        builder.HasIndex(k => k.IndexedAt);
        builder.HasIndex(k => k.LastAccessedAt);
        builder.HasIndex(k => k.RelevanceScore);
        builder.HasIndex(k => k.AccessCount);
    }

    private static byte[] SerializeFloatArray(float[] floats)
    {
        var bytes = new byte[floats.Length * sizeof(float)];
        Buffer.BlockCopy(floats, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    private static float[] DeserializeFloatArray(byte[] bytes)
    {
        var floats = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
        return floats;
    }
}
