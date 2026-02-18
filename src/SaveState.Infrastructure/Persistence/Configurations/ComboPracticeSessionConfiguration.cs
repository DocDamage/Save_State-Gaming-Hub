using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.Mugen.ComboDatabase;

namespace SaveState.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for ComboPracticeSession.
/// </summary>
public class ComboPracticeSessionConfiguration : IEntityTypeConfiguration<ComboPracticeSession>
{
    public void Configure(EntityTypeBuilder<ComboPracticeSession> builder)
    {
        builder.ToTable("ComboPracticeSessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.ComboId)
            .IsRequired();

        builder.Property(s => s.Attempts)
            .IsRequired();

        builder.Property(s => s.Successes)
            .IsRequired();

        builder.Property(s => s.TotalPracticeTime)
            .IsRequired();

        // JSON serialization for attempts log
        builder.Property(s => s.AttemptsLog)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<PracticeAttempt>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<PracticeAttempt>());

        // Indexes
        builder.HasIndex(s => s.ComboId)
            .HasDatabaseName("IX_ComboPracticeSessions_ComboId");

        builder.HasIndex(s => s.StartedAt)
            .HasDatabaseName("IX_ComboPracticeSessions_StartedAt");
    }
}
