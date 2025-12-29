namespace SaveState.Core.Common.Configuration;

using System.ComponentModel.DataAnnotations;

public class ApplicationOptions : IValidatableObject
{
    public const string Section = "Application";

    public string ApplicationName { get; set; } = "SaveState Reborn";
    public string Version { get; set; } = "1.0.0";
    public string Environment { get; set; } = "Development";
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool EnableDetailedLogging { get; set; } = false;
    public string DataDirectory { get; set; } = "./data";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        if (string.IsNullOrWhiteSpace(ApplicationName))
            results.Add(new ValidationResult("Application name is required", new[] { nameof(ApplicationName) }));

        if (DefaultTimeout <= TimeSpan.Zero)
            results.Add(new ValidationResult("Default timeout must be positive", new[] { nameof(DefaultTimeout) }));

        return results;
    }
}
