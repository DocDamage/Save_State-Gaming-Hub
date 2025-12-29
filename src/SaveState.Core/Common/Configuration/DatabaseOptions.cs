namespace SaveState.Core.Common.Configuration;

using System.ComponentModel.DataAnnotations;

public class DatabaseOptions : IValidatableObject
{
    public const string Section = "Database";

    public string ConnectionString { get; set; } = string.Empty;
    public int CommandTimeoutSeconds { get; set; } = 30;
    public int MaxRetryCount { get; set; } = 3;
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);
    public bool EnableSensitiveDataLogging { get; set; } = false;
    public bool EnableDetailedErrors { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        if (string.IsNullOrWhiteSpace(ConnectionString))
            results.Add(new ValidationResult("Connection string is required", new[] { nameof(ConnectionString) }));

        if (CommandTimeoutSeconds <= 0)
            results.Add(new ValidationResult("Command timeout must be positive", new[] { nameof(CommandTimeoutSeconds) }));

        if (MaxRetryCount < 0)
            results.Add(new ValidationResult("Max retry count cannot be negative", new[] { nameof(MaxRetryCount) }));

        return results;
    }
}
