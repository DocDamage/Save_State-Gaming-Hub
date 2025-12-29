namespace SaveState.Core.Common.Configuration;

using System.ComponentModel.DataAnnotations;

public class MemoryOptions : IValidatableObject
{
    public const string Section = "Memory";

    public int MaxEntries { get; set; } = 500;
    public int MaxTokens { get; set; } = 50000;
    public int PruneBatchSize { get; set; } = 50;
    public TimeSpan DefaultTtl { get; set; } = TimeSpan.FromHours(1);
    public int MaxConcurrentScans { get; set; } = 3;
    public long MaxMemoryPressureBytes { get; set; } = 100 * 1024 * 1024; // 100MB
    public float MemoryPressureThreshold { get; set; } = 0.8f;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        if (MaxEntries <= 0)
            results.Add(new ValidationResult("Max entries must be positive", new[] { nameof(MaxEntries) }));

        if (MaxTokens <= 0)
            results.Add(new ValidationResult("Max tokens must be positive", new[] { nameof(MaxTokens) }));

        if (DefaultTtl <= TimeSpan.Zero)
            results.Add(new ValidationResult("Default TTL must be positive", new[] { nameof(DefaultTtl) }));

        return results;
    }
}
